using BenchmarkDotNet.Attributes;
using Thanos.CollisionMatrix;
using Thanos.Model;

namespace Thanos.Console.SuitUnderTest;

public class BuildCollisionMatrixBenchmarks
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
    public void BuildCollisionMatrixSimd_Benchmark()
    {
        CollisionMatrixSIMD.BuildCollisionMatrix(_board, _mySnake);
    }

    [Benchmark]
    public void BuildCollisionMatrixUnsafe_Benchmark()
    {
        CollisionMatrixUnsafe.BuildCollisionMatrix(_board, _mySnake);
    }

    [Benchmark]
    public void BuildCollisionMatrixUnsafeNoBounds_Benchmark()
    {
        CollisionMatrixUnsafeNoBounds.BuildCollisionMatrix(_board, _mySnake);
    }
}