using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public readonly struct Coordinate(uint x, uint y)
{
    [JsonPropertyName("x")]
    public uint X { get; } = x;

    [JsonPropertyName("y")]
    public uint Y { get; } = y;
}