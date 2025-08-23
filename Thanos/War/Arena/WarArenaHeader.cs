using System.Runtime.InteropServices;

namespace Thanos.War.Arena;

[StructLayout(LayoutKind.Sequential)]
public struct WarArenaHeader(int liveSnakesCount)
{
    public readonly int LiveSnakesCount;
    public long Hash { get; private set; }
    
    /// <summary>
    /// Calculates the initial Zobrist hash for the entire game state.
    /// </summary>
     public void InitializeHash()
     {
        Hash = 0;
     }
}