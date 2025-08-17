namespace Thanos.MCST;

public static class Moves
{
    public const byte None = 0;   // 0000
    public const byte Up = 1;     // 0001
    public const byte Down = 2;   // 0010
    public const byte Left = 4;   // 0100
    public const byte Right = 8;  // 1000
    
    public static readonly byte[] AllDirections = [Up, Down, Left, Right];
}