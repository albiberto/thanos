using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct WarArena
{
    private WarField* _field;
    private WarSnake* _snakes;
    
    public uint LiveSnakesCount;
    
    public static void PlacementNew(WarArena* arena, WarSnake* snakes, WarField* field, uint liveSnakesCount)
    {
        arena->_field = field;
        arena->_snakes = snakes;
        
        arena->LiveSnakesCount = liveSnakesCount;
    }
}