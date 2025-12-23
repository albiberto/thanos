using Moq;
using Thanos.Abstract;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.ColdPath;

[TestFixture]
[Parallelizable(ParallelScope.All)]
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
        // Setup di base per Dispose, chiamato automaticamente dal TearDown o explicit
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
        Throws<ArgumentNullException>(() => new BattleSnakeAgent(null!));
    }

    [Test]
    public void Start_WhenRequestIsReceived_ThenMapsSnakeIdsSortingHeroFirst()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        // ID attesi dal file MediumRequest.json:
        // Hero: "snake-hero"
        // Enemy1: "snake-enemy"
        // Enemy2: "snake-enemy-2"
        // Enemy3: "snake-enemy-3"

        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(ids => 
                    ids.Length == 4 &&
                    ids[0] == Support.Me &&       // Hero SEMPRE primo
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

        _mockCluster
            .Setup(c => c.ComputeMoveAsync(request))
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
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.MediumJson);
        
        // Act & Assert
        DoesNotThrow(() => _agent.End(request));
    }

    [Test]
    public void Dispose_WhenCalled_ThenDisposesUnderlyingCluster()
    {
        // Arrange (Setup già fatto, ma esplicitiamo la verifica)
        _mockCluster.Setup(c => c.Dispose()).Verifiable();

        // Act
        _agent.Dispose();

        // Assert
        _mockCluster.Verify(c => c.Dispose(), Times.AtLeastOnce());
    }
}