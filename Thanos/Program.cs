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
    agent.Start(request);
});

app.MapPost("/move", async context =>
{
    var request = await ReadAsync(context);
    var result = agent.Move(request);
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { move = ToApiMove(result) });
});

app.MapPost("/end", async context =>
{
    var request = await ReadAsync(context);
    agent.End(request);
});

app.Run();
return;

async Task<Request> ReadAsync(HttpContext httpContext) => await JsonSerializer.DeserializeAsync(httpContext.Request.Body, ThanosSerializerContext.Default.Request);

static string ToApiMove(byte move) =>
    move switch
    {
        Moves.Up => "up",
        Moves.Down => "down",
        Moves.Left => "left",
        Moves.Right => "right",
        _ => "up" // Fallback
    };