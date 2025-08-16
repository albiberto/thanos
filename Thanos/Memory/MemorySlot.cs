using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public readonly unsafe ref struct MemorySlot(byte* slotPtr, in WarContext context, in MemoryLayout layout)
{
    private readonly Span<byte> _slot = new(slotPtr, layout.Sizes.Slot);

    private readonly WarContext _context = context;
    private readonly MemoryLayout _layout = layout;

    public void CloneFrom(in Request request)
    {
        var nodeSpan = _slot.Slice(_layout.Offsets.Node, _layout.Sizes.Node);
        PlacementNewNode(nodeSpan);
        
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        bitboardsSpan.Clear();
        var warField = PlacementNewWarField(bitboardsSpan, in _context, in _layout, in request.Board);

        var snakesSpan = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);
        PlacementNewWarSnakes(snakesSpan, ref warField, in _context, in _layout, in request.Board);

        var arenaSpan = _slot.Slice(_layout.Offsets.WarArena, _layout.Sizes.WarArena);
        PlacementNewWarArena(arenaSpan, snakesPtr, ref fieldRef, in _context, in _layout);
    }
    
    private static void PlacementNewNode(Span<byte> nodeSpan)
    {
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node();
    }

    private static WarField PlacementNewWarField(Span<byte> bitboardsByteSpan, in WarContext context, in MemoryLayout layout, in Board board)
    {
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsByteSpan);

        var offsetThirdElement = layout.Sizes.BitboardStride * 2;
    
        var food = bitboardsUlongSpan[..layout.Sizes.BitboardStride];
        var hazards = bitboardsUlongSpan[layout.Sizes.BitboardStride .. offsetThirdElement];
        var snakes = bitboardsUlongSpan[offsetThirdElement .. (layout.Sizes.BitboardStride * 3)];
    
        return new WarField(in context, food, hazards, snakes, board.Food,board.Hazards);
    }
    
    private static void PlacementNewWarSnakes(Span<byte> snakesSpan, ref WarField field, in WarContext context, in MemoryLayout layout, in Board board)
    {
        for (var i = 0; i < context.SnakeCount; i++)
        {
            // Prepara i pezzi di memoria grezza
            var singleSnakeBlock = snakesSpan.Slice(i * layout.Sizes.SnakeStride, layout.Sizes.SnakeStride);
            var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);
            ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
        
            // Una singola, chiara chiamata al costruttore che fa tutto il lavoro.
            new WarSnake(ref header, bodySpan, in board.Snakes[i], ref field);
        }
    }
    
    /// <summary>
    /// Crea una "vista" WarArena che opera sui dati di gioco forniti.
    /// Sostituisce il vecchio metodo PlacementNewWarArena.
    /// </summary>
    private static WarArena PlacementNewWarArena(ref WarField field, Span<byte> snakesMemory, in WarContext context, in MemoryLayout layout) => 
        new(ref field, snakesMemory, in context, layout.Sizes.SnakeStride);
}