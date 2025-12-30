using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CircularQueueState
{
    // 3 Byte esatti.
    // Max Capacity gestibile: 256 (per via degli indici byte)
    // Max Length logica: 255 (saturata)
    public byte Length;
    public byte HeadIndex;
    public byte TailIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Length = 0;
        HeadIndex = 0;
        TailIndex = 0;
    }
}