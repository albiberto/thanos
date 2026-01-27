using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    [Test]
    public void UpdateAfterMove_WhenEatingInHazard_ShouldPrioritizeFoodHealingOverDamage()
    {
        // Scenario: Snake moves into a cell that has BOTH Food and Hazard.
        // Rule: Food consumption (Full Cure) overrides hazard damage applied in the same turn.

        // Arrange
        var context = new SnakeMemoryContext(16, 64, "HazardFoodPriority");
        var snake = context.Build();

        ushort[] body = [10, 11, 12];
        snake.Initialize(new Snake("hero", 50, body)); // Start with 50 HP

        var nextPos = (ushort)(body[0] + 1);
        const byte HazardDamage = 15;

        // Act
        // ateFood = true, damage = 15
        snake.UpdateAfterMove(nextPos, true, HazardDamage);

        // Assert
        That(snake.Hp, Is.EqualTo(100), "Food healing failed to override hazard damage.");
        That(snake.IsDead, Is.False, "Snake should not die when eating.");
        That(snake.IsGrowthPending, Is.True, "Growth should be scheduled.");
    }

    [Test]
    public void UpdateAfterMove_WhenDamageIsZero_ShouldMaintainCurrentHP()
    {
        // Scenario: Zero-damage move (e.g. custom rules or god mode).

        // Arrange
        var context = new SnakeMemoryContext(16, 64, "ZeroDamage");
        var snake = context.Build();

        ushort[] body = [10, 11, 12];
        const byte InitialHP = 80;
        snake.Initialize(new Snake("hero", InitialHP, body));

        var nextPos = (ushort)(body[0] + 1);

        // Act
        snake.UpdateAfterMove(nextPos, false, 0);

        // Assert
        That(snake.Hp, Is.EqualTo(InitialHP), "HP changed unexpectedly on zero-damage move.");
    }

    [Test]
    public void UpdateAfterMove_WhenCollidingWithNeck_ShouldMaintainStructConsistency()
    {
        // Scenario: Suicide move (180 degree turn).
        // Head moves exactly to where the 'Neck' (ElementBeforeTail in logic, but technically Body[1]) is.
        // Note: For Length 3 [H, B, T], moving H to B is a collision.
        // We verify that Bitboard logic handles the overlapping set correctly (Idempotency).

        // Arrange
        var context = new SnakeMemoryContext(16, 64, "SuicideIntegrity");
        var snake = context.Build();

        // Body: Head(2), Body(1), Tail(0)
        ushort[] body = [2, 1, 0];
        snake.Initialize(new Snake("hero", 100, body));

        var suicidePos = body[1]; // Position 1 (The Neck/Body)

        // Act
        snake.UpdateAfterMove(suicidePos, false, 1);

        // Assert
        // 1. Queue State
        That(snake.Head, Is.EqualTo(suicidePos), "Head logic failed to update.");
        That(snake.Length, Is.EqualTo(3), "Length should not change.");

        // 2. Bitboard Integrity
        // Tail (0) should be unset.
        // Head (2) remains set (it's now part of body).
        // NewHead (1) remains set (collision).

        That(snake.Body.IsSet(0), Is.False, "Tail was not cleared.");
        That(snake.Body.IsSet(1), Is.True, "Collision point (Neck) should remain set.");
        That(snake.Body.IsSet(2), Is.True, "Old Head should remain set.");

        // 3. PopCount Anomaly Check
        // Length is 3, but spatially we only occupy 2 unique squares (1 and 2).
        // The Head is physically 'inside' the body.
        That(snake.Body.PopCount(), Is.EqualTo(2), "Bitboard PopCount should reflect spatial overlap (loss of 1 unique bit).");
    }
}