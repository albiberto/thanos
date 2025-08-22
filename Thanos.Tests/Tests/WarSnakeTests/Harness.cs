using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

/// <summary>
/// Provides helper methods for allocating raw memory structures needed for snake-related tests.
/// Its single responsibility is memory allocation, not state initialization.
/// </summary>
public static class Harness
{
    /// <summary>
    /// A container for the raw memory segments required to host a WarSnake instance.
    /// </summary>
    public struct SnakeTestContext
    {
        public Health Health;
        public Anatomy Anatomy;
        public ushort[] BodyBuffer;
    }
    
    /// <summary>
    /// Allocates a raw, zeroed-out memory context for a single snake.
    /// </summary>
    /// <param name="capacity">The total capacity for the snake's body buffer.</param>
    public static SnakeTestContext CreateTestContext(int capacity) =>
        new()
        {
            Health = new Health(), 
            Anatomy = new Anatomy(),
            BodyBuffer = new ushort[capacity]
        };
}