using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Snakes.Core;
using Spectre.Console;
using Thanos.LightSpeed;
using Thanos.Memory;
using Thanos.War.Rules;
using Thanos.War.State;
using HyperRules = Thanos.Hyper.HyperRules;
using HyperState = Thanos.Hyper.HyperState;

namespace Thanos.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class SimulationBenchmarks
{
    private const int Turns = 100;
    
    private readonly Allocator _allocator = new(10240);
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
    public void LightSpeed()
    {
        // 1. Inizializzazione Estrema a Costo Zero
        // Evitiamo la bzero() nativa di C# su 1KB di memoria. Massima velocità.
        Unsafe.SkipInit(out LSState state);

        // Inizializzazione tramite HyperBoard Pre-calcolata (Copia SIMD 32-byte in 1 ciclo di clock)
        state.Obstacles = PrecomputedBoards.Border11x11;
        state.Food.Clear();
        state.AliveCount = 4;

        // Pulizia manuale delle BodyMask (Necessaria perché usiamo SkipInit)
        state.Snake0.BodyMask.Clear();
        state.Snake1.BodyMask.Clear();
        state.Snake2.BodyMask.Clear();
        state.Snake3.BodyMask.Clear();

        // 2. Setup iniziale dei serpenti. 
        // Snake 0: equivalente a 5 + 2 * 11 (X=6, Y=3) -> 54
        state.Snake0.Health = 100;
        state.Snake0.Length = 1;
        state.Snake0.PendingGrowth = 2;
        state.Snake0.StackedSegments = 0;
        state.Snake0.HeadPointer = 0;
        state.Snake0.TailPointer = 0;
        state.Snake0.AdvanceHead(ref state.Obstacles, 54);

        // Snake 1: equivalente a 5 + 8 * 11 (X=6, Y=9) -> 150
        state.Snake1.Health = 100;
        state.Snake1.Length = 1;
        state.Snake1.PendingGrowth = 2;
        state.Snake1.StackedSegments = 0;
        state.Snake1.HeadPointer = 0;
        state.Snake1.TailPointer = 0;
        state.Snake1.AdvanceHead(ref state.Obstacles, 150);

        // Snake 2: equivalente a 2 + 5 * 11 (X=3, Y=6) -> 99
        state.Snake2.Health = 100;
        state.Snake2.Length = 1;
        state.Snake2.PendingGrowth = 2;
        state.Snake2.StackedSegments = 0;
        state.Snake2.HeadPointer = 0;
        state.Snake2.TailPointer = 0;
        state.Snake2.AdvanceHead(ref state.Obstacles, 99);

        // Snake 3: equivalente a 8 + 5 * 11 (X=9, Y=6) -> 105
        state.Snake3.Health = 100;
        state.Snake3.Length = 1;
        state.Snake3.PendingGrowth = 2;
        state.Snake3.StackedSegments = 0;
        state.Snake3.HeadPointer = 0;
        state.Snake3.TailPointer = 0;
        state.Snake3.AdvanceHead(ref state.Obstacles, 105);

        // 3. Esecuzione del Benchmark
        for (var i = 0; i < Turns; i++)
        {
            // Array di byte estratto e passato by ref (Zero overhead)
            var turnMoves = _moves[i % _moves.Length];
        
            // La macchina branchless macina le mosse
            LSRules.SimulateTurn(ref state, turnMoves);
        }
    }
    
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
}