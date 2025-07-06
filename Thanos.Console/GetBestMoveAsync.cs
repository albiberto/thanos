using BenchmarkDotNet.Attributes;
using Thanos.Model;

namespace Thanos.Console;

public class GetBestMoveAsync
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
    public async Task GetBestMove_Benchmark()
    {
        await _monteCarlo.GetBestMoveAsync(new MoveRequest
        {
            Board = _board,
            You = _mySnake
        });
    }
}