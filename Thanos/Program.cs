using System.Text.Json;
using Microsoft.IO;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos;

public static class Program
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        //  --- L'agent viene creato e gestito qui ---
        var agent = new BattleSnakeAgent();
        app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());

        // --- Endpoint dell'API ---

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
            var request = await ReadRequestAsync(context);
            agent.Start(request);
        });

        app.MapPost("/move", async context =>
        {
            var request = await ReadRequestAsync(context);
            var result = agent.Move(request);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { move = ToApiMove(result) });
        });

        app.MapPost("/end", async context =>
        {
            var request = await ReadRequestAsync(context);
            agent.End(request);
        });

        app.Run();
    }

    /// <summary>
    ///     Legge e deserializza la richiesta HTTP usando il pool di stream e i convertitori custom.
    /// </summary>
    private static async Task<Request> ReadRequestAsync(HttpContext context)
    {
        // Permette di leggere lo stream del body più volte
        context.Request.EnableBuffering();
        
        // Legge l'intero body della richiesta come una stringa
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var jsonString = await reader.ReadToEndAsync();
        
        // --- STAMPA IL JSON GREZZO A CONSOLE ---
        Console.WriteLine("--- RAW /start JSON RECEIVED ---");
        Console.WriteLine(jsonString);
        Console.WriteLine("------------------------------");
        
        // // Riporta lo stream all'inizio per permettere la normale deserializzazione
        context.Request.Body.Position = 0;
    
        // Usa l'override che accetta lo Stream, il JsonTypeInfo dal source generator
        // e un CancellationToken per gestire l'annullamento della richiesta.

        
        // OTTIMIZZAZIONE: Usa uno stream riciclato dal pool invece di allocarne uno nuovo.
        await using var memoryStream = StreamManager.GetStream();
        await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted);
        memoryStream.Position = 0;

        // FASE 1: Estrai la larghezza
        using var jsonDoc = await JsonDocument.ParseAsync(memoryStream, cancellationToken: context.RequestAborted);
        var width = jsonDoc.RootElement.GetProperty("board").GetProperty("width").GetInt32();

        memoryStream.Position = 0;

        // FASE 2: Deserializza con i convertitori
        var arrayConverter = new CoordinateArrayToUshortArrayConverter(width);
        var singleConverter = new CoordinateToUshortConverter(width);

        var options = new JsonSerializerOptions { Converters = { arrayConverter, singleConverter } };
        var contextWithOptions = new ThanosSerializerContext(options);

        var request = await JsonSerializer.DeserializeAsync(
            memoryStream,
            contextWithOptions.Request,
            context.RequestAborted);

        return request!;
    }

    /// <summary>
    ///     Converte la mossa da byte a stringa per la risposta API.
    /// </summary>
    private static string ToApiMove(byte move) =>
        move switch
        {
            Moves.Up => "up",
            Moves.Down => "down",
            Moves.Left => "left",
            Moves.Right => "right",
            _ => "up" // Un fallback sicuro
        };
}