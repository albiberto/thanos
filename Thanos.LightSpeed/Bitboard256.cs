using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Thanos.LightSpeed;

/// <summary>
/// A perfectly cache-aligned 256-bit board mapping a 16x16 grid.
/// Packed to 32 bytes for AVX2 Vectorization.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 32)]
public unsafe struct Bitboard256
{
    public fixed ulong Chunks[4]; // 256 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(byte position) => Chunks[position >> 6] |= 1UL << (position & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(byte position) => Chunks[position >> 6] &= ~(1UL << (position & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(byte position) => (Chunks[position >> 6] & (1UL << (position & 63))) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(byte position) => (Chunks[position >> 6] & (1UL << (position & 63))) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (Vector256.IsHardwareAccelerated)
        {
            Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref Chunks[0]), Vector256<byte>.Zero);
        }
        else
        {
            Chunks[0] = Chunks[1] = Chunks[2] = Chunks[3] = 0UL;
        }
    }

    /// <summary>
    /// SIMD Optimized AndNot: this = this & ~other
    /// Used for O(1) Snake Kills.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AndNot(ref Bitboard256 other)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            ref byte a = ref Unsafe.As<ulong, byte>(ref Chunks[0]);
            ref byte b = ref Unsafe.As<ulong, byte>(ref other.Chunks[0]);
            
            var vecA = Unsafe.ReadUnaligned<Vector256<byte>>(ref a);
            var vecB = Unsafe.ReadUnaligned<Vector256<byte>>(ref b);
            
            // AndNot: (~vecB) & vecA
            Unsafe.WriteUnaligned(ref a, Vector256.AndNot(vecA, vecB));
        }
        else
        {
            Chunks[0] &= ~other.Chunks[0];
            Chunks[1] &= ~other.Chunks[1];
            Chunks[2] &= ~other.Chunks[2];
            Chunks[3] &= ~other.Chunks[3];
        }
    }

    public void InitializeGhostBorders(int actualWidth, int actualHeight)
    {
        Clear();
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                if (x == 0 || x > actualWidth || y == 0 || y > actualHeight)
                {
                    Set((byte)((y << 4) | x));
                }
            }
        }
    }
}