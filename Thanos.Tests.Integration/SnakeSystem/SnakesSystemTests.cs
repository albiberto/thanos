using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Memory;
using Thanos.War;

namespace Thanos.Tests.Integration.SnakeSystem;

[TestFixture]
public partial class SnakesSystemTests
{
    public static IEnumerable<TestCaseData> SystemScenarios => BuildSystemScenarios();

    private static IEnumerable<TestCaseData> BuildSystemScenarios()
    {
        var maps = new[]
        {
            (Name: "Small", Constants.Small.Area),
            (Name: "Medium", Constants.Medium.Area),
            (Name: "Large", Constants.Large.Area)
        };

        foreach (var map in maps)
        {
            // Calculate capacity based on map size with specific override for Large map
            var rawCapacity = BitOperations.RoundUpToPowerOf2(map.Area);
            var queueCapacity = (ushort)Math.Min(rawCapacity, 256);

            // Iterate Active Snakes (2 to 8)
            for (byte active = 2; active <= 8; active++)
                // Iterate Layout Capacity (Active to 8)
                // This verifies that having "empty slots" in memory doesn't break the system
            for (var layout = active; layout <= 8; layout++)
            {
                // MEMORY ISOLATION: New context per scenario yield
                var context = new SnakesSystemTestContext(map.Name, map.Area, queueCapacity, active, layout);

                yield return new TestCaseData(context)
                    .SetName($"System_{map.Name}_Active{active}_Layout{layout}");
            }
        }
    }

    /// <summary>
    ///     Manages the unmanaged memory lifecycle for SnakesSystem tests.
    ///     Simulates a single slot within the global memory pool.
    /// </summary>
    public unsafe class SnakesSystemTestContext : IDisposable
    {
        // SlotMemoryLayout must be a field to have a stable memory address.
        // SnakesSystem (ref struct) will store a reference to THIS field.
        private readonly SlotMemoryLayout _layout;

        // Memory Pointers & Layout Storage
        private byte* _basePointer;
        private bool _disposed;

        public SnakesSystemTestContext(string mapName, int area, ushort queueCapacity, byte activeSnakeCount, byte layoutMaxSnakeCount)
        {
            if (activeSnakeCount > layoutMaxSnakeCount)
                throw new ArgumentException($"Active snakes ({activeSnakeCount}) cannot exceed layout capacity ({layoutMaxSnakeCount}).");

            MapName = mapName;
            ActiveCount = activeSnakeCount;
            LayoutCapacity = layoutMaxSnakeCount;

            // 1. Initialize Layout in private field
            _layout = new((ushort)area, queueCapacity, layoutMaxSnakeCount);

            // 2. Size calculation and Allocation
            var totalSize = _layout.SnakeStride.Next * layoutMaxSnakeCount;
            _basePointer = (byte*)NativeMemory.AlignedAlloc(totalSize, Constants.CacheLine);
            NativeMemory.Clear(_basePointer, totalSize);
        }

        // Test Metadata
        public string MapName { get; }
        public int ActiveCount { get; }
        public int LayoutCapacity { get; }

        // Exposure for Assertions (Ref Readonly to avoid copies)
        public ref readonly SlotMemoryLayout Layout => ref _layout;

        public void Dispose()
        {
            if (_disposed) return;

            if (_basePointer != null)
            {
                NativeMemory.AlignedFree(_basePointer);
                _basePointer = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public SnakesSystem Build() => _disposed
            ? throw new ObjectDisposedException(nameof(SnakesSystemTestContext))
            : new SnakesSystem(_basePointer, in _layout, ActiveCount);

        public override string ToString() => $"{MapName} | Active:{ActiveCount} | Layout:{LayoutCapacity}";
    }
}