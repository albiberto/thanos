using Thanos.SourceGen;
using Thanos.Tests.Integration.WarSnake.Support;
using Thanos.War;
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
        snake.UpdateAfterMove(nextPos, ateFood: true, damage: 1);

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
        snake.UpdateAfterMove(nextPos, ateFood: false, damage: 1);

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
        snake.UpdateAfterMove(nextPos, ateFood: false, damage: hazardDamage);

        // Assert: Dies correctly without underflow
        That(snake.HP, Is.EqualTo(0), "HP should be zeroed.");
        That(snake.IsDead, Is.True, "Snake should die from hazard damage.");
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
        snake.UpdateAfterMove(nextPos, ateFood: true, damage: 0);

        // Assert: Operation is idempotent
        That(snake.Head, Is.EqualTo(initialHead), "Dead snake should not move.");
        That(snake.HP, Is.EqualTo(0), "Dead snake should not heal.");
        That(snake.Body.IsSet(nextPos), Is.False, "Dead snake should not occupy new bits.");
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
        That(snake.HP, Is.Zero, "HP mismatch.");
        That(snake.Body.PopCount(), Is.EqualTo(3), "Body should persist until external cleanup.");
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
        snake.UpdateAfterMove(targetPos, ateFood: false, damage: 1);

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
        snake.UpdateAfterMove(nextPos, ateFood: false, damage: damage);

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
        snake.UpdateAfterMove(nextPos, ateFood: true, damage: 0);

        // Assert
        That(snake.HP, Is.EqualTo(100), "HP should be capped at 100.");
        // Regression Check: ensure bytes don't overflow/wrap around
        That(snake.HP, Is.LessThanOrEqualTo(100)); 
    }
}