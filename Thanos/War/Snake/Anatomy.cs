using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public ref struct Anatomy
{
    public ushort TailIndex { get; private set; }
    public ushort Length { get; private set; }

    public void PlacementNew(ushort length)
    {
        TailIndex = 0;
        Length = length;
    }
    
    public void UpdateAfterGrow() => Length++;

    public void UpdateAfterMove(int capacity) => TailIndex = (ushort)((TailIndex + 1) & (capacity - 1));
}