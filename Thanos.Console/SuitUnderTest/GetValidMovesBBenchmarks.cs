using BenchmarkDotNet.Attributes;
using Thanos.CollisionMatrix;
using Thanos.Model;

namespace Thanos.Console.SuitUnderTest;

public class GetValidMovesBBenchmarks
{
    private readonly MonteCarlo _monteCarlo = new();

    private Board _board = null!;
    private Snake _mySnake = null!;

    [GlobalSetup]
    public void Setup()
    {
        _board = Support.BuildBoard();
        _mySnake = _board.snakes[0];
    }

    
    [Benchmark]
    public void GetValidMovesUltraFast_Benchmark()
    {
        GetValidMovesUltraFastB.GetValidMovesUltraFast(
            _board.width, 
            _board.height, 
            _mySnake.id, 
            _mySnake.body, 
            _mySnake.body.Length, 
            _mySnake.head.x, 
            _mySnake.head.y, 
            _board.hazards, 
            _board.hazards.Length, 
            _board.snakes, 
            _board.snakes.Length, 
            _mySnake.health < 100);
    }
    
    [Benchmark]
    public void GetValidMovesLightSpeed_Benchmark()
    {
        GetValidMovesLightSpeedB.GetValidMovesLightSpeed(
            _board.width, 
            _board.height, 
            _mySnake.id, 
            _mySnake.body, 
            _mySnake.body.Length, 
            _mySnake.head.x, 
            _mySnake.head.y, 
            _board.hazards, 
            _board.hazards.Length, 
            _board.snakes, 
            _board.snakes.Length, 
            _mySnake.health < 100);
    }
    
    [Benchmark]
    public void GetValidMoves_Benchmark()
    {
        GetValidMovesB.GetValidMoves(
            _board.width, 
            _board.height, 
            _mySnake.id, 
            _mySnake.body, 
            _mySnake.body.Length, 
            _mySnake.head.x, 
            _mySnake.head.y, 
            _board.hazards, 
            _board.hazards.Length, 
            _board.snakes, 
            _board.snakes.Length, 
            _mySnake.health < 100);
    }
}