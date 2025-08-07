using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Tests;

// The source parameterizes the test fixture.
[TestFixtureSource(nameof(CapacitiesToTest))]
public unsafe partial class BattleSnakeTests(int capacity)
{
    // Step 1: Define the different capacities you want to run all tests against.
    public static readonly int[] CapacitiesToTest = [16, 32, 64, 128, 256, 512, 1024, 2048, 4096];

    private const byte EmptyCell = 0; // Health, Length, Capacity, HeadIndex, TailIndex, Head, Tail
    
    private byte* _memory = null;
    private BattleSnake* _sut = null;
    
    [SetUp]
    public void SetUp()
    {
        var _snakeStride = BattleSnake.HeaderSize + capacity * sizeof(ushort);
        _memory = (byte*)NativeMemory.AlignedAlloc((nuint)_snakeStride, 64);
        _sut = (BattleSnake*)_memory;
        
        Unsafe.InitBlockUnaligned(_memory, EmptyCell, (uint)_snakeStride);
    }
    
    [TearDown]
    public void TearDown()
    {
        if (_memory == null) return;
        
        NativeMemory.AlignedFree(_memory);
        _memory = null;
    }
}