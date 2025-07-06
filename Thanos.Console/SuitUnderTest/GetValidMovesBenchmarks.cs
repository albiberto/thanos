using BenchmarkDotNet.Attributes;
using Thanos.Model;

namespace Thanos.Console.SuitUnderTest;

public class GetValidMovesBenchmarks
{
    private Board _board = null!;
    private Snake _mySnake = null!;

    [GlobalSetup]
    public void Setup()
    {
        _board = Support.BuildBoard();
        _mySnake = _board.snakes[0];
    }

    [Benchmark]
    public void BuildCollisionMatrixMonteCarlo_Benchmark()
    {
        MonteCarlo.GetValidMoves(_board.width, _board.height, _mySnake.id, _mySnake.body, _mySnake.body.Length, _mySnake.head.x, _mySnake.head.y, _board.hazards, _board.hazards.Length, _board.snakes, _board.snakes.Length, _mySnake.health < 100);
    }
}