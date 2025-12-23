using Thanos.Memory;
using Thanos.War;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SlotMemoryLayoutTests
{
    private const ushort Area = 121;
    private const ushort QueueCapacity = 128; // Potenza di 2
    private const byte SnakesCount = 4;

    [Test]
    public void Constructor_WhenInitializing_ThenCalculatesOffsetsWithCorrectAlignment()
    {
        // Arrange
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);

        // Act & Assert (Chain Validation)
        Multiple(() =>
        {
            // 1. WarSnakeLife (Inizio a 0)
            That(layout.WarSnakeLife.Offset, Is.Zero);
            That(layout.WarSnakeLife.Count<WarSnakeLife>(), Is.EqualTo(1));

            // 2. Bitboard (Deve essere allineata a 16 byte per SIMD/Vector128)
            var lifeEnd = layout.WarSnakeLife.Next;
            That(layout.Bitboard.Offset, Is.GreaterThanOrEqualTo(lifeEnd));
            That((long)layout.Bitboard.Offset % 16, Is.Zero, "Bitboard must be 16-byte aligned for SIMD.");

            // 3. QueueState (Dopo Bitboard)
            That(layout.CircularQueueState.Offset, Is.GreaterThanOrEqualTo(layout.Bitboard.Next));

            // 4. QueueBuffer (Critico: Inizio nuova Cache Line per evitare False Sharing sulla coda)
            That(layout.QueueBuffer.Offset, Is.GreaterThanOrEqualTo(layout.CircularQueueState.Next));
            That((long)layout.QueueBuffer.Offset % Constants.CacheLine, Is.Zero, "QueueBuffer must start on CacheLine boundary.");

            // 5. Snake Stride (La dimensione totale di UNO snake deve essere multipla di 64)
            // Questo permette allo snake [1] di iniziare su una nuova cache line rispetto a snake [0]
            That((long)layout.SnakeStride.Next % Constants.CacheLine, Is.Zero, "Snake stride must be CacheLine aligned to isolate snakes.");
        });
    }

    [Test]
    public void Constructor_WhenCheckingGlobalBlocks_ThenAlignsTo32Bytes()
    {
        // Le Bitboard globali (Food, Hazards) sono condivise e accessibili via AVX (futuro)
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);

        Multiple(() =>
        {
            // Offset deve essere successivo alla fine dell'ultimo snake
            var snakesTotalSize = layout.SnakeStride.Next * SnakesCount;
            That(layout.CollisionsBitboard.Offset, Is.GreaterThanOrEqualTo(snakesTotalSize));

            // Verifica Allineamento 32 byte (Future proofing AVX256)
            That((long)layout.CollisionsBitboard.Offset % 32, Is.Zero);
            That((long)layout.FoodBitboard.Offset % 32, Is.Zero);
            That((long)layout.HazardsBitboard.Offset % 32, Is.Zero);
        });
    }

    [Test]
    public void Constructor_WhenCalculatesSlotStride_ThenAlignsToCacheLine()
    {
        // Uno SLOT intero (tutto il match) deve essere un blocco solido allineato
        var layout = new SlotMemoryLayout(Area, QueueCapacity, SnakesCount);

        That((long)layout.SlotStride.Next % Constants.CacheLine, Is.Zero, "Total Slot Stride must be 64-byte aligned.");
    }
}