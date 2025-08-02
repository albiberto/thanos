using Thanos.BitMasks;

namespace Thanos.Tests;

[TestFixture]
public unsafe class BattleSnakeTests
{
    private BattleSnake* _snake;
    private ushort* _bodyPtr; // Puntatore al corpo, pre-calcolato per ridurre la duplicazione.
    private IntPtr _memory;

    private const int MaxSnakeLength = 256;
    private const int MemorySize = BattleSnake.HeaderSize + (MaxSnakeLength * sizeof(ushort));

    [SetUp]
    public void SetUp()
    {
        // Alloca memoria non gestita per un'istanza di BattleSnake.
        _memory = System.Runtime.InteropServices.Marshal.AllocHGlobal(MemorySize);
        _snake = (BattleSnake*)_memory;

        // Azzera la memoria per garantire uno stato pulito prima di ogni test.
        new Span<byte>(_snake, MemorySize).Clear();

        // Pre-calcola il puntatore al corpo, simulando l'uso reale da parte di Battlefield.
        _bodyPtr = (ushort*)((byte*)_snake + BattleSnake.HeaderSize);

        // Imposta uno stato di partenza standard per la maggior parte dei test.
        _snake->Reset(MaxSnakeLength);
    }

    [TearDown]
    public void TearDown()
    {
        if (_memory == IntPtr.Zero) return;
        System.Runtime.InteropServices.Marshal.FreeHGlobal(_memory);
        _memory = IntPtr.Zero;
        _snake = null;
    }

    #region Reset Logic

    [Test]
    public void Reset_ShouldInitializeStateCorrectly()
    {
        // Arrange: Modifica lo stato per assicurarsi che Reset lo sovrascriva.
        _snake->Health = 50;
        _snake->Length = 10;
        _snake->Head = 999;

        // Act
        _snake->Reset(MaxSnakeLength);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_snake->Health, Is.EqualTo(100), "Health should be reset to 100");
            Assert.That(_snake->Length, Is.EqualTo(3), "Length should be reset to 3");
            Assert.That(_snake->MaxLength, Is.EqualTo(MaxSnakeLength), "MaxLength should be set");
            Assert.That(_snake->Head, Is.EqualTo(0), "Head position should be reset to 0");
        });
    }

    #endregion

    #region Health Management

    [Test]
    public void Move_OnEmptyCell_DecreasesHealthByOne()
    {
        var initialHealth = _snake->Health;

        var survived = _snake->Move(_bodyPtr, 10, CellContent.Empty);

        Assert.That(survived, Is.True);
        Assert.That(_snake->Health, Is.EqualTo(initialHealth - 1), "Health should decrease by 1 on an empty cell");
    }

    [Test]
    public void Move_OnFoodCell_RestoresHealthTo100()
    {
        _snake->Health = 25; // Arrange: start with low health.

        var survived = _snake->Move(_bodyPtr, 10, CellContent.Food);

        Assert.That(survived, Is.True);
        Assert.That(_snake->Health, Is.EqualTo(100), "Health should be restored to 100 after eating");
    }

    [Test]
    public void Move_OnHazardCell_AppliesDefaultDamage()
    {
        var initialHealth = _snake->Health;
        const int defaultHazardDamage = 15;

        var survived = _snake->Move(_bodyPtr, 10, CellContent.Hazard);

        Assert.That(survived, Is.True);
        Assert.That(_snake->Health, Is.EqualTo(initialHealth - defaultHazardDamage), "Default hazard damage should be 15");
    }
    
    [Test]
    public void Move_OnEnemySnake_IsFatal()
    {
        var survived = _snake->Move(_bodyPtr, 10, CellContent.EnemySnake);

        Assert.That(survived, Is.False, "Collision with an enemy snake should be fatal");
        Assert.That(_snake->Health, Is.EqualTo(0), "Health should be 0 after a fatal collision");
    }

    #endregion

    #region Death Conditions

    [Test]
    public void Move_WhenHealthReachesZero_IsFatal()
    {
        _snake->Health = 1; // Arrange: Health will become 0 after this move.

        var survived = _snake->Move(_bodyPtr, 10, CellContent.Empty);

        Assert.That(survived, Is.False, "Snake should die when health drops to 0");
        Assert.That(_snake->Health, Is.EqualTo(0));
    }

    [Test]
    public void Move_WhenHealthBecomesNegative_IsFatal()
    {
        _snake->Health = 5; // Arrange: Health will become negative from hazard damage.

        var survived = _snake->Move(_bodyPtr, 10, CellContent.Hazard, 10);

        Assert.That(survived, Is.False, "Snake should die when health drops below 0");
        Assert.That(_snake->Health, Is.LessThan(0));
    }

    [Test]
    public void Move_IfFatal_DoesNotChangeState()
    {
        // Arrange
        _snake->Length = 3;
        _snake->Head = 100;
        _snake->Body[0] = 200;
        _snake->Body[1] = 201;
        _snake->Body[2] = 202;
        
        var originalHead = _snake->Head;
        var originalLength = _snake->Length;

        // Act: Collision is fatal and should cause an early exit in the Move method.
        _snake->Move(_bodyPtr, 150, CellContent.EnemySnake);

        // Assert: Snake's state should be unmodified because the method exits before updates.
        Assert.Multiple(() =>
        {
            Assert.That(_snake->Head, Is.EqualTo(originalHead), "Head position should not change on fatal move");
            Assert.That(_snake->Length, Is.EqualTo(originalLength), "Length should not change on fatal move");
            Assert.That(_snake->Body[0], Is.EqualTo(200), "Body content should not change on fatal move");
            Assert.That(_snake->Body[1], Is.EqualTo(201));
            Assert.That(_snake->Body[2], Is.EqualTo(202));
        });
    }

    #endregion

    #region Body Management: Growth

    [Test]
    public void Move_OnFood_IncreasesLengthByOne()
    {
        var initialLength = _snake->Length;

        _snake->Move(_bodyPtr, 10, CellContent.Food);

        Assert.That(_snake->Length, Is.EqualTo(initialLength + 1), "Length should increase by 1 after eating");
    }

    [Test]
    public void Move_OnFood_AppendsOldHeadToBody()
    {
        // Arrange
        _snake->Length = 3;
        _snake->Head = 75;
        _snake->Body[0] = 100;
        _snake->Body[1] = 101;
        _snake->Body[2] = 102;

        // Act
        _snake->Move(_bodyPtr, 85, CellContent.Food);

        // Assert
        Assert.That(_snake->Length, Is.EqualTo(4));
        Assert.That(_snake->Body[3], Is.EqualTo(75), "The new body segment should be the old head's position");
        Assert.That(_snake->Head, Is.EqualTo(85), "Head should be in the new position");
    }
    
    [Test]
    public void Move_OnFood_WhenAtMaxLength_DoesNotGrowButShifts()
    {
        // Arrange
        _snake->Reset(5); // Set MaxLength to 5.
        _snake->Length = 5; // Start at max length.
        _snake->Head = 50;
        _snake->Body[3] = 4; // A known value for the "neck" segment.
        
        var originalLength = _snake->Length;

        // Act
        _snake->Move(_bodyPtr, 60, CellContent.Food);

        // Assert
        Assert.That(_snake->Length, Is.EqualTo(originalLength), "Length should not exceed MaxLength");
        Assert.That(_snake->Health, Is.EqualTo(100), "Health should still be restored to 100");
        Assert.That(_snake->Body[4], Is.EqualTo(50), "Body should shift, with old head at the end");
    }

    #endregion

    #region Body Management: Movement

    [Test]
    public void Move_OnEmptyCell_ShiftsBody()
    {
        // Arrange: Setup a snake with a known body and head position.
        _snake->Length = 3;
        _snake->Head = 100;
        _snake->Body[0] = 200; // Tail
        _snake->Body[1] = 201; // Middle
        _snake->Body[2] = 202; // Neck

        // Act: Move to a new position.
        _snake->Move(_bodyPtr, 150, CellContent.Empty);

        // Assert: The body should shift forward, and the old head becomes the new neck.
        // Expected: [200, 201, 202] -> [201, 202, 100]
        Assert.Multiple(() =>
        {
            Assert.That(_snake->Body[0], Is.EqualTo(201));
            Assert.That(_snake->Body[1], Is.EqualTo(202));
            Assert.That(_snake->Body[2], Is.EqualTo(100));
            Assert.That(_snake->Head, Is.EqualTo(150));
            Assert.That(_snake->Length, Is.EqualTo(3), "Length should not change when just moving");
        });
    }

    [Test]
    public void Move_WithLengthOfOne_ReplacesBodyWithOldHead()
    {
        // Arrange
        _snake->Length = 1;
        _snake->Head = 50;

        // Act
        _snake->Move(_bodyPtr, 60, CellContent.Empty);

        // Assert
        Assert.That(_snake->Length, Is.EqualTo(1));
        Assert.That(_snake->Body[0], Is.EqualTo(50), "The single body segment should become the old head's position");
        Assert.That(_snake->Head, Is.EqualTo(60));
    }
    
    #endregion
}