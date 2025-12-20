using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public sealed class CoordinateToUshortConverter(int gridWidth) : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

        int x = -1, y = -1;
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            
            if (reader.ValueTextEquals("x"))
            {
                reader.Read();
                x = reader.GetInt32();
            }
            else if (reader.ValueTextEquals("y"))
            {
                reader.Read();
                y = reader.GetInt32();
            }
            else
            {
                reader.Skip();
            }
        }

        return (ushort)(y * gridWidth + x);
    }

    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options) => throw new NotImplementedException();
}