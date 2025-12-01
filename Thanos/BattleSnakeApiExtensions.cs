using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.IO;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos;

public static class BattleSnakeApiExtensions
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    extension(WebApplication app)
    {
        public WebApplication MapGetInfo()
        {
            app.MapGet("/", () => new
            {
                apiversion = "1",
                author = "Thanos",
                color = "#EB9736",
                head = "safe",
                tail = "round-bum"
            });

            return app;
        }

        public WebApplication MapStart(BattleSnakeAgent agent)
        {
            app.MapPost("/start", async context =>
            {
                var request = await context.ReadRequestAsync();
                agent.Start(request);
            });

            return app;
        }

        public WebApplication MapMove(BattleSnakeAgent agent)
        {
            app.MapPost("/move", async context =>
            {
                var request = await context.ReadRequestAsync();
                var result = await agent.Move(request);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    move = result switch
                    {
                        Moves.Up => "up",
                        Moves.Down => "down",
                        Moves.Left => "left",
                        Moves.Right => "right",
                        _ => "up"
                    }
                });
            });

            return app;
        }

        public WebApplication MapEnd(BattleSnakeAgent agent)
        {
            app.MapPost("/end", async context =>
            {
                var request = await context.ReadRequestAsync();
                agent.End(request);
            });

            return app;
        }
    }

#if DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<Request> ReadRequestAsync(this HttpContext httpContext)
    {
        await using var stream = StreamManager.GetStream();
        await httpContext.Request.Body.CopyToAsync(stream, httpContext.RequestAborted);
        stream.Position = 0;

        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var json = await reader.ReadToEndAsync();
            Console.WriteLine("--- BEGIN OF RAW JSON ---");
            Console.WriteLine(json);
            Console.WriteLine("--- END OF RAW JSON ---");
        }

        stream.Position = 0;

        int width;
        using (var document = await JsonDocument.ParseAsync(stream, cancellationToken: httpContext.RequestAborted))
        {
            width = document.RootElement.GetProperty("board").GetProperty("width").GetInt32();
        }

        stream.Position = 0;

        var arrayConverter = new CoordinateArrayToUshortArrayConverter(width);
        var singleConverter = new CoordinateToUshortConverter(width);
        var options = new JsonSerializerOptions { Converters = { arrayConverter, singleConverter } };
        var serializerContext = new ThanosSerializerContext(options);

        var request = await JsonSerializer.DeserializeAsync(stream, serializerContext.Request, httpContext.RequestAborted);
        return request!;
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<Request> ReadRequestAsync(this HttpContext httpContext)
    {
        await using var stream = StreamManager.GetStream();
        await httpContext.Request.Body.CopyToAsync(stream, httpContext.RequestAborted);
        stream.Position = 0;

        int width;
        using (var document = await JsonDocument.ParseAsync(stream, cancellationToken: httpContext.RequestAborted))
        {
            width = document.RootElement.GetProperty("board").GetProperty("width").GetInt32();
        }

        stream.Position = 0;

        var arrayConverter = new CoordinateArrayToUshortArrayConverter(width);
        var singleConverter = new CoordinateToUshortConverter(width);
        var options = new JsonSerializerOptions { Converters = { arrayConverter, singleConverter } };
        var serializerContext = new ThanosSerializerContext(options);

        var request = await JsonSerializer.DeserializeAsync(stream, serializerContext.Request, httpContext.RequestAborted);
        return request!;
    }
#endif
}