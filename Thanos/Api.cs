using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Thanos.Model;

namespace Thanos;

public static class Api
{
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

    public static void AddEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => new
        {
            apiversion = "1",
            author = "YourName",
            color = "#FF0000",
            head = "default",
            tail = "default"
        });

        // Move endpoint - IL CORE DELL'APPLICAZIONE
        app.MapPost("/move", (MoveRequest _) => Results.Ok(new
        {
            move = "up", // Logica di movimento qui
            shout = "Hello, world!" // Messaggio opzionale
        }));


        app.MapPost("/move2", async (HttpRequest request) =>
        {
            var moveRequest = await JsonSerializer.DeserializeAsync<MoveRequestOptimized>(
                request.Body,
                new JsonSerializerOptions { Converters = { new MoveRequestOptimizedConverter() } }
            );

            const string move = "up";
            return Results.Ok(new { move, shout = "Indici calcolati alla sorgente!" });
        });


        app.MapPost("/move-ultra", async (HttpRequest request) =>
        {
            // Leggiamo tutto in un buffer pooled per evitare allocazioni
            var buffer = BufferPool.Rent(4096); // 4KB dovrebbe bastare per la maggior parte dei payload

            try
            {
                var totalRead = 0;
                int bytesRead;
                while ((bytesRead = await request.Body.ReadAsync(buffer.AsMemory(totalRead))) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead >= buffer.Length - 1024) // Se quasi pieno, espandi
                    {
                        var newBuffer = BufferPool.Rent(buffer.Length * 2);
                        Array.Copy(buffer, newBuffer, totalRead);
                        BufferPool.Return(buffer);
                        buffer = newBuffer;
                    }
                }

                // Parsing ultra-veloce usando ReadOnlySpan
                var jsonSpan = buffer.AsSpan(0, totalRead);
                var moveRequest = ParseMoveRequestUltraFast(jsonSpan);

                // La tua logica qui...
                const string move = "up";

                return Results.Ok(new { move });
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        });
    }

    public static MoveRequestOptimized ParseMoveRequestUltraFast(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);

        int turn = 0, width = 0, height = 0;
        var foodList = new List<int>(16); // Pre-allocazione
        var hazardList = new List<int>(16);
        var snakesList = new List<SnakeOptimized>(4);
        SnakeOptimized? you = null;

        // Parsing manuale ultra-ottimizzato
        while (reader.Read())
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "turn":
                        turn = reader.GetInt32();
                        break;
                    case "board":
                        ParseBoard(ref reader, ref width, ref height, foodList, hazardList, snakesList);
                        break;
                    case "you":
                        you = ParseSnake(ref reader, width);
                        break;
                }
            }

        return new MoveRequestOptimized
        {
            Turn = turn,
            BoardWidth = width,
            BoardHeight = height,
            FoodIndices = foodList.ToArray(),
            HazardIndices = hazardList.ToArray(),
            Snakes = snakesList.ToArray(),
            You = you!
        };
    }

    private static void ParseBoard(ref Utf8JsonReader reader, ref int width, ref int height,
        List<int> food, List<int> hazards, List<SnakeOptimized> snakes)
    {
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var prop = reader.GetString();
                reader.Read();

                switch (prop)
                {
                    case "width":
                        width = reader.GetInt32();
                        break;
                    case "height":
                        height = reader.GetInt32();
                        break;
                    case "food":
                        ParsePointArray(ref reader, food, width);
                        break;
                    case "hazards":
                        ParsePointArray(ref reader, hazards, width);
                        break;
                    case "snakes":
                        ParseSnakeArray(ref reader, snakes, width);
                        break;
                }
            }
    }

    private static void ParsePointArray(ref Utf8JsonReader reader, List<int> indices, int width)
    {
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                int x = 0, y = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var prop = reader.GetString();
                        reader.Read();
                        if (prop == "x") x = reader.GetInt32();
                        else if (prop == "y") y = reader.GetInt32();
                    }

                indices.Add(y * width + x); // Conversione diretta a indice
            }
    }

    private static void ParseSnakeArray(ref Utf8JsonReader reader, List<SnakeOptimized> snakes, int width)
    {
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            if (reader.TokenType == JsonTokenType.StartObject)
                snakes.Add(ParseSnake(ref reader, width));
    }

    private static SnakeOptimized ParseSnake(ref Utf8JsonReader reader, int width)
    {
        var id = "";
        int health = 0, length = 0;
        var body = new List<int>(20); // Pre-allocazione per il corpo

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var prop = reader.GetString();
                reader.Read();

                switch (prop)
                {
                    case "id":
                        id = reader.GetString()!;
                        break;
                    case "health":
                        health = reader.GetInt32();
                        break;
                    case "length":
                        length = reader.GetInt32();
                        break;
                    case "body":
                        ParsePointArray(ref reader, body, width);
                        break;
                    case "head":
                        // Skip head, lo prendiamo dal primo elemento del body
                        reader.Skip();
                        break;
                }
            }

        return new SnakeOptimized
        {
            Id = id,
            Health = health,
            Length = length,
            BodyIndices = body.ToArray(),
            HeadIndex = body.Count > 0 ? body[0] : 0
        };
    }
}

public class MoveRequestOptimizedConverter : JsonConverter<MoveRequestOptimized>
{
    // Scriviamo solo la logica di lettura, perché il nostro snake non invia mai questo oggetto
    public override void Write(Utf8JsonWriter writer, MoveRequestOptimized value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    // QUESTO È IL CUORE DELLA MAGIA
    public override MoveRequestOptimized? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Usiamo JsonDocument per poter navigare facilmente nel JSON ricevuto
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var board = root.GetProperty("board");
        var width = board.GetProperty("width").GetInt32();
        var height = board.GetProperty("height").GetInt32();

        // Funzione helper per convertire un array di punti JSON in un array di indici
        int[] GetIndices(JsonElement pointsArray)
        {
            var indices = new int[pointsArray.GetArrayLength()];
            var i = 0;
            foreach (var point in pointsArray.EnumerateArray()) indices[i++] = point.GetProperty("y").GetInt32() * width + point.GetProperty("x").GetInt32();
            return indices;
        }

        // Funzione helper per convertire un serpente JSON in SnakeOptimized
        SnakeOptimized ToSnakeOptimized(JsonElement snakeElement)
        {
            var bodyIndices = GetIndices(snakeElement.GetProperty("body"));
            return new SnakeOptimized
            {
                Id = snakeElement.GetProperty("id").GetString()!,
                Health = snakeElement.GetProperty("health").GetInt32(),
                Length = snakeElement.GetProperty("length").GetInt32(),
                BodyIndices = bodyIndices,
                HeadIndex = bodyIndices[0] // La testa è il primo elemento
            };
        }

        var snakes = board.GetProperty("snakes").EnumerateArray().Select(ToSnakeOptimized).ToArray();
        var you = ToSnakeOptimized(root.GetProperty("you"));

        return new MoveRequestOptimized
        {
            Turn = root.GetProperty("turn").GetInt32(),
            BoardWidth = width,
            BoardHeight = height,
            FoodIndices = GetIndices(board.GetProperty("food")),
            HazardIndices = GetIndices(board.GetProperty("hazards")),
            Snakes = snakes,
            You = you
        };
    }
}