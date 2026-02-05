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
        snake.Initialize(new("hero", 50, body)); // Start with 50 HP

        var nextPos = (ushort)(body[0] + 1);
        const byte HazardDamage = 15;

        // Act
        // ateFood = true, damage = 15
        snake.UpdateAfterMove(nextPos, true, HazardDamage);

        // Assert
        That(snake.Hp, Is.EqualTo(100), "Food healing failed to override hazard damage.");
        That(snake.IsDead, Is.False, "Snake should not die when eating.");
        
        // Non-stacked snake eating: Grows immediately. Credits 0 -> 1 -> 0.
        // So pending should be False.
        That(snake.IsGrowthPending, Is.False, "Growth should be consumed immediately for non-stacked snake.");
        
        // Length check
        That(snake.ActualLength, Is.EqualTo(4), "Snake should have grown.");
        That(snake.Length, Is.EqualTo(4), "Physical length should match.");
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
        snake.Initialize(new("hero", InitialHP, body));

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
        
        // Arrange
        var context = new SnakeMemoryContext(16, 64, "SuicideIntegrity");
        var snake = context.Build();

        // Body: Head(2), Body(1), Tail(0)
        ushort[] body = [2, 1, 0];
        snake.Initialize(new("hero", 100, body));

        var suicidePos = body[1]; // Position 1 (The Neck/Body)

        // Act
        snake.UpdateAfterMove(suicidePos, false, 1);

        // Assert
        That(snake.Head, Is.EqualTo(suicidePos), "Head logic failed to update.");
        That(snake.ActualLength, Is.EqualTo(3), "Length should not change.");
        
        // Bitboard Integrity
        That(snake.Body.IsSet(0), Is.False, "Tail was not cleared.");
        That(snake.Body.IsSet(1), Is.True, "Collision point (Neck) should remain set.");
        That(snake.Body.IsSet(2), Is.True, "Old Head should remain set.");
        
        That(snake.Body.PopCount(), Is.EqualTo(2), "Bitboard PopCount should reflect spatial overlap.");
    }
}