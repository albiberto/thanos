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

// Move endpoint - the heart of the system
app.MapPost("/move", (HttpContext context) =>
{
    unsafe
    {
        var state = GameManager.State;
        
        // Read request body synchronously
        using var reader = new StreamReader(context.Request.Body);
        var json = reader.ReadToEnd();
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        
        BattlesnakeParser.ParseFromSpan(bytes, state);
        
        // Compute optimal move
        var direction = ComputeOptimalMove(state);
        
        // Write response
        context.Response.ContentType = "application/json";
        context.Response.WriteAsync(BattlesnakeParser.GetMoveResponse(direction));
    }
});

app.Run("http://0.0.0.0:8080");

// AI Logic
[MethodImpl(MethodImplOptions.AggressiveOptimization)]
unsafe Direction ComputeOptimalMove(GameState* state)
{
    Span<ushort> neighbors = stackalloc ushort[4];
    Span<int> scores = stackalloc int[4];
    
    ref var you = ref state->You();
    Movement.GetNeighbors(you.Head, state->Width, state->Height, neighbors);
    
    for (int i = 0; i < 4; i++)
    {
        scores[i] = EvaluatePosition(state, neighbors[i], (Direction)i);
    }
    
    int bestScore = int.MinValue;
    Direction bestMove = Direction.Up;
    
    for (int i = 0; i < 4; i++)
    {
        if (scores[i] > bestScore)
        {
            bestScore = scores[i];
            bestMove = (Direction)i;
        }
    }
    
    return bestMove;
}

[MethodImpl(MethodImplOptions.AggressiveOptimization)]
unsafe int EvaluatePosition(GameState* state, ushort position, Direction dir)
{
    const int DEATH_PENALTY = -10000;
    const int FOOD_BONUS = 100;
    const int SPACE_BONUS = 10;
    
    if (!Movement.IsValid(position))
        return DEATH_PENALTY;
    
    // Check collisions
    for (int i = 0; i < state->SnakeCount; i++)
    {
        ref var snake = ref state->Snakes[i];
        
        if (position == snake.Head && i != state->YouIndex)
        {
            if (snake.Length >= state->You().Length)
                return DEATH_PENALTY;
        }
        
        for (int j = 0; j < snake.Length - 1; j++)
        {
            if (snake.Body[j] == position)
                return DEATH_PENALTY;
        }
    }
    
    int score = 0;
    
    // Food proximity
    if (state->FoodCount > 0 && state->You().Health < 50)
    {
        var (px, py) = GridMath.ToCoords(position, state->Width);
        int minFoodDist = int.MaxValue;
        
        for (int i = 0; i < state->FoodCount; i++)
        {
            var (fx, fy) = GridMath.ToCoords(state->FoodPositions[i], state->Width);
            int dist = Math.Abs(fx - px) + Math.Abs(fy - py);
            minFoodDist = Math.Min(minFoodDist, dist);
        }
        
        score += FOOD_BONUS / (minFoodDist + 1);
    }
    
    // Space control
    score += CountReachableSpaces(state, position) * SPACE_BONUS;
    
    // Center control
    var (x, y) = GridMath.ToCoords(position, state->Width);
    int centerDist = Math.Abs(x - state->Width/2) + Math.Abs(y - state->Height/2);
    score += (state->Width + state->Height) / 2 - centerDist;
    
    return score;
}

[MethodImpl(MethodImplOptions.AggressiveOptimization)]
unsafe int CountReachableSpaces(GameState* state, ushort startPos)
{
    Span<ulong> visited = stackalloc ulong[(state->TotalCells + 63) / 64];
    visited.Clear();
    
    for (int i = 0; i < state->SnakeCount; i++)
    {
        ref var snake = ref state->Snakes[i];
        for (int j = 0; j < snake.Length - 1; j++)
        {
            SetBit(visited, snake.Body[j]);
        }
    }
    
    Span<ushort> queue = stackalloc ushort[128];
    int head = 0, tail = 0;
    int count = 0;
    
    queue[tail++] = startPos;
    SetBit(visited, startPos);
    
    Span<ushort> neighbors = stackalloc ushort[4];
    
    while (head < tail && count < 30)
    {
        var current = queue[head++];
        count++;
        
        Movement.GetNeighbors(current, state->Width, state->Height, neighbors);
        
        for (int i = 0; i < 4; i++)
        {
            if (Movement.IsValid(neighbors[i]) && !GetBit(visited, neighbors[i]))
            {
                SetBit(visited, neighbors[i]);
                if (tail < queue.Length)
                    queue[tail++] = neighbors[i];
            }
        }
    }
    
    return count;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
void SetBit(Span<ulong> bits, ushort index)
{
    bits[index >> 6] |= 1UL << (index & 63);
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
bool GetBit(Span<ulong> bits, ushort index)
{
    return (bits[index >> 6] & (1UL << (index & 63))) != 0;
}

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