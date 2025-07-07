using BenchmarkDotNet.Running;
using Thanos;
using Thanos.Console;
using Thanos.Console.SuitUnderTest;
using Thanos.Model;

// BenchmarkRunner.Run<BuildCollisionMatrixBenchmarks>();
BenchmarkRunner.Run<GetValidMovesBBenchmarks>();
// BenchmarkRunner.Run<GetBestMoveBenchmarks>();

return;

var board = Support.BuildBoard();
var mySnake = board.snakes[0];

var moves = MonteCarlo.GetValidMovesUltraFast(board.width, board.height, mySnake.id, mySnake.body, mySnake.body.Length, mySnake.head.x, mySnake.head.y, board.hazards, board.hazards.Length, board.snakes, board.snakes.Length, mySnake.health < 100);

// board.Print();
// var monteCarlo = new MonteCarlo();
// monteCarlo.GetBestMove(new MoveRequest
// {
//     Board = board,
//     You = mySnake
// });
