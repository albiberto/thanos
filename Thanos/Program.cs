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
        var fileWriter = new StreamWriter("log.txt", append: true)
        {
            AutoFlush = true
        };
        Console.SetOut(fileWriter);
        Console.SetError(fileWriter);
        
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
            color = "#00BFFF",
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
    private static async Task<Request> ReadRequestAsync(HttpContext httpContext)
    {
        // Usa uno stream riciclato dal pool invece di allocarne uno nuovo.
        await using var stream = StreamManager.GetStream();
        await httpContext.Request.Body.CopyToAsync(stream, httpContext.RequestAborted);
        stream.Position = 0;

         using var reader = new StreamReader(stream, leaveOpen: true);
         var json = await reader.ReadToEndAsync();
         Console.WriteLine("--- RAW JSON RECEIVED ---");
         Console.WriteLine(json);
         Console.WriteLine("------------------------------");

        stream.Position = 0;

        // FASE 1: Estrai la larghezza
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: httpContext.RequestAborted);
        var width = document.RootElement.GetProperty("board").GetProperty("width").GetInt32();

        stream.Position = 0;

        // FASE 2: Deserializza con i convertitori
        var arrayConverter = new CoordinateArrayToUshortArrayConverter(width);
        var singleConverter = new CoordinateToUshortConverter(width);

        var options = new JsonSerializerOptions { Converters = { arrayConverter, singleConverter } };
        var serializerContext = new ThanosSerializerContext(options);

        var request = await JsonSerializer.DeserializeAsync(stream, serializerContext.Request, httpContext.RequestAborted);

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
