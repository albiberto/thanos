using System.Text.Json.Serialization;

namespace Thanos.Tests.Support.Model;

[method: JsonConstructor]
public class BattleSnake(string id, string name, uint health, Coordinate[] body, string latency, Coordinate head, uint length, string shout, Customizations customizations)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public uint Health { get; } = health;
    public IReadOnlyCollection<Coordinate> Body { get; } = body;
    public string Latency { get; } = latency;
    public Coordinate head { get; } = head;
    public uint length { get; } = length;
    public string shout { get; } = shout;
    public Customizations customizations { get; } = customizations;
}

[method: JsonConstructor]
public class Customizations(string color, string head, string tail)
{
    public string Color { get; } = color;
    public string Head { get; } = head;
    public string Tail { get; } = tail;
}