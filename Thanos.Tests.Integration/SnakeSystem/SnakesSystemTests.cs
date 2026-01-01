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
        // FIX COMPILATORE:
        // SlotMemoryLayout deve essere un campo (field) per avere un indirizzo di memoria stabile.
        // SnakesSystem (ref struct) memorizzerà un riferimento a QUESTO campo.
        private readonly SlotMemoryLayout _layout;

        private readonly nuint _totalSize;

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

            // 1. Inizializzazione Layout nel campo privato
            _layout = new SlotMemoryLayout((ushort)area, queueCapacity, layoutMaxSnakeCount);

            // 2. Calcolo dimensione e Allocazione
            _totalSize = _layout.SnakeStride.Next * layoutMaxSnakeCount;
            _basePointer = (byte*)NativeMemory.AlignedAlloc(_totalSize, Constants.CacheLine);
            NativeMemory.Clear(_basePointer, _totalSize);
        }

        // Metadata per i Test
        public string MapName { get; }
        public int ActiveCount { get; }
        public int LayoutCapacity { get; }

        // Esposizione per Asserzioni (Ref Readonly per evitare copie)
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

        /// <summary>
        ///     Helper White-box per asserzioni sulla memoria.
        ///     Permette di verificare che i serpenti siano distanziati dallo Stride corretto.
        /// </summary>
        public byte* GetSnakePointer(int index)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SnakesSystemTestContext));

            var offset = (nuint)index * _layout.SnakeStride.Next;
            if (offset >= _totalSize) throw new IndexOutOfRangeException($"Pointer Request OOB: Index {index} exceeds allocated memory.");

            return _basePointer + offset;
        }

        public override string ToString() => $"{MapName} | Active:{ActiveCount} | Layout:{LayoutCapacity}";
    }
}