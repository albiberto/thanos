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
    /// Inizializza questo slot di memoria con uno stato di gioco iniziale da una Request.
    /// </summary>
    public void CloneFrom(in Request request)
    {
        InitializeNode();
        var warField = InitializeWarField(in request.Board);
        InitializeWarSnakes(ref warField, in request.Board);
    }

    /// <summary>
    /// Clona l'intero stato di gioco da un altro MemorySlot in questo.
    /// </summary>
    public void CloneFrom(in MemorySlot source) => source._slot.CopyTo(_slot);
    
    /// <summary>
    /// Restituisce un puntatore unsafe al Node all'inizio di questo slot.
    /// </summary>
    public unsafe Node* GetNodePtr()
    {
        return (Node*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_slot));
    }
    
    private void InitializeNode()
    {
        var nodeSpan = _slot.Slice(_layout.Offsets.Node, _layout.Sizes.Node);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node(); // Inizializza a zero/default
    }

    private WarField InitializeWarField(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        bitboardsSpan.Clear();
        
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.Sizes.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan.Slice(0, stride);
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);
    
        return new WarField(_context.Width, _context.Height, _context.Area, food, hazards, snakes, board.Food, board.Hazards);
    }
    
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
    
    /// <summary>
    /// Restituisce la "vista" WarField per i dati di questo slot.
    /// </summary>
    private WarField GetField()
    {
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.Sizes.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        // ORA È SICURO: Passiamo i valori primitivi estratti da _context.
        return new WarField(_context.Width, _context.Height, _context.Area, food, hazards, snakes); 
    }

    /// <summary>
    /// Restituisce la "vista" WarArena per i dati di questo slot.
    /// </summary>
    public WarArena GetArena()
    {
        var field = GetField();
        var snakesMemory = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);

        return new WarArena(field, snakesMemory, _context.SnakeCount, _layout.Sizes.SnakeStride);
    }
}