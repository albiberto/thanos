using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Request))]
public class Request(Game game, int turn, Board board, BattleSnake you)
{
    [JsonPropertyName("game")]
    public Game Game { get; } = game;
    
    [JsonPropertyName("turn")]
    public int Turn { get; } = turn;
    
    [JsonPropertyName("board")]
    public Board Board { get; } = board;
    
    [JsonPropertyName("you")]
    public BattleSnake You { get; } = you;
}
