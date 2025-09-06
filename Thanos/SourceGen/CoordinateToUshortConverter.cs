using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public class CoordinateToUshortConverter(int gridWidth) : JsonConverter<ushort>
{
    public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token for Coordinate");

        int x = -1, y = -1;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            
            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName.Equals("x", StringComparison.OrdinalIgnoreCase))
            {
                x = reader.GetInt32();
            }
            else if (propertyName.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                y = reader.GetInt32();
            }
        }

        if (x != -1 && y != -1)
        {
            return (ushort)(y * gridWidth + x);
        }

        throw new JsonException("Coordinate object must contain both 'x' and 'y' properties.");
    }

    public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options) => throw new NotImplementedException();
}