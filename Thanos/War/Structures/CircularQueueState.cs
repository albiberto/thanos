using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

[StructLayout(LayoutKind.Sequential)]
public struct CircularQueueState
{
    public byte Length { get; private set; }
    public ushort HeadIndex { get; private set; }
    public ushort TailIndex { get; private set; }
    public ushort WrapMask { get; private set; }

    public void PlacementNew(ushort capacity) => WrapMask = (ushort)(capacity - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceHead()
    {
        HeadIndex = (ushort)((HeadIndex + 1) & WrapMask);
        Length++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTail()
    {
        TailIndex = (ushort)((TailIndex + 1) & WrapMask);
        Length--;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Length = 0;
        HeadIndex = 0;
        TailIndex = 0;
    }
}