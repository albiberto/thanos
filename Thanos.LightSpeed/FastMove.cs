namespace Thanos.LightSpeed;

/// <summary>
/// Bitmask for valid moves.
/// Using byte ensures it fits perfectly in registers without zero-extension overhead.
/// </summary>
public static class FastMoves
{
    public const byte None  = 0;
    public const byte Up    = 1 << 0; // -16
    public const byte Down  = 1 << 1; // +16
    public const byte Left  = 1 << 2; // -1
    public const byte Right = 1 << 3; // +1
}