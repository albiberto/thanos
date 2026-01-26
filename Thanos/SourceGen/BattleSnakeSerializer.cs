using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Request))]
public partial class BattleSnakeSerializerContext : JsonSerializerContext;

public static class BattleSnakeSerializer
{
    private static readonly BattleSnakeSerializerContext?[] FastContextCache = new BattleSnakeSerializerContext[32];

    static BattleSnakeSerializer()
    {
        // Pre-warm common sizes
        FastContextCache[7] = CreateContext(7);   // Small
        FastContextCache[11] = CreateContext(11); // Standard
        FastContextCache[19] = CreateContext(19); // Large
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<Request> ReadRequestAsync(HttpContext httpContext)
    {
        var reader = httpContext.Request.BodyReader;
        var result = await reader.ReadAsync(httpContext.RequestAborted);
        var buffer = result.Buffer;

        try
        {
            return Parse(buffer);
        }
        finally
        {
            reader.AdvanceTo(buffer.End);
        }
    }

    public static Request Parse(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return Parse(new ReadOnlySequence<byte>(bytes));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Request Parse(ReadOnlySequence<byte> sequence)
    {
        // 1. Peek Width (necessario per selezionare il converter corretto)
        var width = PeekBoardWidth(sequence);

        // 2. Select Context (Cached)
        var context = GetContextForWidth(width);

        // 3. Deserialize directly from sequence (System.Text.Json supporta ReadOnlySequence)
        var reader = new Utf8JsonReader(sequence);
        return JsonSerializer.Deserialize(ref reader, context.Request)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BattleSnakeSerializerContext GetContextForWidth(int width)
    {
        if (width >= FastContextCache.Length) return CreateContext(width);
        
        var ctx = FastContextCache[width];
        if (ctx is not null) return ctx;
        
        // Lazy init per dimensioni non standard ma piccole
        return FastContextCache[width] = CreateContext(width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BattleSnakeSerializerContext CreateContext(int width)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new CoordinateArrayToUshortArrayConverter(width),
                new CoordinateToUshortConverter(width)
            },
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = BattleSnakeSerializerContext.Default
        };

        return new BattleSnakeSerializerContext(options);
    }

    private static int PeekBoardWidth(ReadOnlySequence<byte> sequence)
    {
        var reader = new Utf8JsonReader(sequence);
        var depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    depth++;
                    break;
                case JsonTokenType.EndObject:
                    depth--;
                    break;
                case JsonTokenType.PropertyName:
                    if (depth == 2 && reader.ValueTextEquals("width"))
                    {
                        if (reader.Read() && reader.TokenType == JsonTokenType.Number)
                        {
                            return reader.GetInt32();
                        }
                    }
                    break;
            }
        }
        
        return 11;
    }
}