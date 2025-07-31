using System.Text.Json.Serialization;

namespace Thanos.Tests.Support.Model;

[method: JsonConstructor]
public class Board(uint height, uint width, Coordinate[] food, Coordinate[] hazards, BattleSnake[] snakes)
{
    public uint Height { get; } = height;
    public uint Width { get; } = width;
    public IReadOnlyCollection<Coordinate> Food { get; } = food;
    public IReadOnlyCollection<Coordinate> Hazards { get; } = hazards;
    public IReadOnlyCollection<BattleSnake> Snakes { get; } = snakes;
}