using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Snakes.Core;
using Spectre.Console;
using Thanos;
using Thanos.Memory;
using Thanos.War.Rules;
using Thanos.War.State;

// var b = new SimulationBenchmarks();
// for (var i = 0; i < 1000_000; i++)
// {
//     b.Roald();
// }
//
// return;

// AnsiConsole.Live(new Panel(""))
//     .AutoClear(false)
//     .Overflow(VerticalOverflow.Ellipsis)
//     .Cropping(VerticalOverflowCropping.Bottom)
//     .Start(ctx =>
//     {
//         var bench = new SimulationBenchmarks();
//         bench.Roald(ctx);
//         bench.Roald(ctx);
//         bench.Roald(ctx);
//         
//         // bench.Alby(ctx);
//         // bench.Alby(ctx);
//         // bench.Alby(ctx);
//         
//         // new SimulationBenchmarks().Alby(ctx);
//     });
//
// return;

BenchmarkRunner.Run<SimulationBenchmarks>();

[MemoryDiagnoser]
public class SimulationBenchmarks
{
    readonly Allocator allocator = new(10240);
    
    readonly GameContext context = new(LookupsMemoryPool.Medium.NeighborsMatrix, 0, 0, 20);
    readonly SlotMemoryPool pool = new(
        10,
        0, 
        Constants.MaxSnakesCount, 
        LookupsMemoryPool.Medium, 
        new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount));

    readonly byte[][] moves = new byte[][]
    {
        // up
        [1, 1, 1, 1],
        // right
        [3, 3, 3, 3],
        // down
        [0, 0, 0, 0],
        // left
        [2, 2, 2, 2]
    };

    const int turns = 100;

    [Benchmark(Baseline = true)]
    public void Alby()
    {
        var game = pool.GetGameState(0);
        StateMapper.Initialize(ref game, [
            5 + 2 * 11,
            5 + 8 * 11,
            2 + 5 * 11,
            8 + 5 * 11
        ]);

        for (var i = 0; i < turns; i++)
        {
            StandardRules.SimulateTurn(ref game, context, moves[i % moves.Length]);
            EnvironmentRules.SimulateFoodSpawn(ref game, context);
        }
        
        pool.Reset();
    }
    
    [Benchmark]
    public void Roald()
    {
        var board = new Board();
        board.New(allocator, new Board.Parameters
        {
            Width = 11,
            Height = 11,
            Snakes =
            [
                5 + 2 * 11,
                5 + 8 * 11,
                2 + 5 * 11,
                8 + 5 * 11 
            ]
        });

        for (var i = 0; i < turns; i++)
        {
            board.BeginTurn();
            board.MoveSnakes(moves[i % moves.Length]);
            board.EndTurn(spawnFood: true);
        }

        allocator.Reset();
    }   

    public void Alby(LiveDisplayContext ctx)
    {
        var game = pool.GetGameState(0);
        StateMapper.Initialize(ref game, [
            5 + 2 * 11,
            5 + 8 * 11,
            2 + 5 * 11,
            8 + 5 * 11
        ]);
        
        for (var i = 0; i < turns; i++)
        {
            StandardRules.SimulateTurn(ref game, context, moves[i % moves.Length]);
            EnvironmentRules.SimulateFoodSpawn(ref game, context);
            
            var ui = game.Render(Constants.Medium.Width, Constants.Medium.Height);
            ctx.UpdateTarget(new Panel(new Markup(ui)));
            ctx.Refresh();

            Thread.Sleep(50);
        }
        
        pool.Reset();
    }
    
    public void Roald(LiveDisplayContext ctx)
    {
        var board = new Board();
        board.New(allocator, new Board.Parameters
        {
            Width = 11,
            Height = 11,
            Snakes =
            [
                5 + 2 * 11,
                5 + 8 * 11,
                2 + 5 * 11,
                8 + 5 * 11 
            ]
        });

        for (var i = 0; i < turns; i++)
        {
            board.BeginTurn();
            board.MoveSnakes(moves[i % moves.Length]);
            board.EndTurn(true);

            var ui = board.Render([]);
            ctx.UpdateTarget(new Panel(new Markup(ui)));
            ctx.Refresh();

            Thread.Sleep(50);
        }
        
        allocator.Reset();
    }
}