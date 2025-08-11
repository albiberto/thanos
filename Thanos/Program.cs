using System.Buffers;
using System.Text.Json;
using Thanos;
using Thanos.SourceGen;

// --- Setup iniziale del server web ---
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var agent = new BattleSnakeAgent();
app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());


app.MapGet("/", () => new
{
    apiversion = "1",
    author = "Il tuo Nome",
    color = "#8B0000", // Un bel rosso scuro per Thanos
    head = "safe",
    tail = "round-bum"
});

app.MapPost("/start", (Request game) =>
{
    Console.WriteLine($"--- Inizio Partita: {game.Game.Id} ---");
    return Results.Ok();
});

app.MapPost("/move", async context =>
{
    var readResult = await context.Request.BodyReader.ReadAsync();
    var sequence = readResult.Buffer;
    
    var request = sequence.IsSingleSegment
        ? JsonSerializer.Deserialize(sequence.FirstSpan, ThanosSerializerContext.Default.Request)
        : JsonSerializer.Deserialize(sequence.ToArray(), ThanosSerializerContext.Default.Request);

    var result = await agent.HandleMoveAsync(request);
});

app.MapPost("/end", (Request game) =>
{
    Console.WriteLine($"--- Fine Partita: {game.Game.Id} ---");
    return Results.Ok();
});


app.Run();