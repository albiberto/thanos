using BenchmarkDotNet.Attributes;
using Snakes.Core;
using Spectre.Console;
using Thanos.Hyper;
using Thanos.Memory;
using Thanos.War.Rules;
using Thanos.War.State;

namespace Thanos.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class SimulationBenchmarks
{
    private const int Turns = 100;
    
    private readonly Allocator _allocator = new(10240);
    private readonly GameContext _context = new(LookupsMemoryPool.Medium.NeighborsMatrix, 0, 0, 20);
    private readonly SlotMemoryPool _pool = new(10, 0, Constants.MaxSnakesCount, LookupsMemoryPool.Medium, new (Constants.Medium.Area, 64, Constants.MaxSnakesCount));
    private readonly sbyte[] _hyperOffsets = [-1, 1, 16, -16];
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
    
    [Benchmark]
    public void HyperSpeed()
    {
        // Nessuna allocazione heap. State vive sullo stack.
        var state = new HyperState();
        state.Initialize(11, 11);

        // TRADUZIONE COORDINATE (X, Y) -> 1-based Ghost Border (X+1, Y+1)
        // Formula: (Y + 1) * 16 + (X + 1)
        
        // Alby: 5 + 2 * 11 (X=5, Y=2) => X=6, Y=3 => 3 * 16 + 6 = 54
        state.Snake0.AdvanceHead(ref state.Obstacles, 54);
        
        // Alby: 5 + 8 * 11 (X=5, Y=8) => X=6, Y=9 => 9 * 16 + 6 = 150
        state.Snake1.AdvanceHead(ref state.Obstacles, 150);
        
        // Alby: 2 + 5 * 11 (X=2, Y=5) => X=3, Y=6 => 6 * 16 + 3 = 99
        state.Snake2.AdvanceHead(ref state.Obstacles, 99);
        
        // Alby: 8 + 5 * 11 (X=8, Y=5) => X=9, Y=6 => 6 * 16 + 9 = 105
        state.Snake3.AdvanceHead(ref state.Obstacles, 105);

        for (var i = 0; i < Turns; i++)
        {
            var turnMoves = _moves[i % _moves.Length];

            // PHASE 1: Srotolamento Code (BeginTurn)
            state.Snake0.AdvanceTail(ref state.Obstacles);
            state.Snake1.AdvanceTail(ref state.Obstacles);
            state.Snake2.AdvanceTail(ref state.Obstacles);
            state.Snake3.AdvanceTail(ref state.Obstacles);

            // PHASE 2: Movimento Teste (MoveSnakes)
            // L'unchecked permette il wrap-around dei calcoli sui byte in 1 ciclo di clock
            state.Snake0.AdvanceHead(ref state.Obstacles, unchecked((byte)(state.Snake0.GetHead() + _hyperOffsets[turnMoves[0]])));
            state.Snake1.AdvanceHead(ref state.Obstacles, unchecked((byte)(state.Snake1.GetHead() + _hyperOffsets[turnMoves[1]])));
            state.Snake2.AdvanceHead(ref state.Obstacles, unchecked((byte)(state.Snake2.GetHead() + _hyperOffsets[turnMoves[2]])));
            state.Snake3.AdvanceHead(ref state.Obstacles, unchecked((byte)(state.Snake3.GetHead() + _hyperOffsets[turnMoves[3]])));
        }
    }
    
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