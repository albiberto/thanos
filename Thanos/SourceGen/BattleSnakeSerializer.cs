using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IO;

namespace Thanos.SourceGen;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(Request))]
public partial class BattleSnakeSerializerContext : JsonSerializerContext;

public static class BattleSnakeSerializer
{
    private static readonly BattleSnakeSerializerContext?[] _fastContextCache = new BattleSnakeSerializerContext[32];
    private static readonly RecyclableMemoryStreamManager _streamManager = new();

    static BattleSnakeSerializer()
    {
        _fastContextCache[7] = CreateContext(7);
        _fastContextCache[11] = CreateContext(11);
        _fastContextCache[19] = CreateContext(19);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<Request> ReadRequestAsync(HttpContext httpContext)
    {
        await using var stream = _streamManager.GetStream();
        await httpContext.Request.Body.CopyToAsync(stream, httpContext.RequestAborted);
        
        var sequence = stream.GetReadOnlySequence();

        return Parse(sequence);
    }

    public static Request Parse(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var sequence = new ReadOnlySequence<byte>(bytes);
        return Parse(sequence);
    }

    private static Request Parse(ReadOnlySequence<byte> sequence)
    {
        var width = PeekBoardWidth(sequence);

        var context = GetContextForWidth(width);

        var reader = new Utf8JsonReader(sequence);
        
        return JsonSerializer.Deserialize(ref reader, context.Request);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BattleSnakeSerializerContext GetContextForWidth(int width)
    {
        if (width >= _fastContextCache.Length) return CreateContext(width);

        var ctx = _fastContextCache[width];
        if (ctx is not null) return ctx;

        // Lazy Init: Se arriva una dimensione strana (es. 15), la creiamo e cachiamo.
        return _fastContextCache[width] = CreateContext(width);
    }

    private static BattleSnakeSerializerContext CreateContext(int width)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new CoordinateArrayToUshortArrayConverter(width),
                new CoordinateToUshortConverter(width)
            },
            PropertyNameCaseInsensitive = true
        };

        return new BattleSnakeSerializerContext(options);
    }

    private static int PeekBoardWidth(ReadOnlySequence<byte> sequence)
    {
        var reader = new Utf8JsonReader(sequence);

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("board")) continue;

            reader.Read(); // Entra nell'oggetto board (StartObject)
            
            if (reader.TokenType != JsonTokenType.StartObject) continue;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("width"))
                {
                    reader.Read();
                    return reader.GetInt32();
                }

                reader.Skip();
            }
        }

        throw new JsonException("Could not find 'board.width' property in the JSON.");
    }
}