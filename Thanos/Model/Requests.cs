using System.Text.Json.Serialization;

namespace Thanos.Model;

/// <summary>
///     Move request DTO - struttura principale per /move endpoint
///     Ottimizzato per deserializzazione ultra-rapida
/// </summary>
public sealed class MoveRequest
{
    public Game game { get; set; } = new();

    public uint turn { get; set; }
    [JsonPropertyName("board")] public Board Board { get; set; } = new();

    [JsonPropertyName("you")] public Snake You { get; set; } = new();
}