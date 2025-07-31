using System.Text.Json.Serialization;

namespace Thanos.Tests.Support.Model;

[method: JsonConstructor]
public class Coordinate(uint x, uint y)
{
    public uint X { get; } = x;
    public uint Y { get; } = y;
}