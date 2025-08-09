using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.War;

namespace Thanos.Tests;

/// <summary>
///     Defines the input parameters for a BattleSnake test case.
/// </summary>
public record TestCase(int Health, int Capacity, ushort[] Body);

/// <summary>
///     Test fixture for BattleSnake, parameterized by a collection of TestCase inputs.
/// </summary>
[TestFixtureSource(nameof(Cases))] // The source parameterizes the test fixture.
public unsafe partial class SnakeTests(TestCase @case)
{
    /// <summary>
    ///     Sets up a new BattleSnake instance for each test using aligned unmanaged memory.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // Calculate required memory size
        var _snakeStride = WarSnake.HeaderSize + @case.Capacity * sizeof(ushort);

        // Allocate aligned memory
        _memory = (byte*)NativeMemory.AlignedAlloc((nuint)_snakeStride, 64);
        _sut = (WarSnake*)_memory;

        // Fill memory with EmptyCell
        Unsafe.InitBlockUnaligned(_memory, EmptyCell, (uint)_snakeStride);

        // Construct the BattleSnake in-place
        WarSnake.PlacementNew(_sut, @case.Health, @case.Body.Length, @case.Capacity, (ushort*)Unsafe.AsPointer(ref @case.Body[0]));
    }

    /// <summary>
    ///     Frees the allocated memory after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_memory == null) return;

        NativeMemory.AlignedFree(_memory);
        _memory = null;
    }

    /// <summary>
    ///     Value used to initialize memory blocks to empty.
    /// </summary>
    private const byte EmptyCell = 0;

    /// <summary>
    ///     List of capacities used to generate test cases.
    /// </summary>
    private static readonly int[] Capacities = [16, 32, 64, 128, 256, 512, 1024, 2048, 4096];

    /// <summary>
    ///     All test cases built from predefined capacities.
    /// </summary>
    private static readonly TestCase[] Cases = BuildTestSnakes(Capacities).ToArray();

    private byte* _memory = null;
    private WarSnake* _sut = null;

    /// <summary>
    ///     Dynamically generates a suite of test configurations for snakes.
    ///     For each capacity defined in <see cref="Capacities" />, this method creates 5 different scenarios.
    /// </summary>
    private static IEnumerable<TestCase> BuildTestSnakes(int[] capacities)
    {
        Random random = new();

        foreach (var capacity in capacities)
        {
            // Configuration 1: Minimal BattleSnake (length 1)
            yield return new TestCase(100, capacity, [(ushort)random.Next(1, 1000)]);

            // Configuration 2: Short BattleSnake (25% of capacity)
            var shortLength = capacity / 4;
            yield return new TestCase(90, capacity, Enumerable.Range(random.Next(1, 500), shortLength).Select(i => (ushort)i).ToArray());

            // Configuration 3: Medium BattleSnake (50% of capacity)
            var mediumLength = capacity / 2;
            yield return new TestCase(75, capacity, Enumerable.Range(random.Next(1, 500), mediumLength).Select(i => (ushort)i).ToArray());

            // Configuration 4: Large BattleSnake (75% of capacity)
            var largeLength = (int)(capacity * 0.75);
            yield return new TestCase(50, capacity, Enumerable.Range(random.Next(1, 500), largeLength).Select(i => (ushort)i).ToArray());

            // Configuration 5: Full BattleSnake (100% of capacity)
            yield return new TestCase(100, capacity, Enumerable.Range(random.Next(1, 500), capacity).Select(i => (ushort)i).ToArray());
        }
    }
}