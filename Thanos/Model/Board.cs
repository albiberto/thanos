using Thanos.Domain;

namespace Thanos.Model;

/// <summary>
///     Board state - property names match JSON esatto
/// </summary>
public sealed class Board
{
    public uint height { get; set; }
    public uint width { get; set; }
    public Point[] food { get; set; } = [];
    public Point[] hazards { get; set; } = [];
    public Snake[] snakes { get; set; } = [];
}