namespace Thanos.LightSpeed;

/// <summary>
/// Unifies move semantics and memory offsets.
/// Left: -1 (255), Right: +1, Up: +16, Down: -16 (240)
/// </summary>
public static class LSMoves
{
    public const byte Left = 0;
    public const byte Right = 1;
    public const byte Up = 2;
    public const byte Down = 3;

    // Inline static span. The JIT mounts this in the data section. Zero allocation.
    public static ReadOnlySpan<byte> Offsets => [255, 1, 16, 240];
}