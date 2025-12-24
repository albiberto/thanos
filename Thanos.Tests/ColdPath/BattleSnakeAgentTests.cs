using Moq;
using Thanos.Abstract;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.ColdPath;

[TestFixture]
public class BattleSnakeAgentTests
{
    private MockRepository _repository;
    private Mock<IBattleSnakeCluster> _mockCluster;
    private BattleSnakeAgent _agent;

    [SetUp]
    public void Setup()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _mockCluster = _repository.Create<IBattleSnakeCluster>();

        // Setup GLOBALE per Dispose (necessario perché viene chiamato nel TearDown)
        _mockCluster.Setup(c => c.Dispose());

        _agent = new BattleSnakeAgent(_mockCluster.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _agent.Dispose(); 
        _repository.VerifyAll(); 
    }

    [Test]
    public void Constructor_WhenClusterIsNull_ThenThrowsArgumentNullException()
    {
        Throws<ArgumentNullException>(() => _ = new BattleSnakeAgent(null!));
    }

    [Test]
    public void Start_WhenRequestIsReceived_ThenMapsSnakeIdsSortingHeroFirst()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        
        // CORRETTO: Qui configuriamo SOLO InitializeGame.
        // Se per errore qui c'era .Setup(c => c.ComputeMoveAsync...), RIMUOVIDO!
        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(ids => 
                    ids.Length == 4 &&
                    ids[0] == Support.Me &&
                    ids[1] == Support.Enemy1 &&
                    ids[2] == Support.Enemy2 &&
                    ids[3] == Support.Enemy3
                )))
            .Verifiable();

        // Act
        _agent.Start(request);

        // Assert
        _mockCluster.Verify();
    }

    [Test]
    public async Task Move_WhenCalled_ThenDelegatesToClusterAndReturnsResult()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        const byte expectedMove = 2; // Down

        // CORRETTO: Qui (e SOLO QUI) configuriamo ComputeMoveAsync
        _mockCluster
            .Setup(c => c.ComputeMoveAsync(It.IsAny<Request>())) 
            .ReturnsAsync(expectedMove)
            .Verifiable();

        // Act
        var result = await _agent.Move(request);

        // Assert
        That(result, Is.EqualTo(expectedMove));
    }

    [Test]
    public void End_WhenCalled_ThenDoesNotThrow()
    {
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        
        // Nessun Setup necessario qui (a parte Dispose che è globale)
        DoesNotThrow(() => _agent.End(request));
    }

    [Test]
    public void Dispose_WhenCalled_ThenDisposesUnderlyingCluster()
    {
        // Act
        _agent.Dispose();

        // Assert
        _mockCluster.Verify(c => c.Dispose(), Times.AtLeastOnce());
    }
}