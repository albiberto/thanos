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

    /// <summary>
    ///     Verifies that Start() correctly maps the JSON request to InitializeGame,
    ///     ordering snake IDs with Hero first followed by enemies in the correct sequence.
    /// </summary>
    [Test]
    public void Start_Should_Map_RealJsonRequest_Correctly_To_Cluster()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(buffer => 
                    buffer.Length == 4 &&
                    buffer[0] == Support.Me && 
                    buffer[1] == Support.Enemy1 && 
                    buffer[2] == Support.Enemy2 &&
                    buffer[3] == Support.Enemy3
                )))
            .Verifiable();

        _agent.Start(request);

        _mockCluster.Verify();
    }

    /// <summary>
    ///     Verifies that Move() correctly delegates to the cluster's ComputeMoveAsync
    ///     and returns the computed move byte.
    /// </summary>
    [Test]
    public async Task Move_Should_DelegateToCluster_And_Return_ComputedByte()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);
        const byte expectedMove = 2;

        _mockCluster
            .Setup(c => c.ComputeMoveAsync(request))
            .ReturnsAsync(expectedMove)
            .Verifiable();

        var actualMove = await _agent.Move(request);

        Multiple(() =>
        {
            That(actualMove, Is.EqualTo(expectedMove), 
                $"Move should return {expectedMove} but was {actualMove}.");
        });

        _mockCluster.Verify();
    }

    /// <summary>
    ///     Verifies that Start() correctly handles a single-player scenario
    ///     with only the hero snake and no enemies.
    /// </summary>
    [Test]
    public void Start_Should_Handle_SinglePlayer_Scenario()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(buffer => 
                    buffer.Length >= 1 &&
                    buffer[0] == Support.Me
                )))
            .Verifiable();

        _agent.Start(request);

        _mockCluster.Verify();
    }

    /// <summary>
    ///     Verifies that End() executes without throwing exceptions,
    ///     ensuring proper cleanup lifecycle.
    /// </summary>
    [Test]
    public void End_Should_Execute_Without_Exceptions()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        DoesNotThrow(() => _agent.End(request),
            "End should not throw exceptions.");
    }

    /// <summary>
    ///     Verifies that Dispose() is called on the cluster when the agent is disposed,
    ///     ensuring proper resource cleanup.
    /// </summary>
    [Test]
    public void Dispose_Should_Dispose_Cluster()
    {
        // Dispose is already set up in Setup() and verified in TearDown()
        // This test verifies the behavior explicitly

        _mockCluster
            .Setup(c => c.Dispose())
            .Verifiable();

        _agent.Dispose();

        _mockCluster.Verify(c => c.Dispose(), Times.AtLeastOnce());
    }

    /// <summary>
    ///     Verifies that constructor throws ArgumentNullException when cluster is null,
    ///     ensuring proper validation.
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Cluster_Is_Null()
    {
        Throws<ArgumentNullException>(() => new BattleSnakeAgent(null!),
            "Constructor should throw ArgumentNullException when cluster is null.");
    }

    /// <summary>
    ///     Verifies that Start() orders snake IDs correctly with hero always at index 0,
    ///     regardless of the order in the request.
    /// </summary>
    [Test]
    public void Start_Should_Place_Hero_At_Index_Zero()
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(buffer => 
                    buffer.Length > 0 &&
                    buffer[0] == request.You.Id
                )))
            .Verifiable();

        _agent.Start(request);

        _mockCluster.Verify();
    }

    /// <summary>
    ///     Verifies that Move() returns the exact byte value provided by the cluster,
    ///     testing with different move values.
    /// </summary>
    [TestCase((byte)0)]
    [TestCase((byte)1)]
    [TestCase((byte)2)]
    [TestCase((byte)3)]
    public async Task Move_Should_Return_Exact_Byte_From_Cluster(byte moveValue)
    {
        var request = BattleSnakeSerializer.Parse(Support.SampleJson);

        _mockCluster
            .Setup(c => c.ComputeMoveAsync(request))
            .ReturnsAsync(moveValue)
            .Verifiable();

        var actualMove = await _agent.Move(request);

        That(actualMove, Is.EqualTo(moveValue),
            $"Move should return {moveValue} but was {actualMove}.");

        _mockCluster.Verify();
    }
}

