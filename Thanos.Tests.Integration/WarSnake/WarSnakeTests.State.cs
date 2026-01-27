using Thanos.SourceGen;
using Thanos.Tests.Integration.WarSnake.Support;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    [TestCaseSource(nameof(ExhaustiveScenarios))]
    public void Initialize_WhenExhaustiveScenarios_ShouldMaintainInvariants(SnakeMemoryContext context, Environment _, ushort[] body, byte hp, SnakeStartCorner __)
    {
        // Arrange
        var snake = context.Build();
        var data = new Snake("hero", hp, body);

        // Act
        snake.Initialize(in data);

        // Assert
        // 1. Vital Signs (Direct Properties)
        That(snake.Hp, Is.EqualTo(hp), "HP property mismatch.");
        That(snake.IsDead, Is.EqualTo(hp == 0), "IsDead property logic failed.");
        That(snake.IsGrowthPending, Is.False, "IsGrowthPending should be false after initialization.");

        // 2. Queue Geometry (Direct Properties)
        That(snake.Length, Is.EqualTo(body.Length), "Queue Length mismatch.");
        That(snake.Head, Is.EqualTo(body[0]), "Head position mismatch (Queue Head).");
        That(snake.Tail, Is.EqualTo(body[^1]), "Tail position mismatch (Queue Tail).");

        if (body.Length >= 2)
            That(snake.PreTail, Is.EqualTo(body[^2]), "ElementBeforeTail mismatch (Queue Neck).");

        // 3. Bitboard Consistency (Indirect Properties)
        // A. Population Count Integrity
        // Since our generator creates non-overlapping bodies, the number of set bits MUST exactly match the length.
        That(snake.Body.PopCount(), Is.EqualTo(body.Length), "Bitboard PopCount does not match Snake Length (Phantom or missing bits).");

        // B. Spatial Verification (Lookup)
        // Verify that IsOnBody() and Bitboard.IsSet() return true for EVERY segment.
        for (var i = 0; i < body.Length; i++)
        {
            var segment = body[i];
            That(snake.Body.IsSet(segment), Is.True, $"Bitboard check failed at index {i} (Pos {segment}).");
            That(snake.IsOnBody(segment), Is.True, $"IsOnBody helper failed at index {i} (Pos {segment}).");
        }
    }

    [Test]
    public void Kill_WhenCalled_ShouldPreserveBodyGeometryWaitngForCleanup()
    {
        // Arrange: Alive snake
        var context = new SnakeMemoryContext(16, 64, "ExplicitKill");
        var snake = context.Build();
        ushort[] body = [10, 11, 12];
        snake.Initialize(new Snake("hero", 100, body));

        // Act: Explicitly kill (e.g. collision detected)
        snake.Kill();

        // Assert: Flag is dead, but body remains for simultaneous collision resolution
        That(snake.IsDead, Is.True, "IsDead flag mismatch.");
        That(snake.Hp, Is.Zero, "HP mismatch.");
        That(snake.Body.PopCount(), Is.EqualTo(3), "Body should persist until external cleanup.");
    }

    [Test]
    public void UpdateAfterMove_WhenAlreadyDead_ShouldNotMoveOrChangeState()
    {
        // Arrange: A dead snake
        var context = new SnakeMemoryContext(16, 64, "ZombieCheck");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 0, body));

        var initialHead = snake.Head;
        const ushort nextPos = 3;

        // Act: Attempt to update a corpse
        snake.UpdateAfterMove(nextPos, true, 0);

        // Assert: Operation is idempotent
        That(snake.Head, Is.EqualTo(initialHead), "Dead snake should not move.");
        That(snake.Hp, Is.EqualTo(0), "Dead snake should not heal.");
        That(snake.Body.IsSet(nextPos), Is.False, "Dead snake should not occupy new bits.");
    }
}