// using BenchmarkDotNet.Attributes;
// using Thanos.CollisionMatrix;
// using Thanos.Model;
//
// namespace Thanos.Console.SuitUnderTest;
//
// public class GetValidMovesBenchmarks
// {
//     private Board _board = null!;
//     private Snake _mySnake = null!;
//
//     [GlobalSetup]
//     public void Setup()
//     {
//         _board = Support.BuildBoard();
//         _mySnake = _board.snakes[0];
//     }
// }


using BenchmarkDotNet.Attributes;
using Thanos.CollisionMatrix;
using Thanos.Model;

namespace Thanos.Console.SuitUnderTest;

public class GetValidMovesBenchmarks
{

    [Benchmark]
    public void BenchmarkProcessMove()
    {
        GameManager.ProcessMove(Direction.Left);
        GameManager.ProcessMove(Direction.Left);
        GameManager.ProcessMove(Direction.Down);
        GameManager.ProcessMove(Direction.Up);
    }
}