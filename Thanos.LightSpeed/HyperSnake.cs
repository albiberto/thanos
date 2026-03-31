using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.LightSpeed;

[StructLayout(LayoutKind.Sequential, Pack = 32)]
public unsafe struct HyperSnake
{
    public byte StackedSegments; // > 0 nei primi turni
    public Bitboard256 BodyMask; // SIMD Friendly tracking
    
    public fixed byte Body[256];
    
    public byte HeadPointer;
    public byte TailPointer;
    
    public byte Length;
    public byte Health;
    public byte PendingGrowth;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetHead() => Body[HeadPointer];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetTail() => Body[TailPointer];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceHead(ref Bitboard256 obstacles, byte newHeadPos)
    {
        HeadPointer = unchecked((byte)(HeadPointer + 1));
        Body[HeadPointer] = newHeadPos;
        obstacles.Set(newHeadPos);
        BodyMask.Set(newHeadPos);
    }
}