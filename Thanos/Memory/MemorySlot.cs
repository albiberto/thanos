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
            // Dividi il blocco di memoria per il singolo serpente
            var singleSnakeBlock = snakesSpan.Slice(i * layout.Sizes.SnakeStride, layout.Sizes.SnakeStride);
            var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..]);

            // Ottieni un riferimento alla memoria dell'header e inizializzala
            ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
        
            var initialSnakeData = board.Snakes[i];
            var capacity = (uint)bodySpan.Length;
            var length = (uint)System.Math.Min(initialSnakeData.Length, capacity);

            header.Capacity = capacity;
            header.Length = length;
            header.Health = initialSnakeData.Health;
            header.TailIndex = 0;
            header.NextHeadIndex = length & (capacity - 1);

            // Popola il corpo e aggiorna la bitboard in WarField
            for (var j = 0; j < length; j++)
            {
                // Il corpo del serpente nei dati iniziali è in ordine inverso (testa alla fine)
                var index = (int)(length - 1 - j);
                ref readonly var coordinate = ref initialSnakeData.Body[index];
                var coord1D = field.To1D(in coordinate);

                bodySpan[j] = coord1D;
                field.SetSnakeBit(coord1D);
            }

            header.Head = length > 0
                ? bodySpan[(int)length - 1] // La testa è l'ultimo elemento scritto
                : ushort.MaxValue;
        }
    }
    
    private static void PlacementNewWarArena(Span<byte> arenaSpan, WarSnake* snakesPtr, ref WarField field, in WarContext context, in MemoryLayout layout)
    {
        fixed (WarField* fieldPtr = &field) // Ottieni il puntatore da passare
        {
            WarArena.PlacementNew(MemoryMarshal.Cast<byte, WarArena>(arenaSpan), snakesPtr, fieldPtr, context, layout);
        }
    }
}