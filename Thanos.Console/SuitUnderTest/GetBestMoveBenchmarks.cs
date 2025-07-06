using BenchmarkDotNet.Attributes;
using Thanos.Model;

namespace Thanos.Console.SuitUnderTest;

public class GetBestMoveBenchmarks
{
    private readonly MonteCarlo _monteCarlo = new([]);

    private Board _board = null!;
    private Snake _mySnake = null!;

    [GlobalSetup]
    public void Setup()
    {
        _board = Support.BuildBoard();
        _mySnake = _board.snakes[0];
    }

    [Benchmark]
    public void GetBestMove_Benchmark()
    {
        _monteCarlo.GetBestMove(new MoveRequest
        {
            Board = _board,
            You = _mySnake
        });
    }
}