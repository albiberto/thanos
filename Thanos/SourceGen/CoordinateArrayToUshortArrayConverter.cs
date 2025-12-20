using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public sealed class CoordinateArrayToUshortArrayConverter(int gridWidth) : JsonConverter<ushort[]>
{
    public override ushort[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        
        var checkpoint = reader;
        var count = 0;
        
        // 1. Fast Scan for Count
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.StartObject) continue;
            
            count++;
            reader.Skip();
        }
        
        if (count == 0) return [];

        // 2. Allocate Exact Size
        var result = new ushort[count];
        
        // 3. Reset & Parse
        reader = checkpoint;
        var idx = 0;
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;

            if (reader.TokenType != JsonTokenType.StartObject) continue;

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
                    reader.Read(); // skip value
                }
            }
                
            // Map & Store
            result[idx++] = (ushort)(y * gridWidth + x);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, ushort[] value, JsonSerializerOptions options) => throw new NotImplementedException();
}