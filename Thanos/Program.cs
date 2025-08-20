using System.Text.Json;
using Thanos;
using Thanos.MCST;
using Thanos.SourceGen;

// --- Setup iniziale del server web ---
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var agent = new BattleSnakeAgent();
app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());

app.MapGet("/", () => new
{
    apiversion = "1",
    author = "Thanos",
    color = "#8B0000",
    head = "safe",
    tail = "round-bum"
});

app.MapPost("/start", async context =>
{
    var request = await ReadAsync(context);
    agent.Start(request!.Value);
});

app.MapPost("/move", async context =>
{
    var request = await ReadAsync(context);
    var result = agent.Move(request!.Value);
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { move = ToApiMove(result) });
});

app.MapPost("/end", async context =>
{
    var request = await ReadAsync(context);
    BattleSnakeAgent.End(request!.Value);
});

app.Run();
return;

static async Task<Request?> ReadAsync(HttpContext httpContext)
{
    // Usa l'override che accetta lo Stream, il JsonTypeInfo dal source generator
    // e un CancellationToken per gestire l'annullamento della richiesta.
    return await JsonSerializer.DeserializeAsync(
        httpContext.Request.Body,
        ThanosSerializerContext.Default.Request,
        httpContext.RequestAborted); 
}

static string ToApiMove(byte move) =>
    move switch
    {
        Moves.Up => "up",
        Moves.Down => "down",
        Moves.Left => "left",
        Moves.Right => "right",
        _ => "up" // Fallback
    };