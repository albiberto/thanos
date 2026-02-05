using Thanos.Common;
using Thanos.SourceGen;
using Thanos.Tests.Integration.Support;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    [TestCaseSource(nameof(MovementStackedScenarios))]
    public void UpdateAfterMove_WhenStackedAtStart_ShouldUnrollGradually(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakePlacement _)
    {
        // Arrange
        var snake = context.Build();
        var snakeData = new Snake("stacked-hero", hp, body);
        snake.Initialize(in snakeData);

        var initialCredits = body.Length - 1;
        var initialTailPos = body[^1];

        // Oracle: Standard Queue represents the LOGICAL body (fully unrolled view)
        var oracleQueue = new Queue<ushort>(body.Reverse());
        var currentHead = oracleQueue.Last();
        var moves = new[] { Moves.Up, Moves.Right, Moves.Down, Moves.Left };
        var expectedHp = (int)hp;
        var turnCounter = 0;

        // Act & Assert
        foreach (var move in moves)
        foreach (var __ in Enumerable.Range(1, env.Width / 2))
        {
            turnCounter++;
            var (cx, cy) = GetCoord(currentHead, env.Width);
            var (nx, ny) = move switch
            {
                Moves.Up => (cx, cy + 1),
                Moves.Right => (cx + 1, cy),
                Moves.Down => (cx, cy - 1),
                Moves.Left => (cx - 1, cy),
                _ => (cx, cy)
            };

            var nextHead = (ushort)(ny * env.Width + nx);
            
            // Oracle Logic:
            // 1. Always Enqueue new head.
            // 2. Always Dequeue old tail (Conservation of Mass).
            //    The "Unrolling" is a physical implementation detail of the SUT (Credits -> Queue),
            //    but logically the snake has constant length during movement without food.
            oracleQueue.Enqueue(nextHead);
            oracleQueue.Dequeue();

            // Execution
            snake.UpdateAfterMove(nextHead, false, NormalDamage);

            // Verification
            expectedHp -= NormalDamage;
            That(snake.Hp, Is.EqualTo(expectedHp), $"Turn {turnCounter}: HP mismatch.");

            // Length Verification (Dual Property Check)
            // ActualLength: The logical game length (must match Oracle count).
            // Length: The physical queue length (matches Oracle unique segments if compression is perfect).
            That(snake.ActualLength, Is.EqualTo(oracleQueue.Count), $"Turn {turnCounter}: ActualLength mismatch.");
            
            // Note: snake.Length represents occupied physical slots. For a stacked snake, 
            // this roughly matches the number of *unique* positions in the logical body 
            // (assuming the stack is all on one tile).
            var expectedPhysicalLength = oracleQueue.Distinct().Count();
            That(snake.Length, Is.EqualTo(expectedPhysicalLength), $"Turn {turnCounter}: Physical Length mismatch.");

            var isCurrentlyUnrolling = turnCounter <= initialCredits;
            if (isCurrentlyUnrolling)
            {
                // FASE DI UNROLL: La coda LOGICA è ancora ferma sulla cella di start
                // The Snake implementation handles this via Credits.
                That(snake.Tail, Is.EqualTo(initialTailPos), $"Turn {turnCounter}: Tail moved during unroll.");

                // Check Stack State
                if (turnCounter < initialCredits)
                    That(snake.IsGrowthPending, Is.True, $"Turn {turnCounter}: Growth should be pending (Stacked).");
                else
                    That(snake.IsGrowthPending, Is.False, $"Turn {turnCounter}: Last credit consumed.");
            }
            else
            {
                // FASE POST-UNROLL: La coda avanza normalmente
                That(snake.Tail, Is.EqualTo(oracleQueue.Peek()), $"Turn {turnCounter}: Tail stall after unroll.");
                That(snake.IsGrowthPending, Is.False, $"Turn {turnCounter}: Growth pending should be false.");
            }

            // Invariants
            That(snake.Head, Is.EqualTo(nextHead), "Head mismatch.");
            That(snake.Body.PopCount(), Is.EqualTo(expectedPhysicalLength), $"Turn {turnCounter}: PopCount mismatch.");

            currentHead = nextHead;
        }
    }

    [TestCaseSource(nameof(MovementStackedScenarios))]
    public void UpdateAfterMove_WhenEatingEveryTurn_ShouldGrowAndAnchorTail(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakePlacement _)
    {
        // Arrange
        var snake = context.Build();
        var snakeData = new Snake("hungry-hero", hp, body);

        var moves = new[] { Moves.Up, Moves.Right, Moves.Down, Moves.Left };

        // Act & Assert
        foreach (var move in moves)
        {
            context.Reset();
            snake.Initialize(in snakeData);

            var oracleQueue = new Queue<ushort>(body.Reverse());
            var currentHead = oracleQueue.Last();
            var startPosition = oracleQueue.Peek();

            var nextMove = move switch
            {
                Moves.Up => Moves.Right,
                Moves.Right => Moves.Down,
                Moves.Down => Moves.Left,
                Moves.Left => Moves.Up,
                _ => Moves.None
            };

            var pathDirections = new[] { move, nextMove };

            foreach (var direction in pathDirections)
            {
                var (currX, currY) = GetCoord(currentHead, env.Width);

                var stepsToWall = direction switch
                {
                    Moves.Up => env.Height - 1 - currY,
                    Moves.Right => env.Width - 1 - currX,
                    Moves.Down => currY,
                    Moves.Left => currX,
                    _ => 0
                };

                foreach (var __ in Enumerable.Range(1, stepsToWall))
                {
                    (currX, currY) = GetCoord(currentHead, env.Width);
                    var (nx, ny) = direction switch
                    {
                        Moves.Up => (currX, currY + 1),
                        Moves.Right => (currX + 1, currY),
                        Moves.Down => (currX, currY - 1),
                        Moves.Left => (currX - 1, currY),
                        _ => (currX, currY)
                    };

                    var nextHead = (ushort)(ny * env.Width + nx);

                    // Oracle Logic: Grow (Enqueue but NO Dequeue)
                    oracleQueue.Enqueue(nextHead);

                    // Execution
                    snake.UpdateAfterMove(nextHead, true, 0);

                    // Verification
                    That(snake.Hp, Is.EqualTo(100), "Full cure failed.");
                    That(snake.IsDead, Is.False, "Snake died.");

                    // LOGIC FIX: Check IsGrowthPending based on Stack state.
                    // Even with Eager execution, if the snake *remains* stacked (Tail == Neck),
                    // the implementation must maintain Credits > 0.
                    var expectedTail = oracleQueue.Peek();
                    var expectedNeck = oracleQueue.ElementAt(1);
                    var isOracleStacked = expectedTail == expectedNeck;
                    
                    That(snake.IsGrowthPending, Is.EqualTo(isOracleStacked), 
                        $"IsGrowthPending mismatch. OracleStacked: {isOracleStacked}.");

                    That(snake.Tail, Is.EqualTo(startPosition), "Tail moved from anchor.");
                    That(snake.Head, Is.EqualTo(nextHead), "Head mismatch.");
                    
                    // Length Verification (Dual Property)
                    That(snake.ActualLength, Is.EqualTo(oracleQueue.Count), "ActualLength mismatch.");
                    That(snake.Length, Is.EqualTo(oracleQueue.Distinct().Count()), "Physical Length mismatch.");

                    That(snake.Body.PopCount(), Is.EqualTo(oracleQueue.Distinct().Count()), "PopCount mismatch.");
                    That(snake.Body.IsSet(startPosition), Is.True, "Anchor bit lost.");

                    currentHead = nextHead;
                }
            }
        }
    }

    private static (int x, int y) GetCoord(ushort index, int width) => (index % width, index / width);
}