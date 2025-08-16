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
        
        var fieldSpan = _slot.Slice(_layout.Offsets.WarField, _layout.Sizes.WarFieldHeader);
        var bitboardsSpan = _slot.Slice(_layout.Offsets.Bitboards, _layout.Sizes.Bitboards);
        bitboardsSpan.Clear();
        ref var fieldRef = ref PlacementNewWarField(fieldSpan, bitboardsSpan, in _context, in _layout, in request.Board);

        var snakesSpan = _slot.Slice(_layout.Offsets.Snakes, _layout.Sizes.Snakes);
        var snakesPtr = PlacementNewWarSnakes(snakesSpan, ref fieldRef, in _context, in _layout, in request.Board);

        var arenaSpan = _slot.Slice(_layout.Offsets.WarArena, _layout.Sizes.WarArena);
        PlacementNewWarArena(arenaSpan, snakesPtr, ref fieldRef, in _context, in _layout);
    }
    


    private static void PlacementNewNode(Span<byte> nodeSpan)
    {
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node();
    }

    private static ref WarField PlacementNewWarField(Span<byte> fieldSpan, Span<byte> bitboardsByteSpan, in WarContext context, in MemoryLayout layout, in Board board)
    {
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsByteSpan);

        var offsetThirdElement = layout.Sizes.BitboardStride * 2;
        
        var food = bitboardsUlongSpan[..layout.Sizes.BitboardStride];
        var hazards = bitboardsUlongSpan[layout.Sizes.BitboardStride .. offsetThirdElement];
        var snakes = bitboardsUlongSpan[offsetThirdElement..];
        
        ref var field = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarField>(fieldSpan));
        field = new WarField();

        return ref field;
    }
    
    private static WarSnake* PlacementNewWarSnakes(Span<byte> snakesSpan, ref WarField field, in WarContext context, in MemoryLayout layout, in Board board)
    {
        // Il corpo di questo metodo ora usa lo span ricevuto
        for (int i = 0; i < context.SnakeCount; i++)
        {
            var singleSnakeBlock = snakesSpan.Slice(i * layout.Sizes.SnakeStride, layout.Sizes.SnakeStride);
            var headerSpan = singleSnakeBlock[..layout.Sizes.WarSnakeHeader];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[layout.Sizes.WarSnakeHeader..]);

            fixed (WarField* fieldPtr = &field) // Ottieni il puntatore da passare
            {
                WarSnake.PlacementNew(headerSpan, bodySpan, in board.Snakes[i], in *fieldPtr);
            }
        }
        return (WarSnake*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(snakesSpan));
    }
    
    private static void PlacementNewWarArena(Span<byte> arenaSpan, WarSnake* snakesPtr, ref WarField field, in WarContext context, in MemoryLayout layout)
    {
        fixed (WarField* fieldPtr = &field) // Ottieni il puntatore da passare
        {
            WarArena.PlacementNew(MemoryMarshal.Cast<byte, WarArena>(arenaSpan), snakesPtr, fieldPtr, context, layout);
        }
    }
}