using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Thanos;
using Thanos.Enums;
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
    
    unsafe
    {
        WarArena? arena = null; // La dichiariamo fuori dal try/finally

        try
        {
            // --- 1. Deserializzazione ---
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

            // --- 2. Creazione del Contesto (una sola volta) ---
            // Allochiamo memoria non gestita per il contesto
            var contextPtr = (WarContext*)NativeMemory.AlignedAlloc((nuint)sizeof(WarContext), Constants.CacheLineSize);
            // Creiamo il contesto e lo copiamo in quella memoria
            *contextPtr = new WarContext(in request.Board);

            // --- 3. Creazione dell'Arena ---
            // L'arena riceve la request e il puntatore al contesto immutabile
            arena = new WarArena(in request, contextPtr);
    
            // --- 4. Qui inizia la tua logica di gioco (MCTS, ecc.) ---
            // Esempio:
            // var bestMove = mcts.FindBestMove(in arena);
            // return bestMove;
        }
        finally
        {
            // --- 5. Pulizia Finale ---
            // L'arena si occupa di liberare tutta la memoria che gestisce, incluso il contesto.
            arena?.Dispose();
        }
    }
});


app.Run();
