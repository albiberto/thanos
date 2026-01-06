using Thanos.SourceGen;
using Thanos.Tests.Integration.WarSnake.Support;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    /// <summary>
    ///     Verifies standard movement mechanics for an unrolled snake.
    ///     The Head advances, the Tail retracts, and invariants (Length, Bitboard) are maintained.
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenMovingNormally_ShouldAdvanceHeadAndClearTail(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        var snake = context.Build();
        snake.Initialize(new Snake("hero", hp, body));

        // Oracle: Calculate expected geometry based on direction
        var targetPos = GetNextPosition(body[0], facing, env.Width);
        var expectedTail = body[^2]; // The neck becomes the new Tail
        var oldTail = body[^1]; // The old Tail is dropped
        var expectedHp = hp > NormalDamage ? hp - NormalDamage : 0;

        // Act
        // Move without eating (Standard Step)
        snake.UpdateAfterMove(targetPos, false, NormalDamage);

        // Assert
        // 1. Vital Signs
        That(snake.HP, Is.EqualTo(expectedHp), "HP should decrease by damage amount.");
        That(snake.IsDead, Is.EqualTo(expectedHp == 0), "Dead status mismatch.");
        That(snake.IsGrowthPending, Is.False, "Standard move should not trigger growth.");

        if (snake.IsDead) return;

        // 2. Queue Geometry
        That(snake.Length, Is.EqualTo(body.Length), "Length must remain constant.");
        That(snake.Head, Is.EqualTo(targetPos), "Head did not move to target.");
        That(snake.Tail, Is.EqualTo(expectedTail), "Tail did not advance correctly.");

        if (body.Length > 2)
        {
            var expectedNeck = body[^3];
            That(snake.PreTail, Is.EqualTo(expectedNeck), "ElementBeforeTail (Neck) is incorrect.");
        }

        // 3. Bitboard Consistency
        That(snake.Body.IsSet(oldTail), Is.False, "Old tail bit was not cleared.");
        That(snake.Body.IsSet(targetPos), Is.True, "New head bit was not set.");
        That(snake.Body.IsSet(expectedTail), Is.True, "New tail bit missing.");

        // Paranoid: Ensure no phantom bits were created
        That(snake.Body.PopCount(), Is.EqualTo(body.Length), "Bitboard population count mismatch.");
    }

    /// <summary>
    ///     Verifies food consumption logic.
    ///     The snake grows (+1 Length), heals (Full Cure), and the Tail remains STATIONARY.
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenEatingFood_ShouldGrowAndHeal(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        var snake = context.Build();
        snake.Initialize(new Snake("hero", hp, body));

        var targetPos = GetNextPosition(body[0], facing, env.Width);
        var currentTail = body[^1]; // Tail should anchor here
        var currentNeck = body[^2]; // Neck remains same relative to Tail

        // Act
        // Move onto Food
        snake.UpdateAfterMove(targetPos, true, NormalDamage);

        // Assert
        // 1. Vital Signs
        That(snake.HP, Is.EqualTo(100), "Eating should trigger Full Cure (100 HP).");
        That(snake.IsDead, Is.False, "Snake should be alive after eating.");

        // 2. Growth Logic
        That(snake.Length, Is.EqualTo(body.Length + 1), "Length should increase by 1 immediately.");
        That(snake.IsGrowthPending, Is.True, "Growth pending flag should be set for next turn.");

        // 3. Queue Geometry (Anchor Check)
        That(snake.Head, Is.EqualTo(targetPos), "Head mismatch.");
        That(snake.Tail, Is.EqualTo(currentTail), "Tail should NOT move when eating (Anchored).");
        That(snake.PreTail, Is.EqualTo(currentNeck), "Neck should remain unchanged.");

        // 4. Bitboard Consistency
        That(snake.Body.IsSet(targetPos), Is.True, "New head bit missing.");
        That(snake.Body.IsSet(currentTail), Is.True, "Tail bit cleared incorrectly.");
        That(snake.Body.PopCount(), Is.EqualTo(body.Length + 1), "Bitboard population count mismatch (Length + 1).");
    }

    /// <summary>
    ///     Verifies the "Digestion" turn (the move immediately following food consumption).
    ///     The growth flag is consumed, and the tail finally advances.
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenDigestingFood_ShouldMoveTail(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        var snake = context.Build();
        snake.Initialize(new Snake("hero", 100, body));

        var eatPos = GetNextPosition(body[0], facing, env.Width);

        // Pre-Condition: Eat Food (Turn T) -> Length + 1, GrowthPending = True
        snake.UpdateAfterMove(eatPos, true, 0);

        // Prepare for Digestion (Turn T+1)
        var digestPos = GetNextPosition(eatPos, facing, env.Width);

        // Oracle: Tail advances now. 
        // Original: [H, B, T]. Eat: [H', H, B, T]. Digest: [H'', H', H, B].
        var expectedTail = body[^2];
        var expectedHp = 100 - NormalDamage;

        // Act
        // Move normally (Digest phase)
        snake.UpdateAfterMove(digestPos, false, NormalDamage);

        // Assert
        // 1. Vital Signs
        That(snake.HP, Is.EqualTo(expectedHp), "HP should decrease normally during digestion.");
        That(snake.IsGrowthPending, Is.False, "Growth flag should be consumed.");

        // 2. Queue Geometry
        That(snake.Length, Is.EqualTo(body.Length + 1), "Snake should maintain the gained length.");
        That(snake.Head, Is.EqualTo(digestPos), "Head mismatch.");
        That(snake.Tail, Is.EqualTo(expectedTail), "Tail MUST advance during digestion phase.");

        // 3. Bitboard Consistency
        That(snake.Body.IsSet(digestPos), Is.True, "Head bit missing.");
        That(snake.Body.IsSet(expectedTail), Is.True, "New tail bit missing.");
        That(snake.Body.PopCount(), Is.EqualTo(body.Length + 1), "PopCount mismatch.");
    }

    /// <summary>
    ///     Verifies consecutive eating behavior ("Chain Eating").
    ///     The tail should remain stationary for as many turns as food is eaten.
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenEatingConsecutively_ShouldKeepTailStationaryAgain(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        var snake = context.Build();
        snake.Initialize(new Snake("hero", 100, body));

        var pos1 = GetNextPosition(body[0], facing, env.Width);
        var pos2 = GetNextPosition(pos1, facing, env.Width);
        var originalTail = body[^1];

        // Act
        // Turn 1: Eat
        snake.UpdateAfterMove(pos1, true, 0);

        // Turn 2: Eat Again
        snake.UpdateAfterMove(pos2, true, 0);

        // Assert
        // 1. Vital Signs
        That(snake.HP, Is.EqualTo(100), "Full cure failed on second eat.");
        That(snake.IsGrowthPending, Is.True, "Growth flag should remain set (re-armed).");

        // 2. Queue Geometry
        That(snake.Length, Is.EqualTo(body.Length + 2), "Length should increase by 2.");
        That(snake.Head, Is.EqualTo(pos2), "Head mismatch.");
        That(snake.Tail, Is.EqualTo(originalTail), "Tail should stay anchored for 2 consecutive turns.");

        // 3. Bitboard Consistency
        That(snake.Body.IsSet(body[0]), Is.True, "Original Head lost.");
        That(snake.Body.IsSet(pos1), Is.True, "First food position lost.");
        That(snake.Body.IsSet(pos2), Is.True, "Second food position missing.");
        That(snake.Body.PopCount(), Is.EqualTo(body.Length + 2), "PopCount mismatch (Length + 2).");
    }

    private static ushort GetNextPosition(ushort current, SnakeFacing facing, int width)
    {
        // Logic: 0,0 is Bottom-Left. 
        // Generates valid coordinates assuming the test scenarios provide safety margins.
        int dx = 0, dy = 0;
        switch (facing)
        {
            case SnakeFacing.Up: dy = 1; break;
            case SnakeFacing.Down: dy = -1; break;
            case SnakeFacing.Left: dx = -1; break;
            case SnakeFacing.Right: dx = 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(facing), facing, null);
        }

        var y = current / width;
        var x = current % width;

        return (ushort)((y + dy) * width + x + dx);
    }

    [Test]
    public void UpdateAfterMove_WhenChasingOwnTail_ShouldMaintainBodyIntegrity()
    {
        // Scenario "Ouroboros":
        // Il serpente si muove esattamente nella cella lasciata libera dalla coda.
        // Questo è legale. La Bitboard deve riflettere che quella cella è ORA occupata dalla Testa.

        // Arrange
        var context = new SnakeMemoryContext(16, 64, "TailChase");
        var snake = context.Build();

        // Body: Head(1), Tail(0). Length 2.
        // Move to 0 (Where Tail is).
        ushort[] body = [1, 0];
        snake.Initialize(new Snake("hero", 100, body));

        var targetPos = body[^1]; // 0 (Tail position)

        // Act
        snake.UpdateAfterMove(targetPos, false, 1);

        // Assert
        // 1. Geometry
        That(snake.Head, Is.EqualTo(targetPos), "Head must be at the old tail position.");
        That(snake.Tail, Is.EqualTo(body[0]), "Tail must calculate new position correctly (Old Head becomes Tail in Len 2).");

        // 2. Bitboard Logic
        // Se la logica fosse sbagliata (es: SetHead poi UnsetTail), il bit a 0 verrebbe spento.
        // Deve essere: UnsetTail (0 -> Off) POI SetHead (0 -> On). Risultato: 0 è On.
        That(snake.Body.IsSet(targetPos), Is.True, "The position shared by OldTail and NewHead MUST be set.");

        // 3. PopCount
        That(snake.Body.PopCount(), Is.EqualTo(2), "Snake must preserve its mass (2 bits set).");
    }
}