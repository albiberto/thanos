using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Thanos;
using Thanos.SourceGen;
using Thanos.War;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Initialize GameManager during application bootstrapping
// RuntimeHelpers.RunClassConstructor(typeof(GameEngine).TypeHandle);

app.MapGet("/", () => Results.Ok());

app.MapPost("/start", () => Results.Ok());

app.MapPost("/end", () => Results.Ok());

app.MapPost("/move", async context =>
{
    var readResult = await context.Request.BodyReader.ReadAsync();
    var sequence = readResult.Buffer;

    try
    {
        Request request;

        if (sequence.IsSingleSegment)
        {
            request = JsonSerializer.Deserialize(sequence.FirstSpan, ThanosSerializerContext.Default.Request);
        }
        else
        {
            var bytes = sequence.ToArray();
            request = JsonSerializer.Deserialize(bytes, ThanosSerializerContext.Default.Request);
        }

        var arena = new WarArena(in request);
        
        // Usa l'oggetto deserializzato
        await context.Response.WriteAsync("OK");
    }
    finally
    {
        // Segnala che hai consumato tutto il buffer
        context.Request.BodyReader.AdvanceTo(sequence.End);
    }
});


app.Run();
