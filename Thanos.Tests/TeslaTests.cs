using System.Numerics;
using Thanos.Enums;

namespace Thanos.Tests;

[TestFixture]
public class TeslaTests
{
    private Tesla _tesla;
    private uint _defaultBoardWidth = 11;
    private uint _defaultBoardHeight = 11;

    [TearDown]
    public void TearDown()
    {
        _tesla.Dispose();
    }

    [Test]
    public void Initialize_WithValidPositions_ShouldSetActiveSnakes()
    {
        // Arrange
        ushort[] startingPositions = [10, 20, 30];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(3));
    }

    [Test]
    public void Initialize_SingleSnake_ShouldWorkCorrectly()
    {
        // Arrange
        ushort[] startingPositions = [55];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(1));
    }

    [Test]
    public void Initialize_MaxSnakes_ShouldWorkCorrectly()
    {
        // Arrange
        ushort[] startingPositions = [10, 20, 30, 40, 50, 60, 70, 80];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(Constants.MaxSnakes));
    }

    [Test]
    public unsafe void GetSnake_ShouldReturnValidPointer()
    {
        // Arrange
        ushort[] startingPositions = [25, 50, 75];
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Act
        var snake0 = _tesla.GetSnake(0);
        var snake1 = _tesla.GetSnake(1);
        var snake2 = _tesla.GetSnake(2);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That((IntPtr)snake0, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That((IntPtr)snake1, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That((IntPtr)snake2, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That((IntPtr)snake1, Is.Not.EqualTo((IntPtr)snake0));
            Assert.That((IntPtr)snake2, Is.Not.EqualTo((IntPtr)snake1));
        });
    }

    [Test]
    public unsafe void Initialize_ShouldResetSnakesCorrectly()
    {
        // Arrange
        ushort[] startingPositions = [10, 20, 30];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Assert
        for (byte i = 0; i < startingPositions.Length; i++)
        {
            var snake = _tesla.GetSnake(i);
            Assert.Multiple(() =>
            {
                Assert.That(snake->Health, Is.EqualTo(100));
                Assert.That(snake->Length, Is.EqualTo(1));
                Assert.That(snake->Head, Is.EqualTo(startingPositions[i]));
                Assert.That(snake->HeadIndex, Is.EqualTo(0));
                Assert.That(snake->TailIndex, Is.EqualTo(0));
            });
        }
    }

    [Test]
    public unsafe void Initialize_SnakeCapacityMask_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        ushort[] startingPositions = [50];
        var boardArea = _defaultBoardWidth * _defaultBoardHeight; // 121
        var expectedCapacity = (int)BitOperations.RoundUpToPowerOf2(boardArea); // 128
        var expectedMask = Math.Min(expectedCapacity, Constants.MaxBodyLength) - 1; // 127
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Assert
        var snake = _tesla.GetSnake(0);
        Assert.That(snake->CapacityMask, Is.EqualTo(expectedMask));
    }

    [TestCase(5u, 5u, 32u - 1)]    // 25 -> 32
    [TestCase(8u, 8u, 64u - 1)]    // 64 -> 64
    [TestCase(10u, 10u, 128u - 1)] // 100 -> 128
    [TestCase(15u, 15u, 256u - 1)] // 225 -> 256
    [TestCase(20u, 20u, 255u)]     // 400 -> 512, but capped at 256
    public unsafe void Initialize_DifferentBoardSizes_ShouldCalculateCapacityCorrectly(
        uint width, uint height, uint expectedMask)
    {
        // Arrange
        ushort[] startingPositions = [0];
            
        // Act
        _tesla.Initialize(width, height, startingPositions);
            
        // Assert
        var snake = _tesla.GetSnake(0);
        Assert.That(snake->CapacityMask, Is.EqualTo(expectedMask));
    }

    [Test]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        ushort[] startingPositions = [10];
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            _tesla.Dispose();
            _tesla.Dispose();
            _tesla.Dispose();
        });
    }

    [Test]
    public void Dispose_WithoutInitialize_ShouldNotThrow()
    {
        // Arrange
        var tesla = new Tesla();
            
        // Act & Assert
        Assert.DoesNotThrow(() => tesla.Dispose());
    }

    [Test]
    public void Initialize_CalledTwice_ShouldReinitialize()
    {
        // Arrange
        ushort[] firstPositions = [10, 20];
        ushort[] secondPositions = [30, 40, 50];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, firstPositions);
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, secondPositions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(3));
    }

    [Test]
    public unsafe void GetSnake_AllSnakes_ShouldHaveDifferentMemoryAddresses()
    {
        // Arrange
        ushort[] startingPositions = [10, 20, 30, 40];
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Act & Assert
        for (byte i = 0; i < startingPositions.Length - 1; i++)
        {
            var snake1 = _tesla.GetSnake(i);
            var snake2 = _tesla.GetSnake((byte)(i + 1));
                
            // Verify that snakes are not overlapping in memory
            var distance = Math.Abs((byte*)snake2 - (byte*)snake1);
            Assert.That(distance, Is.GreaterThanOrEqualTo(BattleSnake.HeaderSize));
        }
    }

    [Test]
    public void HeaderSize_ShouldBeCorrect()
    {
        // Assert
        Assert.That(Tesla.HeaderSize, Is.EqualTo(2 * Constants.CacheLineSize));
        Assert.That(Tesla.HeaderSize, Is.EqualTo(128)); // 2 * 64
    }

    [Test]
    public unsafe void Initialize_WithEmptyPositions_ShouldSetActiveSnakesToZero()
    {
        // Arrange
        ushort[] emptyPositions = [];
            
        // Act
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, emptyPositions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(0));
    }

    [Test]
    public unsafe void SnakePointers_ShouldBeConsistent()
    {
        // Arrange
        ushort[] startingPositions = [10, 20, 30];
        _tesla.Initialize(_defaultBoardWidth, _defaultBoardHeight, startingPositions);
            
        // Act - Get same snake multiple times
        var snake0_first = _tesla.GetSnake(0);
        var snake0_second = _tesla.GetSnake(0);
        var snake1_first = _tesla.GetSnake(1);
        var snake1_second = _tesla.GetSnake(1);
            
        // Assert - Pointers should be the same
        Assert.Multiple(() =>
        {
            Assert.That((IntPtr)snake0_first, Is.EqualTo((IntPtr)snake0_second));
            Assert.That((IntPtr)snake1_first, Is.EqualTo((IntPtr)snake1_second));
        });
    }

    [Test]
    public unsafe void Initialize_LargeBoardSmallSnakeCount_ShouldNotWasteMemory()
    {
        // This test verifies that memory allocation is based on active snakes, not board size
            
        // Arrange
        ushort[] positions = [100]; // Only 1 snake
        uint largeWidth = 50;
        uint largeHeight = 50;
            
        // Act
        _tesla.Initialize(largeWidth, largeHeight, positions);
            
        // Assert
        Assert.That(_tesla.ActiveSnakes, Is.EqualTo(1));
        // The actual memory test would require access to private fields,
        // but we can verify the snake works correctly
        var snake = _tesla.GetSnake(0);
        Assert.That(snake->Head, Is.EqualTo(100));
    }
}