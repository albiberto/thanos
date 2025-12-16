using System.Runtime.InteropServices;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.HotPath;

[TestFixture]
public class BitboardTests
{
    // Usiamo memoria nativa per garantire l'allineamento durante i test.
    // Questo è necessario perché Vector128.Load richiede memoria allineata, 
    // altrimenti crasha su alcune architetture o è lento.
    private IntPtr _memoryBlock;
    private const int AllocationSize = 64; 

    [SetUp]
    public void Setup()
    {
        _memoryBlock = Marshal.AllocHGlobal(AllocationSize);
        unsafe { new Span<byte>(_memoryBlock.ToPointer(), AllocationSize).Clear(); }
    }

    [TearDown]
    public void TearDown()
    {
        Marshal.FreeHGlobal(_memoryBlock);
    }

    [Test]
    public unsafe void Set_Should_MarkSpecificBit_AsTrue()
    {
        // Arrange: Simuliamo uno span allineato
        var span = new Span<byte>(_memoryBlock.ToPointer(), 16); // 128 bit
        var bitboard = new Bitboard(span);

        // Act
        bitboard.Set(0);   // Primo bit
        bitboard.Set(10);  // Bit intermedio
        bitboard.Set(120); // Ultimo bit (quasi)

        // Assert
            That(bitboard.IsSet(0), Is.True, "Bit 0 should be set");
            That(bitboard.IsSet(10), Is.True, "Bit 10 should be set");
            That(bitboard.IsSet(120), Is.True, "Bit 120 should be set");
            
            That(bitboard.IsSet(1), Is.False, "Bit 1 should NOT be set");
            That(bitboard.IsSet(121), Is.False, "Bit 121 should NOT be set");
    }

    [Test]
    public unsafe void Clear_Should_ResetAllBits_ToZero()
    {
        var span = new Span<byte>(_memoryBlock.ToPointer(), 16);
        var bitboard = new Bitboard(span);
        
        bitboard.Set(42);
        bitboard.Set(99);
        
        // Act
        bitboard.Clear();

        // Assert
            That(bitboard.IsSet(42), Is.False, "Bit 42 should be cleared");
            That(bitboard.IsSet(99), Is.False, "Bit 99 should be cleared");
            That(bitboard.PopCount(), Is.EqualTo(0), "PopCount should be 0");
    }

    [Test]
    public unsafe void Or_Should_MergeTwoBitboards()
    {
        // Arrange: Creiamo due bitboard in memoria adiacente ma distinta
        var ptr1 = _memoryBlock;
        var ptr2 = IntPtr.Add(_memoryBlock, 16); // +16 bytes offset

        var bb1 = new Bitboard(new Span<byte>(ptr1.ToPointer(), 16));
        var bb2 = new Bitboard(new Span<byte>(ptr2.ToPointer(), 16));

        bb1.Set(10);
        bb2.Set(20);

        // Act: bb1 = bb1 | bb2
        bb1.Or(in bb2);

        // Assert
            That(bb1.IsSet(10), Is.True, "Original bit in bb1 should persist");
            That(bb1.IsSet(20), Is.True, "Bit from bb2 should be merged into bb1");
            That(bb2.IsSet(10), Is.False, "Source bb2 should not be modified");
    }
    
    [Test]
    public unsafe void PopCount_Should_CountActiveBits_Correctly()
    {
        var span = new Span<byte>(_memoryBlock.ToPointer(), 16);
        var bitboard = new Bitboard(span);

        bitboard.Set(0);
        bitboard.Set(63);  // Fine primo ulong
        bitboard.Set(64);  // Inizio secondo ulong

        var count = bitboard.PopCount();

        That(count, Is.EqualTo(3), "Should count exactly 3 set bits across boundaries");
    }
}