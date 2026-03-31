using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Hyper;

/// <summary>
/// A zero-allocation, lock-free snake representation.
/// The circular buffer automatically wraps around because byte overflows exactly at 255.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct HyperSnake
{
    // The exact positions of the body parts
    public fixed byte Body[256];
    
    // Auto-wrapping pointers
    public byte HeadPointer;
    public byte TailPointer;
    
    public byte Length;
    public byte Health;
    public byte PendingGrowth;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetHead() => Body[HeadPointer];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetTail() => Body[TailPointer];

    /// <summary>
    /// Advances the tail and removes it from the obstacles bitboard.
    /// Call this BEFORE calculating valid moves if the snake is not eating.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTail(ref Bitboard256 obstacles)
    {
        if (PendingGrowth > 0)
        {
            PendingGrowth--;
            return; // Tail doesn't move
        }

        byte tailPos = Body[TailPointer];
        obstacles.Unset(tailPos);
        
        // Overflow 255 -> 0 is handled implicitly by the byte type. Free modulo!
        TailPointer = unchecked((byte)(TailPointer + 1));
    }

    /// <summary>
    /// Updates the head pointer and registers the new position on the obstacles bitboard.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceHead(ref Bitboard256 obstacles, byte newHeadPos)
    {
        HeadPointer = unchecked((byte)(HeadPointer + 1));
        Body[HeadPointer] = newHeadPos;
        obstacles.Set(newHeadPos);
    }
}