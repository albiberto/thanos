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

        // Oracle: We use a Queue to model the physical snake segments [Tail -> Body -> Head].
        // Reverse body (Head->Tail) to match FIFO enqueue order.
        var oracleQueue = new Queue<ushort>(body.Reverse());
        var currentHead = oracleQueue.Last();

        var moves = new[] { Moves.Up, Moves.Right, Moves.Down, Moves.Left };
        var expectedHp = hp;

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
            oracleQueue.Dequeue();


            // Execution
            snake.UpdateAfterMove(nextHead, false, 1);

            // Verification
            expectedHp--;
            That(snake.HP, Is.EqualTo(expectedHp), $"HP mismatch at {nextHead}.");
            That(snake.IsDead, Is.EqualTo(expectedHp <= 0), "IsDead logic failed.");
            That(snake.IsGrowthPending, Is.False, "Snake should not grow without food.");

            That(snake.Length, Is.EqualTo(body.Length), "Length mismatch.");
            That(snake.Head, Is.EqualTo(oracleQueue.Last()), "Head mismatch.");
            That(snake.Tail, Is.EqualTo(oracleQueue.Peek()), "Tail mismatch.");

            if (body.Length >= 2) That(snake.ElementBeforeTail, Is.EqualTo(oracleQueue.ElementAt(1)), "Neck mismatch.");

            var expectedUniqueBits = oracleQueue.Distinct().Count();
            That(snake.Body.PopCount(), Is.EqualTo(expectedUniqueBits), "PopCount mismatch.");

            foreach (var segment in oracleQueue) That(snake.Body.IsSet(segment), Is.True, $"Bitboard missing segment {segment}.");
            
            currentHead = nextHead;
            
        }
    }

    [TestCaseSource(nameof(MovementStackedScenarios))]
    public void UpdateAfterMove_WhenEatingEveryTurn_ShouldGrowAndAnchorTail(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakePlacement _)
    {
        // Arrange
        var snake = context.Build();
        var snakeData = new Snake("hungry-hero", hp, body);
        snake.Initialize(in snakeData);

        // Oracle: Tail stays anchored (never Dequeue)
        var oracleQueue = new Queue<ushort>(body.Reverse());
        var startPosition = oracleQueue.Peek();

        var currentHead = body[0];
        var (currX, currY) = GetCoord(currentHead, env.Width);

        var pathSegments = new[]
        {
            (Dir: Moves.Up, Steps: env.Height - 1 - currY),
            (Dir: Moves.Right, Steps: env.Width - 1 - currX),
            (Dir: Moves.Down, Steps: env.Height - 1),
            (Dir: Moves.Left, Steps: env.Width - 1)
        };

        // Act & Assert
        foreach (var (dir, steps) in pathSegments)
        foreach (var __ in Enumerable.Range(1, steps))
        {
            // Oracle Step
            var (nx, ny) = dir switch
            {
                Moves.Up => (currX, currY + 1),
                Moves.Right => (currX + 1, currY),
                Moves.Down => (currX, currY - 1),
                Moves.Left => (currX - 1, currY),
                _ => (currX, currY)
            };

            var nextHead = (ushort)(ny * env.Width + nx);
            currX = nx;
            currY = ny;

            oracleQueue.Enqueue(nextHead);

            // Execution
            snake.UpdateAfterMove(nextHead, true, 0);

            // Verification
            That(snake.HP, Is.EqualTo(100), "Full cure failed.");
            That(snake.IsDead, Is.False, "Snake died.");
            That(snake.IsGrowthPending, Is.True, "Growth pending failed.");

            That(snake.Tail, Is.EqualTo(startPosition), "Tail moved from anchor.");
            That(snake.Head, Is.EqualTo(nextHead), "Head mismatch.");
            That(snake.Length, Is.EqualTo(oracleQueue.Count), "Length mismatch.");

            That(snake.Body.PopCount(), Is.EqualTo(oracleQueue.Distinct().Count()), "PopCount mismatch.");
            That(snake.Body.IsSet(startPosition), Is.True, "Anchor bit lost.");
        }
    }

    private static (int x, int y) GetCoord(ushort index, int width) => (index % width, index / width);
}