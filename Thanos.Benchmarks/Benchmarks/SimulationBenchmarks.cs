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
        // 1. Inizializzazione a Costo Zero (Allocazione sullo Stack)
        var state = new HyperState();
        state.Initialize(11, 11); // Inizializza i Ghost Borders per una mappa 11x11

        // 2. Setup iniziale dei serpenti. 
        // Come in Board.cs di Roald, impostiamo Health=100, Length=1 e EatenFood=2 (che per noi è PendingGrowth)
    
        // Snake 0: equivalente a 5 + 2 * 11
        state.Snake0.Health = 100;
        state.Snake0.Length = 1;
        state.Snake0.PendingGrowth = 2;
        state.Snake0.AdvanceHead(ref state.Obstacles, 54);

        // Snake 1: equivalente a 5 + 8 * 11
        state.Snake1.Health = 100;
        state.Snake1.Length = 1;
        state.Snake1.PendingGrowth = 2;
        state.Snake1.AdvanceHead(ref state.Obstacles, 150);

        // Snake 2: equivalente a 2 + 5 * 11
        state.Snake2.Health = 100;
        state.Snake2.Length = 1;
        state.Snake2.PendingGrowth = 2;
        state.Snake2.AdvanceHead(ref state.Obstacles, 99);

        // Snake 3: equivalente a 8 + 5 * 11
        state.Snake3.Health = 100;
        state.Snake3.Length = 1;
        state.Snake3.PendingGrowth = 2;
        state.Snake3.AdvanceHead(ref state.Obstacles, 105);

        // 3. Esecuzione del Benchmark
        for (var i = 0; i < Turns; i++)
        {
            // Estraiamo l'array di byte con le mosse per questo turno
            var turnMoves = _moves[i % _moves.Length];
        
            // La macchina macina le mosse, gestisce code, cibo, muri e morti. Tutto su stack.
            HyperRules.SimulateTurn(ref state, turnMoves);
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