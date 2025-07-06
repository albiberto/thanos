using Thanos.Domain;

namespace Thanos.Model;

/// <summary>
///     Snake data structure - ottimizzato per deserializzazione diretta
/// </summary>
public sealed class Snake
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public uint health { get; set; }
    public Point[] body { get; set; } = [];
    public uint latency { get; set; }
    public Point head { get; set; }
    public uint length { get; set; }
    public string shout { get; set; } = "";
    public string squad { get; set; } = "";
    public SnakeCustomizations customizations { get; set; } = new();
}

/// <summary>
///     Snake customizations - deserializzazione diretta
/// </summary>
public sealed class SnakeCustomizations
{
    public string color { get; set; } = "";
    public string head { get; set; } = "";
    public string tail { get; set; } = "";
}