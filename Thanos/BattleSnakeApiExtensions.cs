using Thanos.Abstract;
using Thanos.Common;
using Thanos.SourceGen;

namespace Thanos;

public static class BattleSnakeApiExtensions
{
    extension(WebApplication app)
    {
        public WebApplication MapGetInfo()
        {
            app.MapGet("/", () => new
            {
                apiversion = "1",
                author = "Thanos",
                color = "#2D6A4F",
                head = "safe",
                tail = "round-bum"
            });

            return app;
        }

        public WebApplication MapStart(IBattleSnakeAgent agent)
        {
            app.MapPost("/start", async context =>
            {
                var request = await BattleSnakeSerializer.ReadRequestAsync(context);
                agent.Start(request);
            });

            return app;
        }

        public WebApplication MapMove(IBattleSnakeAgent agent)
        {
            app.MapPost("/move", async context =>
            {
                var request = await BattleSnakeSerializer.ReadRequestAsync(context);
                
                var move = await agent.Move(request);

                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(MoveResponseCache.Get(move));
            });

            return app;
        }

        public WebApplication MapEnd(IBattleSnakeAgent agent)
        {
            app.MapPost("/end", async context =>
            {
                var request = await BattleSnakeSerializer.ReadRequestAsync(context);
                agent.End(request);
            });

            return app;
        }
    }
}

public static class MoveResponseCache
{
    private static readonly byte[][] _responses = new byte[9][];

    static MoveResponseCache()
    {
        var upBytes    = """{"move":"up"}"""u8.ToArray();
        var downBytes  = """{"move":"down"}"""u8.ToArray();
        var leftBytes  = """{"move":"left"}"""u8.ToArray();
        var rightBytes = """{"move":"right"}"""u8.ToArray();

        Array.Fill(_responses, upBytes);

        _responses[Moves.Up]    = upBytes;
        _responses[Moves.Down]  = downBytes;
        _responses[Moves.Left]  = leftBytes;
        _responses[Moves.Right] = rightBytes;
    }

    public static ReadOnlyMemory<byte> Get(byte moveFlag) => _responses[moveFlag];
}