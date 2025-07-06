using BenchmarkDotNet.Attributes;
using Thanos.CollisionMatrix;
using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Console;

public class BuildCollisionMatrixBenchmarks
{
    private Board _board = null!;
    private Snake _mySnake = null!;

    [GlobalSetup]
    public void Setup()
    {
        _board = new Board
        {
            width = 19,
            height = 19,
            hazards =
            [
                new Point { x = 9, y = 9 },
                new Point { x = 10, y = 10 },
                new Point { x = 6, y = 10 },
                new Point { x = 18, y = 17 }
            ],
            snakes =
            [
                new Snake
                {
                    id = "me",
                    health = 90,
                    head = new Point { x = 0, y = 0 },
                    body =
                    [
                        new Point { x = 0, y = 0 },
                        new Point { x = 1, y = 0 },
                        new Point { x = 2, y = 0 }
                    ]
                },
                new Snake
                {
                    id = "enemy1",
                    health = 100,
                    head = new Point { x = 18, y = 0 },
                    body =
                    [
                        new Point { x = 18, y = 0 },
                        new Point { x = 17, y = 0 },
                        new Point { x = 16, y = 0 }
                    ]
                },
                new Snake
                {
                    id = "enemy2",
                    health = 100,
                    head = new Point { x = 0, y = 18 },
                    body =
                    [
                        new Point { x = 0, y = 18 },
                        new Point { x = 1, y = 18 },
                        new Point { x = 2, y = 18 }
                    ]
                },
                new Snake
                {
                    id = "enemy3",
                    health = 100,
                    head = new Point { x = 18, y = 18 },
                    body =
                    [
                        new Point { x = 18, y = 18 },
                        new Point { x = 17, y = 18 },
                        new Point { x = 16, y = 18 }
                    ]
                }
            ]
        };
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

    [Benchmark]
    public void BuildCollisionMatrixMonteCarlo_Benchmark()
    {
        MonteCarlo.BuildCollisionMatrix(_board.width, _board.height, _mySnake.id, _mySnake.body, _mySnake.body.Length, _board.hazards, _board.hazards.Length, _board.snakes, _board.snakes.Length, _mySnake.health < 100);
    }
}