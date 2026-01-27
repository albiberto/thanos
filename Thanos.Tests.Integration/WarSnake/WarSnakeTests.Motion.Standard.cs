using Thanos.Tests.Integration.Support;
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
        snake.Initialize(new("hero", hp, body));

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
        That(snake.Hp, Is.EqualTo(expectedHp), "HP should decrease by damage amount.");
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
        snake.Initialize(new("hero", hp, body));

        var targetPos = GetNextPosition(body[0], facing, env.Width);
        var currentTail = body[^1]; // Tail should anchor here
        var currentNeck = body[^2]; // Neck remains same relative to Tail

        // Act
        // Move onto Food
        snake.UpdateAfterMove(targetPos, true, NormalDamage);

        // Assert
        // 1. Vital Signs
        That(snake.Hp, Is.EqualTo(100), "Eating should trigger Full Cure (100 HP).");
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
    /// <summary>
    ///     Verifies the "Digestion" turn (the move immediately following food consumption).
    ///     In Battlesnake, a single food item keeps the tail stationary for TWO turns total:
    ///     1. The turn it is eaten (immediate growth).
    ///     2. The following turn (consuming the growth credit).
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenDigestingFood_ShouldKeepTailStationaryDueToCredit(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        var snake = context.Build();
        snake.Initialize(new("hero", 100, body)); // Initial Length: e.g., 10

        var originalTail = body[^1];
        var eatPos = GetNextPosition(body[0], facing, env.Width);

        // --- STEP 1: Turn T (Eat Food) ---
        // ateFood: true -> Skip Dequeue. Length becomes 11. Credit added: 1.
        snake.UpdateAfterMove(eatPos, true, 0);

        That(snake.Length, Is.EqualTo(body.Length + 1), "Turn T: Length should increase immediately.");
        That(snake.Tail, Is.EqualTo(originalTail), "Turn T: Tail must stay anchored.");
        That(snake.IsGrowthPending, Is.True, "Turn T: Should have a pending credit.");

        // --- STEP 2: Turn T+1 (Digestion Phase) ---
        var digestPos = GetNextPosition(eatPos, facing, env.Width);

        // Act: Move onto empty space
        // wasGrowing: true (consumes credit) -> Skip Dequeue. Length becomes 12.
        snake.UpdateAfterMove(digestPos, false, NormalDamage);

        // Assert
        // 1. Vital Signs
        That(snake.Hp, Is.EqualTo(100 - NormalDamage), "HP should decrease normally.");
        That(snake.IsGrowthPending, Is.False, "Credit must be consumed.");

        // 2. Queue Geometry
        // Rule: The tail stays stationary AGAIN because the credit from eating is being processed.
        That(snake.Length, Is.EqualTo(body.Length + 2), "Turn T+1: Length increases again as tail stays stationary.");
        That(snake.Head, Is.EqualTo(digestPos), "Head mismatch.");
        That(snake.Tail, Is.EqualTo(originalTail), "Turn T+1: Tail must STILL be anchored due to credit.");

        // 3. Bitboard Consistency
        That(snake.Body.IsSet(originalTail), Is.True, "Tail bit must still be set.");
        That(snake.Body.PopCount(), Is.EqualTo(body.Length + 2), "Bitboard population count mismatch.");

        // --- STEP 3: Turn T+2 (Final Unroll) ---
        var finalPos = GetNextPosition(digestPos, facing, env.Width);
        var expectedNewTail = body[^2]; // Finally, the tail moves forward

        // wasGrowing: false && ateFood: false -> Dequeue occurs. Length stays 12.
        snake.UpdateAfterMove(finalPos, false, NormalDamage);

        That(snake.Length, Is.EqualTo(body.Length + 2), "Turn T+2: Length stays stable as tail finally moves.");
        That(snake.Tail, Is.EqualTo(expectedNewTail), "Turn T+2: Tail must finally advance.");
        That(snake.Body.IsSet(originalTail), Is.False, "Turn T+2: Old tail bit must be cleared.");
    }

    /// <summary>
    ///     Verifies consecutive eating behavior ("Chain Eating").
    ///     The tail should remain stationary for as many turns as food is eaten.
    /// </summary>
    [TestCaseSource(nameof(MovementUnrolledScenarios))]
    public void UpdateAfterMove_WhenEatingConsecutively_ShouldStackGrowthCreditsAndKeepTailStationary(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
    {
        // Arrange
        // Production Code: The LightSpeed logic relies on WarSnakeLife.Credits
        var snake = context.Build();
        snake.Initialize(new("hero", 100, body));

        var pos1 = GetNextPosition(body[0], facing, env.Width);
        var pos2 = GetNextPosition(pos1, facing, env.Width);
        var originalTail = body[^1];

        // Act & Assert: Turn 1 (First Food)
        // Rule: Food consumption scheduled. Credits: 0 -> 1.
        snake.UpdateAfterMove(pos1, true, 0);

        That(snake.IsGrowthPending, Is.True, "Growth should be pending after first eat.");
        That(snake.Length, Is.EqualTo(body.Length + 1), "Length must increase immediately.");
        That(snake.Tail, Is.EqualTo(originalTail), "Tail must stay anchored on the turn food is consumed.");

        // Act & Assert: Turn 2 (Second Food - Chain)
        // Rule: Credits should stack. Credits: 1 -> 2.
        // Note: UpdateAfterMove calls ConsumePendingGrowth (1 -> 0) then ScheduleGrowth (0 -> 1)
        // because at Turn 2 we consume the growth of Turn 1 but add a new one.
        // Actually, in our logic, if we eat while a growth is pending, the tail still doesn't move.
        snake.UpdateAfterMove(pos2, true, 0);

        // Final Verifications
        // 1. Vital Signs & Stacked State
        That(snake.Hp, Is.EqualTo(100), "Full cure failed on second eat.");
        That(snake.IsGrowthPending, Is.True, "Growth flag should remain set.");

        // 2. Queue Geometry (The "Human" logic check)
        // After eating twice, length is original + 2 and tail hasn't moved a single unit.
        That(snake.Length, Is.EqualTo(body.Length + 2), "Length should increase by 2 after consecutive eating.");
        That(snake.Head, Is.EqualTo(pos2), "Head position mismatch.");
        That(snake.Tail, Is.EqualTo(originalTail), "Tail must stay anchored for 2 turns of consecutive eating.");

        // 3. Bitboard Consistency (Paranoia Effect)
        // Verify every segment is correctly registered in the Bitboard
        That(snake.Body.IsSet(body[0]), Is.True, "Original head position bit lost.");
        That(snake.Body.IsSet(pos1), Is.True, "First food position bit lost.");
        That(snake.Body.IsSet(pos2), Is.True, "Second food position bit missing.");

        // Invariants check inside the test
        var expectedUniqueBits = body.Length + 2;
        That(snake.Body.PopCount(), Is.EqualTo(expectedUniqueBits), $"PopCount mismatch. Expected {expectedUniqueBits} unique bits.");
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
        snake.Initialize(new("hero", 100, body));

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