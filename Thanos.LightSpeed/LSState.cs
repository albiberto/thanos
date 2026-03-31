using System.Runtime.CompilerServices;

namespace Thanos.LightSpeed;

/// <summary>
/// The entire game state packed into contiguous memory.
/// Can be passed by 'ref' without any heap allocation.
/// </summary>
public struct LSState
{
    public LSBitboard Obstacles;
    public LSBitboard Food;
    
    // For a 4-player game. Array overhead removed by manual unrolling.
    public LSSnake Snake0;
    public LSSnake Snake1;
    public LSSnake Snake2;
    public LSSnake Snake3;

    public int AliveCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize11x11(int initialAliveCount)
    {
        Obstacles = PrecomputedBoards.Border11x11;
        Food.Clear(); // VMOVDQU nativo, 1 istruzione CPU
        AliveCount = initialAliveCount;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Initialize(int width, int height)
    {
        // Paint the borders
        Obstacles.InitializeGhostBorders(width, height);
        
        // Clear food
        Food.Clear();
    }
}