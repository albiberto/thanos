// Thanos.Memory/MemorySlot.cs

// CAMBIAMENTO: Non più 'unsafe', accetta uno Span gestito.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(Span<byte> slotSpan, in WarContext context, in MemoryLayout layout)
{
    private readonly Span<byte> _slot = slotSpan;
    private readonly WarContext _context = context;
    private readonly MemoryLayout _layout = layout;

    /// <summary>
    /// Clona l'intero stato di gioco da un altro MemorySlot in questo.
    /// Esegue una copia di memoria a basso livello, estremamente veloce.
    /// </summary>
    /// <param name="source">Lo slot da cui copiare i dati.</param>
    public void CloneFrom(in MemorySlot source) => source._slot.CopyTo(_slot);

    public void CloneFrom(in Request request)
    {
        // La logica è la stessa, ma le chiamate sono più pulite.
        InitializeNode();
        
        // Il WarField viene creato e mantenuto come variabile locale per essere passato agli altri initializers.
        var warField = InitializeWarField(in request.Board);
        InitializeWarSnakes(ref warField, in request.Board);
        InitializeWarArena(ref warField);
    }
    
    /// <summary>
    /// NUOVO HELPER: Crea e restituisce la "vista" WarField per questo slot.
    /// </summary>
    public WarField GetField()
    {
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.Sizes.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan.Slice(0, stride);
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        // NOTA: Qui usiamo un costruttore di WarField che NON ri-popola i dati,
        // ma si limita a creare la vista. Dovremo assicurarci che esista.
        return new WarField(in _context, food, hazards, snakes); 
    }

    /// <summary>
    /// NUOVO HELPER: Crea e restituisce la "vista" WarArena per questo slot.
    /// </summary>
    public WarArena GetArena()
    {
        // 1. Ottieni la vista WarField di cui l'Arena ha bisogno.
        var field = GetField();
        
        // 2. Ottieni lo span di memoria per i serpenti.
        var snakesMemory = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);

        // 3. Crea e restituisce la vista WarArena.
        return new WarArena(ref field, snakesMemory, in _context, in _layout);
    }
    
    public unsafe Node* GetNodePtr()
    {
        // 1. Ottiene un riferimento gestito ('ref byte') al primo byte dello Span.
        ref var firstByte = ref MemoryMarshal.GetReference(_slot);
        
        // 2. Converte il riferimento gestito in un puntatore grezzo ('void*').
        var pointer = Unsafe.AsPointer(ref firstByte);
        
        // 3. Converte il puntatore grezzo nel tipo corretto ('Node*').
        return (Node*)pointer;
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