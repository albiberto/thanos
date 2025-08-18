using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(Span<byte> slot, in MemoryLayout layout)
{
    private readonly Span<byte> _slot = slot;
    private readonly MemoryLayout _layout = layout;

    /// <summary>
    /// Inizializza questo slot di memoria con uno stato di gioco iniziale da una Request.
    /// </summary>
    public void CloneFrom(in Request request)
    {
        InitializeNode();
        var warField = InitializeWarField(in request.Board);
        InitializeWarSnakes(ref warField, in request.Board);
        InitializeArenaHeader(request.Board.SnakeCount); // NUOVO
    }

    /// <summary>
    /// Clona l'intero stato di gioco da un altro MemorySlot in questo.
    /// </summary>
    public void CloneFrom(in MemorySlot source) => source._slot.CopyTo(_slot);
    
    /// <summary>
    /// Restituisce un puntatore unsafe al Node all'inizio di questo slot.
    /// </summary>
    public unsafe Node* GetNodePtr() => (Node*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_slot));

    private void InitializeNode()
    {
        var nodeSpan = _slot.Slice(_layout.NodeOffset, _layout.NodeSize);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node(); // Inizializza a zero/default
    }

    private WarField InitializeWarField(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_layout.BitboardsOffset, _layout.BitboardsSize);
        bitboardsSpan.Clear();
        
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);
    
        return new WarField(board.Width, board.Height, board.Area, food, hazards, snakes, board.Food, board.Hazards);
    }
    
    private void InitializeWarSnakes(ref WarField field, in Board board)
    {
        var snakesSpan = _slot.Slice(_layout.SnakesOffset, _layout.SnakesSize);
        for (var i = 0; i < board.SnakeCount; i++)
        {
            var singleSnakeBlock = snakesSpan.Slice(i * _layout.SnakeStride, _layout.SnakeStride);
            var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);
            ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
        
            new WarSnake(ref header, bodySpan, in board.Snakes[i], ref field);
        }
    }
    
    /// <summary>
    /// Restituisce la "vista" WarField per i dati di questo slot.
    /// </summary>
    private WarField GetField(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_layout.BitboardsOffset, _layout.BitboardsSize);
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _layout.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        // ORA È SICURO: Passiamo i valori primitivi estratti da _context.
        return new WarField(board.Width, board.Height, board.Area, food, hazards, snakes); 
    }

    /// <summary>
    /// Restituisce la "vista" WarArena per i dati di questo slot.
    /// </summary>
    public WarArena GetArena(in Board board)
    {
        var field = GetField(board);
        var snakesMemory = _slot.Slice(_layout.SnakesOffset, _layout.SnakesSize);
        
        // Estrae il riferimento all'header dalla memoria
        var headerSpan = _slot.Slice(_layout.WarArenaHeaderOffset, _layout.WarArenaHeaderSize);
        ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(headerSpan));

        return new WarArena(ref header, field, snakesMemory, board.SnakeCount, _layout.SnakeStride);
    }
    
    private void InitializeArenaHeader(int snakeCount)
    {
        var headerSpan = _slot.Slice(_layout.WarArenaHeaderOffset, _layout.WarArenaHeaderSize);
        ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(headerSpan));
        header.LiveSnakesCount = snakeCount;
    }
}