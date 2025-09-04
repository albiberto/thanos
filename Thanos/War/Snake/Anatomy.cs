using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public struct Anatomy
{
    public ushort TailIndex { get; private set; }
    public ushort Length { get; private set; }

    public void PlacementNew(ushort startLength)
    {
        TailIndex = 0;
        Length = startLength;
    }
    
    // Metodo chiamato quando lo snake cresce
    public void UpdateAfterGrow() => Length++;

    // Metodo chiamato quando lo snake si muove senza crescere
    public void UpdateAfterMove(int capacity)
    {
        TailIndex = (ushort)((TailIndex + 1) & (capacity - 1));
    }
}