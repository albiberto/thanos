using System.Runtime.CompilerServices;
using Thanos.Memory;
using Thanos.War;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryLayoutTests
{
    private const ushort SmallArea = 121; // 11x11
    private const ushort QueueCapacity = 121;
    private const byte MaxSnakes = 4;

    /// <summary>
    ///     Verifies that SlotMemoryLayout correctly calculates struct sizes for WarSnakeLife and Bitboard,
    ///     ensuring memory layout accuracy for a standard grid.
    /// </summary>
    [Test]
    public unsafe void Layout_Should_Calculate_StructSizes_Correctly()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var expectedLifeLength = (nuint)sizeof(WarSnakeLife);
        var expectedBitboardLength = (nuint)(sizeof(ulong) * Constants.BitboardQuadWords); // For area 121: (121+63)/64 = 2 ulongs
        
        var actualLifeLength = layout.WarSnakeLife.Length;
        var actualBitboardLength = layout.Bitboard.Length;

        Multiple(() =>
        {
            That(actualLifeLength, Is.EqualTo(expectedLifeLength), 
                $"WarSnakeLife.Length should be {expectedLifeLength} but was {actualLifeLength}.");
            That(actualBitboardLength, Is.EqualTo(expectedBitboardLength), 
                $"Bitboard.Length should be {expectedBitboardLength} but was {actualBitboardLength}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryLayout aligns QueueBuffer to cache line boundaries (64 bytes)
    ///     and ensures it does not overlap with CircularQueueState.
    /// </summary>
    [Test]
    public void Layout_Should_Align_QueueBuffer_To_CacheLine()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var actualBufferOffset = layout.QueueBuffer.Offset;
        var actualBufferOffsetRemainder = (long)actualBufferOffset % Constants.CacheLine;
        
        var previousBlockEnd = layout.CircularQueueState.Offset + (nuint)Unsafe.SizeOf<CircularQueueState>();

        Multiple(() =>
        {
            That(actualBufferOffsetRemainder, Is.Zero, 
                $"QueueBuffer.Offset should be aligned to {Constants.CacheLine} bytes but remainder was {actualBufferOffsetRemainder}.");
            That(actualBufferOffset, Is.GreaterThanOrEqualTo(previousBlockEnd), 
                $"QueueBuffer.Offset ({actualBufferOffset}) should be >= previousBlockEnd ({previousBlockEnd}) to avoid overlap.");
        });
    }
    
    [Test]
    public void Layout_Bitboard_Should_Be_Aligned_To_16Bytes_ForSIMD()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        // Castiamo tutto a long per evitare problemi con nuint in NUnit
        var offset = (long)layout.Bitboard.Offset;
        var remainder = offset % 16;
        
        // FIX: Cast esplicito a long anche qui
        var lifeEnd = (long)(layout.WarSnakeLife.Offset + layout.WarSnakeLife.Length);

        Multiple(() =>
        {
            That(remainder, Is.Zero, 
                $"Bitboard.Offset ({offset}) must be aligned to 16 bytes for SIMD, but remainder was {remainder}.");
            
            // Ora confrontiamo long con long -> Safe
            That(offset, Is.GreaterThan(lifeEnd), 
                $"There should be padding bytes between WarSnakeLife (End: {lifeEnd}) and Bitboard (Start: {offset}).");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryLayout aligns SnakeStride to cache line boundaries (64 bytes)
    ///     to prevent false sharing between snakes, and ensures stride contains all snake data.
    /// </summary>
    [Test]
    public void Layout_Should_Align_SnakeStride_To_CacheLine()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var actualStrideNext = layout.SnakeStride.Next;
        var actualStrideNextRemainder = (long)actualStrideNext % Constants.CacheLine;
        
        var lastBlockEnd = layout.QueueBuffer.Offset + layout.QueueBuffer.Length;

        Multiple(() =>
        {
            That(actualStrideNextRemainder, Is.Zero, 
                $"SnakeStride.Next should be aligned to {Constants.CacheLine} bytes but remainder was {actualStrideNextRemainder}.");
            That(actualStrideNext, Is.GreaterThanOrEqualTo(lastBlockEnd), 
                $"SnakeStride.Next ({actualStrideNext}) should be >= lastBlockEnd ({lastBlockEnd}) to contain all snake data.");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryLayout correctly calculates offsets for shared bitboards (Collisions, Food, Hazards)
    ///     ensuring proper cache line alignment for CollisionsBitboard and sequential ordering without overlaps.
    /// </summary>
    [Test]
    public void Layout_Should_Calculate_SharedBitboards_Offsets_Correctly()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var snakesTotalSize = layout.SnakeStride.Next * MaxSnakes;
        
        var actualCollisionsBbOffset = layout.CollisionsBitboard.Offset;
        var actualCollisionsBbOffsetRemainder = (long)actualCollisionsBbOffset % Constants.CacheLine;
        
        var collisionsBbEnd = layout.CollisionsBitboard.Offset + layout.CollisionsBitboard.Length;
        var actualFoodBbOffset = layout.FoodBitboard.Offset;
        
        var foodBbEnd = layout.FoodBitboard.Offset + layout.FoodBitboard.Length;
        var actualHazardsBbOffset = layout.HazardsBitboard.Offset;

        Multiple(() =>
        {
            That(actualCollisionsBbOffsetRemainder, Is.Zero, 
                $"CollisionsBitboard.Offset should be aligned to {Constants.CacheLine} bytes but remainder was {actualCollisionsBbOffsetRemainder}.");
            That(actualCollisionsBbOffset, Is.GreaterThanOrEqualTo(snakesTotalSize), 
                $"CollisionsBitboard.Offset ({actualCollisionsBbOffset}) should be >= snakesTotalSize ({snakesTotalSize}).");
            That(actualFoodBbOffset, Is.GreaterThanOrEqualTo(collisionsBbEnd), 
                $"FoodBitboard.Offset ({actualFoodBbOffset}) should be >= collisionsBbEnd ({collisionsBbEnd}).");
            That(actualHazardsBbOffset, Is.GreaterThanOrEqualTo(foodBbEnd), 
                $"HazardsBitboard.Offset ({actualHazardsBbOffset}) should be >= foodBbEnd ({foodBbEnd}).");
        });
    }

    /// <summary>
    ///     Verifies that SlotMemoryLayout aligns total SlotStride.Next to cache line boundaries (64 bytes)
    ///     to ensure efficient memory access patterns.
    /// </summary>
    [Test]
    public void Layout_Should_Align_SlotStride_To_CacheLine()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var actualSlotStrideNext = layout.SlotStride.Next;
        var actualSlotStrideNextRemainder = (long)actualSlotStrideNext % Constants.CacheLine;

        That(actualSlotStrideNextRemainder, Is.Zero, 
            $"SlotStride.Next should be aligned to {Constants.CacheLine} bytes but remainder was {actualSlotStrideNextRemainder}.");
    }

    /// <summary>
    ///     Verifies that SlotMemoryLayout stores the correct QueueCapacity value,
    ///     ensuring proper initialization.
    /// </summary>
    [Test]
    public void Layout_Should_Store_QueueCapacity()
    {
        const ushort expectedCapacity = 150;
        var layout = new SlotMemoryLayout(SmallArea, expectedCapacity, MaxSnakes);

        var actualCapacity = layout.QueueCapacity;

        That(actualCapacity, Is.EqualTo(expectedCapacity), 
            $"QueueCapacity should be {expectedCapacity} but was {actualCapacity}.");
    }

    /// <summary>
    ///     Verifies that all snake-local blocks start at offset 0 relative to their snake,
    ///     ensuring correct relative positioning within a snake's memory space.
    /// </summary>
    [Test]
    public void Layout_Should_Have_SnakeBlocks_Starting_At_Zero()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var actualWarSnakeLifeOffset = layout.WarSnakeLife.Offset;
        var actualSnakeStrideOffset = layout.SnakeStride.Offset;

        Multiple(() =>
        {
            That(actualWarSnakeLifeOffset, Is.EqualTo((nuint)0), 
                $"WarSnakeLife.Offset should be 0 but was {actualWarSnakeLifeOffset}.");
            That(actualSnakeStrideOffset, Is.EqualTo((nuint)0), 
                $"SnakeStride.Offset should be 0 but was {actualSnakeStrideOffset}.");
        });
    }

    /// <summary>
    ///     Verifies that SlotStride starts at offset 0,
    ///     ensuring correct slot container initialization.
    /// </summary>
    [Test]
    public void Layout_Should_Have_SlotStride_Starting_At_Zero()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var actualSlotStrideOffset = layout.SlotStride.Offset;

        That(actualSlotStrideOffset, Is.EqualTo((nuint)0), 
            $"SlotStride.Offset should be 0 but was {actualSlotStrideOffset}.");
    }

    /// <summary>
    ///     Verifies that increasing the number of snakes increases the total slot size proportionally,
    ///     ensuring correct memory scaling.
    /// </summary>
    [Test]
    public void Layout_Should_Scale_With_SnakeCount()
    {
        var layout2Snakes = new SlotMemoryLayout(SmallArea, QueueCapacity, 2);
        var layout4Snakes = new SlotMemoryLayout(SmallArea, QueueCapacity, 4);

        var size2Snakes = layout2Snakes.SlotStride.Next;
        var size4Snakes = layout4Snakes.SlotStride.Next;

        That(size4Snakes, Is.GreaterThan(size2Snakes), 
            $"SlotStride.Next for 4 snakes ({size4Snakes}) should be > than for 2 snakes ({size2Snakes}).");
    }

    /// <summary>
    ///     Verifies that the sequence of memory blocks follows the expected order:
    ///     WarSnakeLife -> Bitboard -> CircularQueueState -> QueueBuffer (per snake)
    ///     then CollisionsBitboard -> FoodBitboard -> HazardsBitboard (global).
    /// </summary>
    [Test]
    public void Layout_Should_Have_Correct_Block_Ordering()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        // Snake-local ordering
        var lifeEnd = layout.WarSnakeLife.Offset + layout.WarSnakeLife.Length;
        var bitboardEnd = layout.Bitboard.Offset + layout.Bitboard.Length;
        var queueStateEnd = layout.CircularQueueState.Offset + layout.CircularQueueState.Length;

        // Global ordering
        var collisionsEnd = layout.CollisionsBitboard.Offset + layout.CollisionsBitboard.Length;
        var foodEnd = layout.FoodBitboard.Offset + layout.FoodBitboard.Length;

        Multiple(() =>
        {
            // Snake-local block ordering
            That(layout.Bitboard.Offset, Is.GreaterThanOrEqualTo(lifeEnd), 
                $"Bitboard should start after WarSnakeLife ends.");
            That(layout.CircularQueueState.Offset, Is.GreaterThanOrEqualTo(bitboardEnd), 
                $"CircularQueueState should start after Bitboard ends.");
            That(layout.QueueBuffer.Offset, Is.GreaterThanOrEqualTo(queueStateEnd), 
                $"QueueBuffer should start after CircularQueueState ends.");

            // Global block ordering
            That(layout.FoodBitboard.Offset, Is.GreaterThanOrEqualTo(collisionsEnd), 
                $"FoodBitboard should start after CollisionsBitboard ends.");
            That(layout.HazardsBitboard.Offset, Is.GreaterThanOrEqualTo(foodEnd), 
                $"HazardsBitboard should start after FoodBitboard ends.");
        });
    }
}

