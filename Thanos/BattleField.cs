using System.Runtime.InteropServices;

namespace Thanos;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Battlefield
{
    // int prevent boxing/unboxing overhead
    private const int MaxSnakes = 8;  // Me + 7 enemies
    private const int TotalSnakesSize = BattleSnake.SnakeSize * MaxSnakes; 
    
    public fixed byte SnakeData[TotalSnakesSize];
    
    public BattleSnake* GetMe()
    {
        fixed (byte* ptr = SnakeData) return (BattleSnake*)ptr;
    }
    
    public BattleSnake* GetSnake(int index)
    {
        fixed (byte* ptr = SnakeData) return (BattleSnake*)(ptr + index * BattleSnake.SnakeSize);
    }
}