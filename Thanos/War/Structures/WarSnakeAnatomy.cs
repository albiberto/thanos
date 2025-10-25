using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

[StructLayout(LayoutKind.Sequential)]
public ref struct WarSnakeAnatomy
{
    public byte Length { get; private set; }
    public ushort HeadIndex { get; private set; }
    public ushort TailIndex { get; private set; }
    public ushort CapacityMask { get; private set; }
    
    public void Initialize(ushort capacity)
    {
        CapacityMask = (ushort)(capacity - 1);
        Length = 0;
        HeadIndex = 0;
        TailIndex = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementHead()
    {
        HeadIndex = (ushort)((HeadIndex + 1) & CapacityMask);
        Length++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementTail()
    {
        TailIndex = (ushort)((TailIndex + 1) & CapacityMask);
        Length--;
    }
}