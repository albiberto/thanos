using BenchmarkDotNet.Running;
using Thanos;
using Thanos.Console;
using Thanos.Model;

BenchmarkRunner.Run<BuildCollisionMatrixBenchmarks>();
BenchmarkRunner.Run<GetValidMovesBenchmarks>();
BenchmarkRunner.Run<GetBestMoveAsync>();

return;

var board = Support.BuildBoard();
var mySnake = board.snakes[0];

MonteCarlo.GetValidMoves(board.width, board.height, mySnake.id, mySnake.body, mySnake.body.Length, mySnake.head.x, mySnake.head.y, board.hazards, board.hazards.Length, board.snakes, board.snakes.Length, mySnake.health < 100);

board.Print();
var monteCarlo = new MonteCarlo([]);
await monteCarlo.GetBestMoveAsync(new MoveRequest
{
    Board = board,
    You = mySnake
});
