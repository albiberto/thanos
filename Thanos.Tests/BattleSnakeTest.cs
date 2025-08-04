using Thanos.Enums;

namespace Thanos.Tests;

[TestFixture]
public class BattleSnakeTests
{
    private static unsafe BattleSnake* CreateSnake(int capacity = 128) => (BattleSnake*)System.Runtime.InteropServices.Marshal.AllocHGlobal(BattleSnake.HeaderSize + capacity * sizeof(ushort));

    private static unsafe void FreeSnake(BattleSnake* snake) => System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)snake);

    [Test]
    public unsafe void Reset_ShouldInitializeSnakeCorrectly()
    {
        var snake = CreateSnake();
        try
        {
            const ushort startPosition = 42;
            const int capacity = 128;

            snake->Reset(startPosition, capacity);

            Assert.Multiple(() =>
            {
                Assert.That(snake->Health, Is.EqualTo(100));
                Assert.That(snake->Length, Is.EqualTo(1));
                Assert.That(snake->Head, Is.EqualTo(startPosition));
                Assert.That(snake->CapacityMask, Is.EqualTo(capacity - 1));
                Assert.That(snake->HeadIndex, Is.EqualTo(0));
                Assert.That(snake->TailIndex, Is.EqualTo(0));
                Assert.That(snake->Body[0], Is.EqualTo(startPosition));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(256)]
    public unsafe void Reset_ShouldWorkWithDifferentCapacities(int capacity)
    {
        var snake = CreateSnake(capacity);
        try
        {
            const ushort startPosition = 100;

            snake->Reset(startPosition, capacity);

            Assert.That(snake->CapacityMask, Is.EqualTo(capacity - 1));
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_OnEmptyCell_ShouldDecrementHealth()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            const ushort newPosition = 11;

            var result = snake->Move(newPosition, Constants.Empty, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snake->Health, Is.EqualTo(99));
                Assert.That(snake->Head, Is.EqualTo(newPosition));
                Assert.That(snake->Length, Is.EqualTo(1));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_OnFood_ShouldRestoreHealthAndGrow()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            snake->Health = 50; // Set lower health
            const ushort newPosition = 11;
            const byte foodCell = Constants.Food;

            var result = snake->Move(newPosition, foodCell, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snake->Health, Is.EqualTo(100));
                Assert.That(snake->Head, Is.EqualTo(newPosition));
                Assert.That(snake->Length, Is.EqualTo(2));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [TestCase(Constants.Me)]
    [TestCase(Constants.Enemy1)]
    [TestCase(Constants.Enemy2)]
    [TestCase(Constants.Enemy3)]
    [TestCase(Constants.Enemy4)]
    [TestCase(Constants.Enemy5)]
    [TestCase(Constants.Enemy6)]
    [TestCase(Constants.Enemy7)]
    public unsafe void Move_OnSnakeBody_ShouldReturnFalse(int snakeContent)
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            ushort newPosition = 11;

            var result = snake->Move(newPosition, (byte)snakeContent, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(snake->Health, Is.EqualTo(0));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [TestCase(10, 90)]
    [TestCase(20, 80)]
    [TestCase(50, 50)]
    public unsafe void Move_OnHazard_ShouldDecrementHealthByHazardDamage(int hazardDamage, int expectedHealth)
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            const ushort newPosition = 11;
            const byte hazardCell = Constants.Hazard;

            var result = snake->Move(newPosition, hazardCell, hazardDamage);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snake->Health, Is.EqualTo(expectedHealth));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_WhenHealthReachesZero_ShouldReturnFalse()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            snake->Health = 1; // One move away from death
            const ushort newPosition = 11;
            const byte emptyCell = Constants.Empty;

            var result = snake->Move(newPosition, emptyCell, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(snake->Health, Is.EqualTo(0));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_CircularBuffer_ShouldWrapAroundCorrectly()
    {
        var snake = CreateSnake(4); // Small capacity for testing wrap-around
        try
        {
            snake->Reset(10, 4);
                
            // Grow the snake to fill the buffer
            snake->Move(11, Constants.Food, 0);
            snake->Move(12, Constants.Food, 0);
            snake->Move(13, Constants.Food, 0);
                
            // This move should wrap around
            var result = snake->Move(14, Constants.Food, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(snake->Length, Is.EqualTo(5));
                Assert.That(snake->HeadIndex & snake->CapacityMask, Is.LessThan(4));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_MultipleMovesWithoutFood_ShouldMaintainLength()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
                
            // Grow the snake first
            snake->Move(11, Constants.Food, 0);
            snake->Move(12, Constants.Food, 0);
                
            var initialLength = snake->Length;
                
            // Move without eating
            snake->Move(13, Constants.Empty, 0);
            snake->Move(14, Constants.Empty, 0);
            snake->Move(15, Constants.Empty, 0);

            Assert.That(snake->Length, Is.EqualTo(initialLength));
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void Move_BodySegments_ShouldFollowHeadCorrectly()
    {
        var snake = CreateSnake(); // Assuming this allocates memory for the snake
        try
        {
            // Setup a snake with capacity 128
            snake->Reset(10, 128); 
        
            // Grow the snake to length 3
            snake->Move(11, Constants.Food, 0); // Head: 11, Body: [10]
            snake->Move(12, Constants.Food, 0); // Head: 12, Body: [10, 11]
        
            // Record the head position before the next move
            var previousHead = snake->Head; // previousHead is 12
        
            // Move without eating
            snake->Move(13, Constants.Empty, 0); // Head: 13, Body: [11, 12]
        
            // --- CORRECTED ASSERTION ---
            // The previous head (12) is now the newest body segment. It's located
            // at the index right before the current (empty) HeadIndex.
            var lastWrittenIndex = (snake->HeadIndex - 1) & (uint)snake->CapacityMask;
            Assert.That(snake->Body[lastWrittenIndex], Is.EqualTo(previousHead), "The newest body segment should be the previous head's position.");
        }
        finally
        {
            FreeSnake(snake); // Assuming this frees the allocated memory
        }
    }

    [Test]
    public unsafe void Move_ComplexScenario_ShouldHandleCorrectly()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(100, 128);
                
            // Scenario: Snake grows, takes hazard damage, eats, and moves normally
            snake->Move(101, Constants.Food, 0);    // Eat, health=100, length=2
            snake->Move(102, Constants.Hazard, 15); // Hazard, health=85, length=2
            snake->Move(103, Constants.Empty, 0);   // Empty, health=84, length=2
            snake->Move(104, Constants.Food, 0);    // Eat, health=100, length=3
            snake->Move(105, Constants.Empty, 0);   // Empty, health=99, length=3

            Assert.Multiple(() =>
            {
                Assert.That(snake->Health, Is.EqualTo(99));
                Assert.That(snake->Length, Is.EqualTo(3));
                Assert.That(snake->Head, Is.EqualTo(105));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }

    [Test]
    public unsafe void CapacityMask_ShouldBeCorrectlyCalculated()
    {
        var testCases = new[] { (16, 15), (32, 31), (64, 63), (128, 127), (256, 255) };
            
        foreach (var (capacity, expectedMask) in testCases)
        {
            var snake = CreateSnake(capacity);
            try
            {
                snake->Reset(10, capacity);
                Assert.That(snake->CapacityMask, Is.EqualTo(expectedMask), 
                    $"Failed for capacity {capacity}");
            }
            finally
            {
                FreeSnake(snake);
            }
        }
    }

    [Test]
    public unsafe void Move_EdgeCase_HealthExactlyZeroAfterHazard_ShouldReturnFalse()
    {
        var snake = CreateSnake();
        try
        {
            snake->Reset(10, 128);
            snake->Health = 30;
                
            var result = snake->Move(11, Constants.Hazard, 30);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(snake->Health, Is.EqualTo(0));
            });
        }
        finally
        {
            FreeSnake(snake);
        }
    }
}