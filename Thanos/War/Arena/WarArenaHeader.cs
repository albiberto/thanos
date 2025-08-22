using System.Runtime.InteropServices;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public struct WarArenaHeader
{
    public int LiveSnakesCount;
    public long Hash;
}