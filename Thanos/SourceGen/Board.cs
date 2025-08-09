using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public readonly struct Board(uint height, uint width, Coordinate[] food, Coordinate[] hazards, BattleSnake[] snakes)
{
    [JsonPropertyName("height")]
    public uint Height { get; } = height;

    [JsonPropertyName("width")]
    public uint Width { get; } = width;
    
    [JsonPropertyName("food")]
    public IReadOnlyCollection<Coordinate> Food { get; } = food;

    [JsonPropertyName("hazards")]
    public IReadOnlyCollection<Coordinate> Hazards { get; } = hazards;

    [JsonPropertyName("snakes")]
    public IReadOnlyCollection<BattleSnake> Snakes { get; } = snakes;
}