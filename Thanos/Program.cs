using System.Text.Json;
using Thanos;
using Thanos.Common;
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
    
    Console.WriteLine($"[START] New game started: {JsonSerializer.Serialize(request)}");
    agent.Start(request!.Value);
});

app.MapPost("/move", async context =>
{
    var request = await ReadAsync(context);
    var result = agent.Move(request!.Value);
    
    context.Response.ContentType = "application/json";
    var move = ToApiMove(result);
    Console.WriteLine($"[MOVE] Chosen move: {move}");
    await context.Response.WriteAsJsonAsync(new { move = ToApiMove(result) });
});

app.MapPost("/end", async context =>
{
    var request = await ReadAsync(context);
    agent.End(request!.Value);
});

app.Run();
return;

static async Task<Request?> ReadAsync(HttpContext context)
{
    // Permette di leggere lo stream del body più volte
    // context.Request.EnableBuffering();
    //
    // // Legge l'intero body della richiesta come una stringa
    // using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    // var jsonString = await reader.ReadToEndAsync();
    //
    // // --- STAMPA IL JSON GREZZO A CONSOLE ---
    // Console.WriteLine("--- RAW /start JSON RECEIVED ---");
    // Console.WriteLine(jsonString);
    // Console.WriteLine("------------------------------");
    //
    // // Riporta lo stream all'inizio per permettere la normale deserializzazione
    // context.Request.Body.Position = 0;
    
    // Usa l'override che accetta lo Stream, il JsonTypeInfo dal source generator
    // e un CancellationToken per gestire l'annullamento della richiesta.
    return await JsonSerializer.DeserializeAsync(
        context.Request.Body,
        ThanosSerializerContext.Default.Request,
        context.RequestAborted); 
}

static string ToApiMove(byte move) =>
    move switch
    {
        Moves.Up => "up",
        Moves.Down => "down",
        Moves.Left => "left",
        Moves.Right => "right",
        _ => "left" // Fallback
    };