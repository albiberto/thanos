using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Tests;

public record TestCase(int Health, int Lenght, int Capacity, ushort[] Body);

[TestFixtureSource(nameof(Cases))] // The source parameterizes the test fixture. 
public unsafe partial class BattleSnakeTests(TestCase @case)
{
    private const byte EmptyCell = 0;

    private static readonly int[] Capacities = [16, 32, 64, 128, 256, 512, 1024, 2048, 4096];
    private static readonly TestCase[] Cases = BuildTestSnakes(Capacities).ToArray();
    
    private byte* _memory = null;
    private BattleSnake* _sut = null;
    
    [SetUp]
    public void SetUp()
    {
        var _snakeStride = BattleSnake.HeaderSize + @case.Capacity * sizeof(ushort);
        _memory = (byte*)NativeMemory.AlignedAlloc((nuint)_snakeStride, 64);
        _sut = (BattleSnake*)_memory;

        Unsafe.InitBlockUnaligned(_memory, EmptyCell, (uint)_snakeStride);
        BattleSnake.PlacementNew(_sut, @case.Health, @case.Body.Length, @case.Capacity, @case.Body);
    }

    [TearDown]
    public void TearDown()
    {
        if (_memory == null) return;

        NativeMemory.AlignedFree(_memory);
        _memory = null;
    }

    /// <summary>
    ///     Dynamically generates a suite of test configurations for snakes.
    ///     For each capacity defined in CapacitiesToTest, this method creates 5 different scenarios.
    /// </summary>
    private static IEnumerable<TestCase> BuildTestSnakes(int[] capacities)
    {
        Random random = new();

        foreach (var capacity in capacities)
        {
            // --- Configuration 1: Minimal Snake (length 1) ---
            // A single-segment snake to test the most basic case.
            yield return new TestCase(100, 1, capacity, [(ushort)random.Next(1, 1000)]);

            // --- Configuration 2: Short Snake (25% of capacity) ---
            // A common scenario with a short snake, not close to its limits.
            var shortLength = capacity / 4;
            yield return new TestCase(90, shortLength, capacity, Enumerable.Range(random.Next(1, 500), shortLength).Select(i => (ushort)i).ToArray());

            // --- Configuration 3: Medium Snake (50% of capacity) ---
            // A snake at half its capacity, a very common scenario.
            var mediumLength = capacity / 2;
            yield return new TestCase(75, mediumLength, capacity, Enumerable.Range(random.Next(1, 500), mediumLength).Select(i => (ushort)i).ToArray());

            // --- Configuration 4: Large Snake (75% of capacity) ---
            // Tests behavior when the snake is long and the circular buffer is heavily used.
            var largeLength = (int)(capacity * 0.75);
            yield return new TestCase(50, largeLength, capacity, Enumerable.Range(random.Next(1, 500), largeLength).Select(i => (ushort)i).ToArray());

            // --- Configuration 5: Full Snake (100% of capacity) ---
            // An edge case to test the behavior when the snake fills its entire buffer.
            yield return new TestCase(100, capacity, capacity, Enumerable.Range(random.Next(1, 500), capacity).Select(i => (ushort)i).ToArray());
        }
    }
}