using Thanos.Common;
using Thanos.SourceGen;
using Thanos.Tests.Integration.WarSnake.Support;
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

        // Oracle: Physical segments [Tail -> Body -> Head]
        var oracleQueue = new Queue<ushort>(body.Reverse());
        var currentHead = oracleQueue.Last();

        var moves = new[] { Moves.Up, Moves.Right, Moves.Down, Moves.Left };
        var expectedHp = (int)hp;

        // Act & Assert
        foreach (var move in moves)
        foreach (var __ in Enumerable.Range(1, env.Width / 2))
        {
            // Oracle Step
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

            oracleQueue.Enqueue(nextHead);
            oracleQueue.Dequeue(); // Logic: Move without growth (constant length)

            // Execution
            snake.UpdateAfterMove(nextHead, false, NormalDamage);

            // Verification
            expectedHp -= NormalDamage;
            That(snake.HP, Is.EqualTo(expectedHp), $"HP mismatch at {nextHead}.");
            That(snake.IsDead, Is.EqualTo(expectedHp <= 0), "IsDead logic failed.");
            That(snake.IsGrowthPending, Is.False, "Snake should not grow without food.");

            That(snake.Length, Is.EqualTo(body.Length), "Length mismatch.");
            That(snake.Head, Is.EqualTo(oracleQueue.Last()), "Head mismatch.");
            That(snake.Tail, Is.EqualTo(oracleQueue.Peek()), "Tail mismatch.");

            if (body.Length >= 2)
                That(snake.PreTail, Is.EqualTo(oracleQueue.ElementAt(1)), "Neck mismatch.");

            var expectedTail = oracleQueue.Peek();
            var expectedNeck = oracleQueue.ElementAt(1);
            var isOracleStacked = expectedTail == expectedNeck;

            That(snake.IsTailStacked, Is.EqualTo(isOracleStacked), $"IsTailStacked logic failed. Expected {isOracleStacked} (Tail:{expectedTail} vs Neck:{expectedNeck}).");

            var expectedUniqueBits = oracleQueue.Distinct().Count();
            That(snake.Body.PopCount(), Is.EqualTo(expectedUniqueBits), "PopCount mismatch.");

            foreach (var segment in oracleQueue)
                That(snake.Body.IsSet(segment), Is.True, $"Bitboard missing segment {segment}.");

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
            // 1. Reset for the new direction (Start fresh from center for each cardinal test)
            context.Reset();
            snake.Initialize(in snakeData);

            var oracleQueue = new Queue<ushort>(body.Reverse());
            var currentHead = oracleQueue.Last();
            var startPosition = oracleQueue.Peek();

            // 2. Define L-Shape Path: Primary Direction -> Clockwise Direction
            var nextMove = move switch
            {
                Moves.Up => Moves.Right,
                Moves.Right => Moves.Down,
                Moves.Down => Moves.Left,
                Moves.Left => Moves.Up,
                _ => Moves.None
            };

            var pathDirections = new[] { move, nextMove };

            // 3. Simulation Loop
            foreach (var direction in pathDirections)
            {
                // Calculate steps dynamically based on CURRENT head position relative to the board edges.
                // This replaces the complex pre-calculation logic.
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
                    // Oracle Step
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

                    oracleQueue.Enqueue(nextHead);
                    // Logic: Continuous growth (no Dequeue), Tail stays anchored

                    // Execution
                    snake.UpdateAfterMove(nextHead, true, 0);

                    // Verification
                    That(snake.HP, Is.EqualTo(100), "Full cure failed.");
                    That(snake.IsDead, Is.False, "Snake died.");
                    That(snake.IsGrowthPending, Is.True, "Growth pending failed.");

                    That(snake.Tail, Is.EqualTo(startPosition), "Tail moved from anchor.");
                    That(snake.Head, Is.EqualTo(nextHead), "Head mismatch.");
                    That(snake.Length, Is.EqualTo(oracleQueue.Count), "Length mismatch.");

                    var expectedTail = oracleQueue.Peek();
                    var expectedNeck = oracleQueue.ElementAt(1);
                    var isOracleStacked = expectedTail == expectedNeck;

                    That(snake.IsTailStacked, Is.EqualTo(isOracleStacked), $"IsTailStacked logic failed. Expected {isOracleStacked} (Tail:{expectedTail} vs Neck:{expectedNeck}).");

                    That(snake.Body.PopCount(), Is.EqualTo(oracleQueue.Distinct().Count()), "PopCount mismatch.");
                    That(snake.Body.IsSet(startPosition), Is.True, "Anchor bit lost.");

                    currentHead = nextHead;
                }
            }
        }
    }

    private static (int x, int y) GetCoord(ushort index, int width) => (index % width, index / width);
}