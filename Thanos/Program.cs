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
    author = "Thanos",
    color = "#8B0000",
    head = "safe",
    tail = "round-bum"
});

app.MapPost("/start", async context =>
{
    var request = await Read(context);
    agent.Start(request);
});

app.MapPost("/move", async context =>
{
    var request = await Read(context);
    var result = agent.Move(request);
});

app.MapPost("/end", async context =>
{
    var request = await Read(context);
    agent.End(request);
});

app.Run();
return;

async Task<Request> Read(HttpContext httpContext)
{
    var readResult = await httpContext.Request.BodyReader.ReadAsync();
    var sequence = readResult.Buffer;
    
    return sequence.IsSingleSegment
        ? JsonSerializer.Deserialize(sequence.FirstSpan, ThanosSerializerContext.Default.Request)
        : JsonSerializer.Deserialize(sequence.ToArray(), ThanosSerializerContext.Default.Request);
}