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
    public void Start_Should_Map_RealJsonRequest_Correctly_To_Cluster()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        // Expectations
        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(buffer => 
                    buffer[0] == Support.Me && 
                    buffer[1] == Support.Enemy1 && 
                    buffer[2] == Support.Enemy2 &&
                    buffer[3] == Support.Enemy3
                ),
                4))
            .Verifiable();

        _mockCluster
            .Setup(c => c.Reset())
            .Verifiable();

        // Act
        _agent.Start(request);
    }

    [Test]
    public async Task Move_Should_DelegateToCluster_And_Return_ComputedByte()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);
        const byte expectedMove = 2;

        // Expectations
        _mockCluster
            .Setup(c => c.ComputeMoveAsync(request))
            .ReturnsAsync(expectedMove)
            .Verifiable();

        // Act
        var result = await _agent.Move(request);

        // Assert
        That(result, Is.EqualTo(expectedMove));
    }
}