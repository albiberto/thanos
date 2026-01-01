using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    [Test]
    public void UpdateAfterMove_WhenHpIsOne_AndEatsFood_ShouldSurviveAndResetHealth()
    {
        // Arrange: Build a snake on the brink of starvation (1 HP)
        var context = new SnakeMemoryContext(16, 64, "StarvationRescue");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 1, body));

        const ushort nextPos = 3;

        // Act: Move onto food. Food logic should override starvation damage.
        snake.UpdateAfterMove(nextPos, true, 1);

        // Assert: Snake lives, grows, and heals
        That(snake.IsDead, Is.False, "Snake should satisfy hunger before starving.");
        That(snake.HP, Is.EqualTo(100), "Health should be fully restored.");
        That(snake.Length, Is.EqualTo(4), "Snake should grow.");
        That(snake.Head, Is.EqualTo(nextPos), "Snake should move to food.");
    }

    [Test]
    public void UpdateAfterMove_WhenHpIsOne_AndMovesEmpty_ShouldDie()
    {
        // Arrange: Build a snake on the brink of starvation
        var context = new SnakeMemoryContext(16, 64, "StarvationDeath");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 1, body));

        const ushort nextPos = 3;

        // Act: Move onto empty space (Normal damage applied)
        snake.UpdateAfterMove(nextPos, false, 1);

        // Assert: Snake dies but position updates (for collision resolution)
        That(snake.HP, Is.EqualTo(0), "HP should drop to 0.");
        That(snake.IsDead, Is.True, "Snake should be dead.");
        That(snake.Head, Is.EqualTo(nextPos), "Head position should update even on death turn.");
    }

    [Test]
    public void UpdateAfterMove_WhenEnteringHazard_AndDamageExceedsHP_ShouldDie()
    {
        // Arrange: Snake with low health
        var context = new SnakeMemoryContext(16, 64, "HazardDeath");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 10, body));

        const ushort nextPos = 3;
        const int hazardDamage = 15;

        // Act: Move into hazard with lethal damage
        snake.UpdateAfterMove(nextPos, false, hazardDamage);

        // Assert: Dies correctly without underflow
        That(snake.HP, Is.EqualTo(0), "HP should be zeroed.");
        That(snake.IsDead, Is.True, "Snake should die from hazard damage.");
    }


    [Test]
    public void UpdateAfterMove_WhenTakingNonLethalDamage_ShouldSurvive()
    {
        // Arrange
        var context = new SnakeMemoryContext(16, 64, "Survival");
        var snake = context.Build();

        ushort[] body = [10, 11, 12];
        byte startHp = 50;
        byte damage = 14; // Hazard damage

        snake.Initialize(new Snake("hero", startHp, body));
        var nextPos = (ushort)(body[0] + 1);

        // Act
        snake.UpdateAfterMove(nextPos, false, damage);

        // Assert
        var expectedHp = startHp - damage;

        That(snake.HP, Is.EqualTo(expectedHp), "HP calculation error.");
        That(snake.IsDead, Is.False, "Snake should strictly survive positive HP.");
        That(snake.Head, Is.EqualTo(nextPos), "Snake should complete the move.");
    }

    [Test]
    public void UpdateAfterMove_WhenHealingFromHighHealth_ShouldCapAtMax()
    {
        // Arrange
        var context = new SnakeMemoryContext(16, 64, "Overheal");
        var snake = context.Build();

        ushort[] body = [5, 6, 7];
        byte startHp = 99;

        snake.Initialize(new Snake("hero", startHp, body));
        var nextPos = (ushort)(body[0] + 1); // Food here (simulated)

        // Act
        snake.UpdateAfterMove(nextPos, true, 0);

        // Assert
        That(snake.HP, Is.EqualTo(100), "HP should be capped at 100.");
        // Regression Check: ensure bytes don't overflow/wrap around
        That(snake.HP, Is.LessThanOrEqualTo(100));
    }
}