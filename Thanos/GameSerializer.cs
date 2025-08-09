using System.Text;
using System.Text.Json;
using Thanos.Parsers;

namespace Thanos;

public static class LowLevelParser
{
    #region Pre-compiled UTF-8 Property Names
    
    private static ReadOnlySpan<byte> GameProp => "game"u8;
    private static ReadOnlySpan<byte> TurnProp => "turn"u8;
    
    #endregion
    
    public static MoveRequest Parse(string json)
    {
        Game game = default;
        var turn = 0;

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(jsonBytes);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals(GameProp))
                {
                    reader.Read(); // vai al valore
                    game = GameParser.Parse(ref reader);
                }
                else if (reader.ValueTextEquals(TurnProp))
                {
                    reader.Read();
                    turn = reader.GetInt32();
                }
                else
                {
                    reader.Skip();
                }
            }
        }

        return new(game, turn, new());
    }
}