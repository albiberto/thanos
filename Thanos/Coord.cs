using System.Runtime.InteropServices;

namespace Thanos;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Coord
{
    public byte X;
    public byte Y;
}