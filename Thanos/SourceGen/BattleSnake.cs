using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public readonly struct BattleSnake(string id, string name, uint health, Coordinate[] body, string latency, Coordinate head, uint length, string shout, Customizations customizations)
{
    [JsonPropertyName("id")]
    public string Id { get; } = id;

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("health")]
    public uint Health { get; } = health;

    [JsonPropertyName("body")]
    public IReadOnlyCollection<Coordinate> Body { get; } = body;

    [JsonPropertyName("latency")]
    public string Latency { get; } = latency;

    [JsonPropertyName("head")]
    public Coordinate Head { get; } = head;

    [JsonPropertyName("length")]
    public uint Length { get; } = length;

    [JsonPropertyName("shout")]
    public string Shout { get; } = shout;

    [JsonPropertyName("customizations")]
    public Customizations Customizations { get; } = customizations;
}

public readonly struct Customizations(string color, string head, string tail)
{
    [JsonPropertyName("color")]
    public string Color { get; } = color;

    [JsonPropertyName("head")]
    public string Head { get; } = head;

    [JsonPropertyName("tail")]
    public string Tail { get; } = tail;
}