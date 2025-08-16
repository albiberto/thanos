// Thanos.Memory/MemorySlot.cs

// CAMBIAMENTO: Non più 'unsafe', accetta uno Span gestito.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

public readonly ref struct MemorySlot(Span<byte> slotSpan, in WarContext context, in MemoryLayout layout)
{
    private readonly Span<byte> _slot = slotSpan;
    private readonly WarContext _context = context;
    private readonly MemoryLayout _layout = layout;

    public void CloneFrom(in Request request)
    {
        // La logica è la stessa, ma le chiamate sono più pulite.
        InitializeNode();
        
        // Il WarField viene creato e mantenuto come variabile locale per essere passato agli altri initializers.
        var warField = InitializeWarField(in request.Board);
        InitializeWarSnakes(ref warField, in request.Board);
        InitializeWarArena(ref warField);
    }
    
    // CAMBIAMENTO: Metodo di istanza, più pulito.
    private void InitializeNode()
    {
        var nodeSpan = _slot.Slice(_layout.Offsets.Node, _layout.Sizes.Node);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node();
    }

    // CAMBIAMENTO: Metodo di istanza, restituisce la "vista" creata.
    private WarField InitializeWarField(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        bitboardsSpan.Clear();
        
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.Sizes.BitboardStrideInUlongs; // Usiamo la dimensione in ulongs
        
        var food = bitboardsUlongSpan.Slice(0, stride);
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);
    
        return new WarField(in _context, food, hazards, snakes, board.Food, board.Hazards);
    }
    
    // CAMBIAMENTO: Metodo di istanza.
    private void InitializeWarSnakes(ref WarField field, in Board board)
    {
        var snakesSpan = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);
        for (var i = 0; i < _context.SnakeCount; i++)
        {
            var singleSnakeBlock = snakesSpan.Slice(i * _layout.Sizes.SnakeStride, _layout.Sizes.SnakeStride);
            var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);
            ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
        
            new WarSnake(ref header, bodySpan, in board.Snakes[i], ref field);
        }
    }
    
    // CAMBIAMENTO: Metodo di istanza, il nome "PlacementNew" qui è fuorviante.
    private void InitializeWarArena(ref WarField field)
    {
        var snakesMemory = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);
        var arena = new WarArena(ref field, snakesMemory, in _context, _layout.Sizes.SnakeStride);
        
        // L'arena ora è una "vista", ma se avesse dati da inizializzare in memoria,
        // questo sarebbe il posto giusto per farlo, simile a come facciamo con Node.
        // In questo caso, la creazione della vista è sufficiente per l'uso successivo.
    }
}