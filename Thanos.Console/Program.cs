using BenchmarkDotNet.Running;
using Thanos.CollisionMatrix;
using Thanos.Console;
using Thanos.Console.SuitUnderTest;
using Thanos.Model;

GameManager.Initialize();
GameManager.ProcessMove(Direction.Left);

// BenchmarkRunner.Run<GetValidMovesBenchmarks>();

// return;

// var board = Support.BuildBoard();
// var mySnake = board.snakes[0];


// board.Print();
// var monteCarlo = new MonteCarlo();
// monteCarlo.GetBestMove(new MoveRequest
// {
    // Board = board,
    // You = mySnake
// });
