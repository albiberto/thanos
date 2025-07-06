using BenchmarkDotNet.Running;
using Thanos;
using Thanos.Console;

BenchmarkRunner.Run<BuildCollisionMatrixBenchmarks>();
BenchmarkRunner.Run<GetValidMovesBenchmarks>();

return;

var board = Support.BuildBoard();
var mySnake = board.snakes[0];

MonteCarlo.GetValidMoves(board.width, board.height, mySnake.id, mySnake.body, mySnake.body.Length, mySnake.head.x, mySnake.head.y, board.hazards, board.hazards.Length, board.snakes, board.snakes.Length, mySnake.health < 100);