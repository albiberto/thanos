using System.Runtime.CompilerServices;
using Thanos.Memory;
using Thanos.War;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class SlotMemoryLayoutTests
{
    private const ushort SmallArea = 121;
    private const ushort QueueCapacity = 128;
    private const byte MaxSnakes = 4;

    [Test]
    public void Bitboard_Should_Be_Aligned_To_16Bytes_ForSIMD()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        var offset = (long)layout.Bitboard.Offset;
        
        // Verifica allineamento
        That(offset % 16, Is.Zero, $"Bitboard offset {offset} must be aligned to 16 bytes.");
        
        // Verifica non sovrapposizione con il blocco precedente
        var lifeEnd = (long)(layout.WarSnakeLife.Offset + layout.WarSnakeLife.Length);
        That(offset, Is.GreaterThanOrEqualTo(lifeEnd));
    }

    [Test]
    public void QueueBuffer_Should_Be_CacheLine_Aligned()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);
        var offset = (long)layout.QueueBuffer.Offset;

        That(offset % 64, Is.Zero, "QueueBuffer must start on a cache line boundary.");
    }

    [Test]
    public void SnakeStride_Should_Be_CacheLine_Aligned()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);
        var stride = (long)layout.SnakeStride.Next;

        That(stride % 64, Is.Zero, "Snake stride must be multiple of 64 bytes.");
    }

    [Test]
    public void GlobalBitboards_Should_Be_Aligned_To_32Bytes_Or_More()
    {
        var layout = new SlotMemoryLayout(SmallArea, QueueCapacity, MaxSnakes);

        That((long)layout.CollisionsBitboard.Offset % 32, Is.Zero);
        That((long)layout.FoodBitboard.Offset % 32, Is.Zero);
        That((long)layout.HazardsBitboard.Offset % 32, Is.Zero);
    }
}