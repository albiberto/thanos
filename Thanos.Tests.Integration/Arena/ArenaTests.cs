using Thanos.Memory;

namespace Thanos.Tests.Integration.Arena;

[TestFixture]
public partial class ArenaTests
{
    private SlotMemoryLayout _layout;
    private LookupsMemoryPool _lookups;
    private const ushort DefaultArea = 121;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _lookups = LookupsMemoryPool.Medium;
        _layout = new(Constants.Medium.Width, Constants.Medium.Height, Constants.MaxSnakesCount);
    }

    /// <summary>
    /// Fornisce un'Arena pulita e isolata per ogni test.
    /// </summary>
    public unsafe class ArenaTestContext : IDisposable
    {
        private readonly SlotMemoryPool _pool;
        private readonly int _slotIndex;

        public ArenaTestContext(LookupsMemoryPool lookups, SlotMemoryLayout layout)
        {
            _pool = new(1, 0, Constants.MaxSnakesCount, lookups, layout);
            _slotIndex = _pool.Allocate();
        }

        public War.Arena Arena => _pool.GetArena(_slotIndex);

        public void Dispose() => _pool.Dispose();
    }
}