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
    private const int Turns = 100;
    
    private readonly Allocator _allocator = new(10240);
    private readonly GameContext _context = new(LookupsMemoryPool.Medium.NeighborsMatrix, 0, 0, 20);
    private readonly SlotMemoryPool _pool = new(10, 0, Constants.MaxSnakesCount, LookupsMemoryPool.Medium, new (Constants.Medium.Area, 64, Constants.MaxSnakesCount));

    private readonly byte[][] _moves = new byte[][]
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
    
    [Benchmark(Baseline = true)]
    public void Alby()
    {
        var game = _pool.GetGameState(0);
        StateMapper.Initialize(ref game, [
            5 + 2 * 11,
            5 + 8 * 11,
            2 + 5 * 11,
            8 + 5 * 11
        ]);

        for (var i = 0; i < Turns; i++)
        {
            StandardRules.SimulateTurn(ref game, _context, _moves[i % _moves.Length]);
            EnvironmentRules.SimulateFoodSpawn(ref game, _context);
        }
        
        _pool.Reset();
    }
    
    [Benchmark]
    public void Roald()
    {
        var board = new Board();
        board.New(_allocator, new Board.Parameters
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

        for (var i = 0; i < Turns; i++)
        {
            board.BeginTurn();
            board.MoveSnakes(_moves[i % _moves.Length]);
            board.EndTurn(spawnFood: true);
        }

        _allocator.Reset();
    }   

    public void Alby(LiveDisplayContext ctx)
    {
        var game = _pool.GetGameState(0);
        StateMapper.Initialize(ref game, [
            5 + 2 * 11,
            5 + 8 * 11,
            2 + 5 * 11,
            8 + 5 * 11
        ]);
        
        for (var i = 0; i < Turns; i++)
        {
            StandardRules.SimulateTurn(ref game, _context, _moves[i % _moves.Length]);
            EnvironmentRules.SimulateFoodSpawn(ref game, _context);
            
            var ui = game.Render(Constants.Medium.Width, Constants.Medium.Height);
            ctx.UpdateTarget(new Panel(new Markup(ui)));
            ctx.Refresh();

            Thread.Sleep(50);
        }
        
        _pool.Reset();
    }
    
    public void Roald(LiveDisplayContext ctx)
    {
        var board = new Board();
        board.New(_allocator, new Board.Parameters
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

        for (var i = 0; i < Turns; i++)
        {
            board.BeginTurn();
            board.MoveSnakes(_moves[i % _moves.Length]);
            board.EndTurn(true);

            var ui = board.Render([]);
            ctx.UpdateTarget(new Panel(new Markup(ui)));
            ctx.Refresh();

            Thread.Sleep(50);
        }
        
        _allocator.Reset();
    }
}