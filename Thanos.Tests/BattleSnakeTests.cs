using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Tests;

[TestFixture]
public unsafe partial class BattleSnakeTests
{
    private const byte EmptyCell = 0; // Health, Length, Capacity, HeadIndex, TailIndex, Head, Tail
    private const int Capacity = 256; 
    private const int SnakeStride = BattleSnake.HeaderSize + Capacity * sizeof(ushort);
    
    private byte* _memory = null;
    private BattleSnake* _sut = null;
    
    [SetUp]
    public void SetUp()
    {
        _memory = (byte*)NativeMemory.AlignedAlloc(SnakeStride, 64);
        Unsafe.InitBlockUnaligned(_memory, EmptyCell, SnakeStride);
        _sut = (BattleSnake*)_memory;
    }
    
    [TearDown]
    public void TearDown()
    {
        if (_memory == null) return;
        
        NativeMemory.AlignedFree(_memory);
        _memory = null;
    }
}