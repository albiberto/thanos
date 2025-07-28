using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Kestrel optimization for low latency
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = null;
    options.Limits.MaxConcurrentUpgradedConnections = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
    
    // Enable synchronous IO for our ultra-fast parser
    options.AllowSynchronousIO = true;
});

// Disable logging in production
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
}

var app = builder.Build();

// Initialize game manager at startup
unsafe
{
    GameManager.Initialize();
}

// Info endpoint
app.MapGet("/", () => Results.Json(new
{
    apiversion = "1",
    author = "Thanos",
    color = "#FF0000",
    head = "pixel",
    tail = "pixel",
    version = "1.0.0"
}));

// Start endpoint
app.MapPost("/start", () => Results.Ok());

// End endpoint
app.MapPost("/end", () => Results.Ok());

// Move endpoint - Corretto per separare async da unsafe
app.MapPost("/move", async (HttpContext context) =>
{
    // --- Step 1: Fase ASINCRONA (fuori da unsafe) ---
    // Leggi il body della richiesta. L'operazione di I/O viene attesa qui.
    var readResult = await context.Request.BodyReader.ReadAsync();
    var buffer = readResult.Buffer;

    Direction bestMove;

    // --- Step 2: Fase SINCRONA e UNSAFE ---
    // Ora che i dati sono in memoria, possiamo usare un blocco unsafe per l'elaborazione.
    unsafe
    {
        // Chiama il parser con la sequenza di byte.
        BattlesnakeParser.ParseFromSpan(buffer.FirstSpan, GameManager.State);
        
        // Chiedi al GameManager di trovare la mossa migliore.
        // Tutta questa logica è sincrona e usa i puntatori.
        bestMove = GameManager.FindBestMove();
    }
    
    // Comunica al BodyReader che abbiamo finito con il buffer.
    context.Request.BodyReader.AdvanceTo(buffer.End);

    // --- Step 3: Fase ASINCRONA (fuori da unsafe) ---
    // Scrivi la risposta. L'operazione di I/O viene attesa qui.
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(BattlesnakeParser.GetMoveResponse(bestMove));
});

app.Run("http://0.0.0.0:8080");

// Inline JSON Parser
public static unsafe class BattlesnakeParser
{
    private static ReadOnlySpan<byte> TurnToken => "turn"u8;
    private static ReadOnlySpan<byte> BoardToken => "board"u8;
    private static ReadOnlySpan<byte> WidthToken => "width"u8;
    private static ReadOnlySpan<byte> HeightToken => "height"u8;
    private static ReadOnlySpan<byte> SnakesToken => "snakes"u8;
    private static ReadOnlySpan<byte> FoodToken => "food"u8;
    private static ReadOnlySpan<byte> HazardsToken => "hazards"u8;
    private static ReadOnlySpan<byte> IdToken => "id"u8;
    private static ReadOnlySpan<byte> HeadToken => "head"u8;
    private static ReadOnlySpan<byte> BodyToken => "body"u8;
    private static ReadOnlySpan<byte> HealthToken => "health"u8;
    private static ReadOnlySpan<byte> XToken => "x"u8;
    private static ReadOnlySpan<byte> YToken => "y"u8;
    private static ReadOnlySpan<byte> YouToken => "you"u8;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ParseFromSpan(ReadOnlySpan<byte> data, GameState* state)
    {
        state->SnakeCount = 0;
        state->FoodCount = 0;
        state->HazardCount = 0;
        
        var reader = new Utf8JsonReader(data, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
        
        Span<byte> youIdBuffer = stackalloc byte[64];
        var youId = youIdBuffer.Slice(0, 0);
        ushort bodyOffset = 0;
        byte snakeIndex = 0;
        Snake* currentSnake = null;
        bool inBoard = false;
        bool inSnakes = false;
        bool inFood = false;
        bool inHazards = false;
        bool inYou = false;
        bool inBody = false;
        bool inHead = false;
        int depth = 0;
        byte pendingX = 0;
        bool hasX = false;
        
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    {
                        var prop = reader.ValueSpan;
                        
                        if (prop.SequenceEqual(TurnToken))
                        {
                            reader.Read();
                            state->Turn = (ushort)reader.GetInt32();
                        }
                        else if (prop.SequenceEqual(BoardToken))
                        {
                            inBoard = true;
                        }
                        else if (prop.SequenceEqual(YouToken))
                        {
                            inYou = true;
                        }
                        else if (inBoard)
                        {
                            if (prop.SequenceEqual(WidthToken))
                            {
                                reader.Read();
                                state->Width = (byte)reader.GetInt32();
                            }
                            else if (prop.SequenceEqual(HeightToken))
                            {
                                reader.Read();
                                state->Height = (byte)reader.GetInt32();
                                state->TotalCells = (ushort)(state->Width * state->Height);
                            }
                            else if (prop.SequenceEqual(SnakesToken))
                            {
                                inSnakes = true;
                            }
                            else if (prop.SequenceEqual(FoodToken))
                            {
                                inFood = true;
                            }
                            else if (prop.SequenceEqual(HazardsToken))
                            {
                                inHazards = true;
                            }
                        }
                        else if (inYou && prop.SequenceEqual(IdToken))
                        {
                            reader.Read();
                            var idSpan = reader.ValueSpan;
                            youId = youIdBuffer.Slice(0, idSpan.Length);
                            idSpan.CopyTo(youId);
                        }
                        else if (currentSnake != null)
                        {
                            if (prop.SequenceEqual(IdToken))
                            {
                                reader.Read();
                                var idSpan = reader.ValueSpan;
                                if (!youId.IsEmpty && idSpan.SequenceEqual(youId))
                                {
                                    state->YouIndex = (byte)(snakeIndex - 1);
                                }
                            }
                            else if (prop.SequenceEqual(HeadToken))
                            {
                                inHead = true;
                            }
                            else if (prop.SequenceEqual(HealthToken))
                            {
                                reader.Read();
                                currentSnake->Health = (byte)reader.GetInt32();
                            }
                            else if (prop.SequenceEqual(BodyToken))
                            {
                                inBody = true;
                            }
                        }
                        else if ((inFood || inHazards || inBody || inHead) && prop.SequenceEqual(XToken))
                        {
                            reader.Read();
                            pendingX = (byte)reader.GetInt32();
                            hasX = true;
                        }
                        else if ((inFood || inHazards || inBody || inHead) && prop.SequenceEqual(YToken) && hasX)
                        {
                            reader.Read();
                            var y = (byte)reader.GetInt32();
                            var pos = GridMath.ToIndex(pendingX, y, state->Width);
                            
                            if (inHead && currentSnake != null)
                            {
                                currentSnake->Head = pos;
                            }
                            else if (inBody && currentSnake != null)
                            {
                                currentSnake->Body[currentSnake->Length++] = pos;
                                currentSnake->BodyHash ^= (uint)pos;
                            }
                            else if (inFood && state->FoodCount < 200)
                            {
                                state->FoodPositions[state->FoodCount++] = pos;
                            }
                            else if (inHazards && state->HazardCount < 255)
                            {
                                state->HazardPositions[state->HazardCount++] = pos;
                            }
                            hasX = false;
                        }
                    }
                    break;
                    
                case JsonTokenType.StartObject:
                    depth++;
                    if (inSnakes && depth == 3 && snakeIndex < 4)
                    {
                        currentSnake = &state->Snakes[snakeIndex];
                        currentSnake->Body = state->SnakeBodies + bodyOffset;
                        currentSnake->BodyHash = 0;
                        currentSnake->Length = 0;
                        snakeIndex++;
                        state->SnakeCount = snakeIndex;
                    }
                    else if ((inFood || inHazards || inBody) && !inHead)
                    {
                        hasX = false;
                    }
                    break;
                    
                case JsonTokenType.EndObject:
                    depth--;
                    if (currentSnake != null && depth == 2)
                    {
                        bodyOffset += currentSnake->Length;
                        currentSnake = null;
                    }
                    else if (inHead && depth == 3)
                    {
                        inHead = false;
                    }
                    else if (inBoard && depth == 0)
                    {
                        inBoard = false;
                    }
                    else if (inYou && depth == 0)
                    {
                        inYou = false;
                    }
                    break;
                    
                case JsonTokenType.StartArray:
                    break;
                    
                case JsonTokenType.EndArray:
                    if (inBody) inBody = false;
                    else if (inSnakes) { inSnakes = false; currentSnake = null; }
                    else if (inFood) inFood = false;
                    else if (inHazards) inHazards = false;
                    break;
            }
        }
    }

    public static string GetMoveResponse(Direction direction)
    {
        var move = direction switch
        {
            Direction.Up => "up",
            Direction.Down => "down",
            Direction.Left => "left",
            Direction.Right => "right",
            _ => "up"
        };
        
        return $$"""{"move":"{{move}}"}""";
    }
}