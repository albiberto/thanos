using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Hyper;

/// <summary>
/// A perfectly cache-aligned 256-bit board mapping a 16x16 grid.
/// Battlesnake boards (up to 14x14) fit inside with a permanent 1-cell padding (Ghost Border).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct Bitboard256
{
    public fixed ulong Chunks[4]; // 4 * 64 bits = 256 bits (32 bytes)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(byte position)
    {
        Chunks[position >> 6] |= 1UL << (position & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(byte position)
    {
        Chunks[position >> 6] &= ~(1UL << (position & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(byte position)
    {
        return (Chunks[position >> 6] & (1UL << (position & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(byte position)
    {
        return (Chunks[position >> 6] & (1UL << (position & 63))) == 0;
    }

    /// <summary>
    /// Paints the unplayable areas (edges) with 1s (Walls).
    /// For an 11x11 board, valid coordinates are X: 1..11, Y: 1..11.
    /// X=0, Y=0, X=12, Y=12 become permanent walls.
    /// </summary>
    public void InitializeGhostBorders(int actualWidth, int actualHeight)
    {
        Chunks[0] = Chunks[1] = Chunks[2] = Chunks[3] = 0UL;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                // 1-based indexing for the playable area. 
                // Anything outside (0, or > actual dimensions) is a Wall.
                if (x == 0 || x > actualWidth || y == 0 || y > actualHeight)
                {
                    byte pos = (byte)((y << 4) | x); // Equivalent to y * 16 + x
                    Set(pos);
                }
            }
        }
    }
}