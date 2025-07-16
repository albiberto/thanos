using BenchmarkDotNet.Attributes;
using Thanos.CollisionMatrix;
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
}