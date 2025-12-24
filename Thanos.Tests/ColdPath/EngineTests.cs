using Moq;
using Thanos.Abstract;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.ColdPath;

[TestFixture]
[Parallelizable(ParallelScope.None)] 
public class EngineTests
{
    private MockRepository _repository;
    private Mock<IWorker> _mockWorker;
    
    private NodeMemoryPool _nodePool;
    private SlotMemoryPool _slotPool;
    private LookupsMemoryPool _lookups;
    
    private Engine _engine;

    private const int MaxNodes = 1000;
    private const int Index = 0;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _lookups = LookupsMemoryPool.Medium;
    }

    [SetUp]
    public void Setup()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _mockWorker = _repository.Create<IWorker>();

        var nodeLayout = new NodeMemoryLayout();
        var slotLayout = new SlotMemoryLayout(Constants.Medium.Area, 64, Constants.MaxSnakesCount);

        _nodePool = new NodeMemoryPool(MaxNodes, 1, nodeLayout);
        _slotPool = new SlotMemoryPool(MaxNodes, 1, Constants.MaxSnakesCount, _lookups, slotLayout);

        _engine = new Engine(_slotPool, _nodePool, _mockWorker.Object, Index);
    }

    [TearDown]
    public void TearDown()
    {
        _nodePool.Dispose();
        _slotPool.Dispose();
        _repository.VerifyAll(); // Verifica chiamate Strict
    }

    [Test]
    public void FindBestMove_WhenCalledFirstTime_ThenAllocatesNewRootAndRunsIterations()
    {
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        const long targetHash = 12345;
        
        _mockWorker
            .Setup(w => w.RunIteration(It.IsAny<int>(), It.IsAny<int>()))
            .Verifiable();

        _engine.FindBestMove(request, -1, targetHash);

        Multiple(() =>
        {
            That(_nodePool.Index, Is.GreaterThan(1), "Should allocate root node.");
            _mockWorker.Verify(w => w.RunIteration(Constants.Medium.Area, It.IsAny<int>()), Times.AtLeastOnce);
        });
    }

    [Test]
    public void FindBestMove_WhenHashMatchesChild_ThenReusesTree()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        const long rootHash = 100;
        const long nextTurnHash = 200;

        _mockWorker.Setup(w => w.RunIteration(It.IsAny<int>(), It.IsAny<int>()));

        // --- TURNO 1: Inizializzazione ---
        _engine.FindBestMove(request, -1, rootHash);
        
        var rootIndex = 1; 
        var chosenChildIndex = _nodePool.Allocate(); // Index 2
        var targetNodeIndex = _nodePool.Allocate();  // Index 3

        // Colleghiamo Root -> ChosenChild
        ref var root = ref _nodePool.Get(rootIndex);
        root.FirstChildIndex = chosenChildIndex;

        // Colleghiamo ChosenChild -> TargetNode
        ref var chosen = ref _nodePool.Get(chosenChildIndex);
        chosen.ParentIndex = rootIndex;
        chosen.FirstChildIndex = targetNodeIndex;
        chosen.NextSiblingIndex = -1;

        // Configuriamo TargetNode per matchare l'hash del Turno 2
        ref var target = ref _nodePool.Get(targetNodeIndex);
        target.Hash = nextTurnHash;
        target.ParentIndex = chosenChildIndex;
        target.NextSiblingIndex = -1;
        target.FirstChildIndex = -1; // <--- FIX QUI

        var indexBeforeTurn2 = _nodePool.Index;

        // --- TURNO 2: Tree Reuse ---
        _engine.FindBestMove(request, chosenChildIndex, nextTurnHash);
        
        // Assert
        That(_nodePool.Index, Is.GreaterThanOrEqualTo(indexBeforeTurn2), 
            "Tree Reuse failed: Pool index was reset instead of preserved.");
    }

    [Test]
    public void FindBestMove_WhenPoolsAreExhausted_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var tinyNodePool = new NodeMemoryPool(2, 1, new NodeMemoryLayout());
        var tinySlotPool = new SlotMemoryPool(2, 1, Constants.MaxSnakesCount, _lookups, new SlotMemoryLayout(121, 64, 4));
        
        // FIX: Rimosso 'var engine = ...' inutilizzato.
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        
        // Usiamo un pool con capacità 0 per garantire il fallimento immediato
        var microPool = new NodeMemoryPool(0, 0, new NodeMemoryLayout()); 
        
        // Creiamo l'engine con il pool difettoso
        var microEngine = new Engine(tinySlotPool, microPool, _mockWorker.Object, 0);

        // Act & Assert
        Throws<InvalidOperationException>(() => microEngine.FindBestMove(request, -1, 123));
        
        // Cleanup manuale dei pool locali
        tinyNodePool.Dispose();
        tinySlotPool.Dispose();
        microPool.Dispose();
    }
    
    [Test]
    public void GetFallbackMove_WhenCalled_ThenReturnsSafeMoveFromLegalMoves()
    {
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        _mockWorker.Setup(w => w.RunIteration(It.IsAny<int>(), It.IsAny<int>()));
        
        // Facciamo girare un turno per popolare l'Arena interna dell'Engine
        _engine.FindBestMove(request, -1, 123);
        
        var move = _engine.GetFallbackMove();
        
        // Deve ritornare una mossa valida (Up/Down/Left/Right) e non 0 (None)
        That(move, Is.Not.EqualTo(0), "Should return a valid move byte.");
    }
}