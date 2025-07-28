using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Thanos.UltraFast;

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
GameManager.Initialize();

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
app.MapPost("/move", async context =>
{
    // --- Step 1: Fase ASINCRONA (fuori da unsafe) ---
    // Leggi il body della richiesta. L'operazione di I/O viene attesa qui.
    var readResult = await context.Request.BodyReader.ReadAsync();
    var buffer = readResult.Buffer;
    
    // --- Step 2: Fase SINCRONA e UNSAFE ---
    // Ora che i dati sono in memoria, possiamo usare un blocco unsafe per l'elaborazione.
    unsafe
    {
        // Chiama il parser con la sequenza di byte.
        BattlesnakeParser.ParseDirect(buffer.FirstSpan, GameManager.State);
        
        // TODO: Chiedi al GameManager di trovare la mossa migliore.
    }
    
    // Comunica al BodyReader che abbiamo finito con il buffer.
    context.Request.BodyReader.AdvanceTo(buffer.End);

    // --- Step 3: Fase ASINCRONA (fuori da unsafe) ---
    // Scrivi la risposta. L'operazione di I/O viene attesa qui.
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(BattlesnakeParser.GetMoveResponse(Direction.Down));
});

app.Run("http://0.0.0.0:8080");
