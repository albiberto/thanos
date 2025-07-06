using System.Text.Json.Serialization;

namespace Thanos.Model;

/// <summary>
///     Move request DTO - struttura principale per /move endpoint
///     Ottimizzato per deserializzazione ultra-rapida
/// </summary>
public sealed class MoveRequest
{
    [JsonPropertyName("game")] public Game Game { get; set; } = new();

    [JsonPropertyName("turn")] public uint Turn { get; set; }

    [JsonPropertyName("board")] public Board Board { get; set; } = new();

    [JsonPropertyName("you")] public Snake You { get; set; } = new();
}

/// <summary>
///     Response per /move endpoint - usando enum per direzioni
/// </summary>
public sealed class MoveResponse
{
    public MoveResponse(MoveDirection direction, string shout = "")
    {
        Move = direction switch
        {
            MoveDirection.Up => "up",
            MoveDirection.Down => "down",
            MoveDirection.Left => "left",
            MoveDirection.Right => "right",
            _ => "up"
        };
        Shout = shout;
    }

    [JsonPropertyName("move")] public string Move { get; set; } = "up";

    [JsonPropertyName("shout")] public string Shout { get; set; } = string.Empty;
}

/// <summary>
///     Enum per direzioni - performance migliori di string
/// </summary>
public enum MoveDirection : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}