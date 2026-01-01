using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    // --- RULES & EDGE CASES ---

    [Test]
    public void UpdateAfterMove_WhenHpIsOne_AndEatsFood_ShouldSurviveAndResetHealth()
    {
        // GIVEN: A snake on the brink of starvation (1 HP)
        var context = new SnakeMemoryContext(16, 64, "StarvationRescue");
        var snake = context.Build();

        // Body length 3
        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 1, body));

        const ushort nextPos = 3;

        // WHEN: Moves onto food
        // Standard rule: Food resets health to 100. This overrides the turn damage.
        snake.UpdateAfterMove(nextPos, true, 1);

        // THEN: Snake lives and is fully healed
        That(snake.IsDead, Is.False, "Snake should satisfy hunger before starving.");
        That(snake.HP, Is.EqualTo(100), "Health should be fully restored.");
        That(snake.Length, Is.EqualTo(4), "Snake should grow.");
        That(snake.Head, Is.EqualTo(nextPos), "Snake should move to food.");
    }

    [Test]
    public void UpdateAfterMove_WhenHpIsOne_AndMovesEmpty_ShouldDie()
    {
        // GIVEN: A snake on the brink of starvation (1 HP)
        var context = new SnakeMemoryContext(16, 64, "StarvationDeath");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 1, body));

        const ushort nextPos = 3;

        // WHEN: Moves onto empty space (Normal damage applied)
        snake.UpdateAfterMove(nextPos, false, 1);

        // THEN: Snake dies
        That(snake.HP, Is.EqualTo(0), "HP should drop to 0.");
        That(snake.IsDead, Is.True, "Snake should be dead.");

        // Critical Rule: Even if dead, the head *did* move legally into the tile before dying.
        // The Bitboard/Queue should reflect the move, even if the snake is now a corpse.
        // This is important for "Simultaneous Death" collision resolution.
        That(snake.Head, Is.EqualTo(nextPos), "Head position should update even on death turn.");
    }

    [Test]
    public void UpdateAfterMove_WhenEnteringHazard_AndDamageExceedsHP_ShouldDie()
    {
        // GIVEN: Snake with low health
        var context = new SnakeMemoryContext(16, 64, "HazardDeath");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 10, body)); // 10 HP

        const ushort nextPos = 3;
        const int hazardDamage = 15; // Lethal damage

        // WHEN: Moves into hazard
        snake.UpdateAfterMove(nextPos, false, hazardDamage);

        // THEN: Dies correctly
        That(snake.HP, Is.EqualTo(0), "HP should be zeroed (no negative overflow).");
        That(snake.IsDead, Is.True, "Snake should die from hazard damage.");
    }

    [Test]
    public void UpdateAfterMove_WhenAlreadyDead_ShouldNotMoveOrChangeState()
    {
        // GIVEN: A dead snake
        var context = new SnakeMemoryContext(16, 64, "ZombieCheck");
        var snake = context.Build();

        ushort[] body = [0, 1, 2];
        snake.Initialize(new Snake("hero", 0, body)); // 0 HP = Dead on arrival

        var initialHead = snake.Head;
        const ushort nextPos = 3;

        // WHEN: Update is called on a corpse
        snake.UpdateAfterMove(nextPos, true, 0);

        // THEN: Nothing happens (Idempotency)
        That(snake.Head, Is.EqualTo(initialHead), "Dead snake should not move.");
        That(snake.HP, Is.EqualTo(0), "Dead snake should not heal.");
        That(snake.Body.IsSet(nextPos), Is.False, "Dead snake should not occupy new bits.");
    }

    [Test]
    public void Kill_WhenCalled_ShouldPreserveBodyGeometryWaitingForCleanup()
    {
        // GIVEN: Alive snake
        var context = new SnakeMemoryContext(16, 64, "ExplicitKill");
        var snake = context.Build();
        ushort[] body = [10, 11, 12];
        snake.Initialize(new Snake("hero", 100, body));

        // WHEN: Explicitly killed (e.g. collision detected by Arena)
        snake.Kill();

        // THEN: Flag is dead, but body remains (Arena handles removal/conversion to food)
        That(snake.IsDead, Is.True);
        That(snake.HP, Is.Zero);

        // The WarSnake struct is "dumb data". It should not self-destruct its bitboard 
        // immediately upon Kill(), because we might need the body data for 
        // resolving other simultaneous collisions in the same tick.
        That(snake.Body.PopCount(), Is.EqualTo(3), "Body should persist until external cleanup.");
    }
}