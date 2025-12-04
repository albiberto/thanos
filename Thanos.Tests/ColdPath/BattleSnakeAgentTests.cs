using Moq;
using Thanos.Abstract;
using Thanos.SourceGen;

namespace Thanos.Tests.ColdPath;

[TestFixture]
public class BattleSnakeAgentTests
{
    private MockRepository _repository;
    private Mock<IBattleSnakeCluster> _mockCluster;
    private BattleSnakeAgent _agent;

    private const string SampleJson = """
    {
      "game": { "id": "game-id", "ruleset": { "name": "standard", "settings": {} }, "map": "standard" },
      "turn": 1,
      "board": {
        "height": 11, "width": 11,
        "food": [{"x": 5, "y": 5}],
        "hazards": [],
        "snakes": [
          { "id": "hero-id", "health": 100, "body": [{"x": 0, "y": 0}], "head": {"x":0,"y":0}, "length": 1 },
          { "id": "enemy-id", "health": 100, "body": [{"x": 1, "y": 1}], "head": {"x":1,"y":1}, "length": 1 }
        ]
      },
      "you": { "id": "hero-id", "health": 100, "body": [{"x": 0, "y": 0}] }
    }
    """;

    [SetUp]
    public void Setup()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _mockCluster = _repository.Create<IBattleSnakeCluster>();
        _agent = new BattleSnakeAgent(_mockCluster.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _agent.Dispose();
        _repository.VerifyAll();
    }

    [Test]
    public void Start_Should_ParseJson_And_PopulateBufferCorrectly()
    {
        // Arrange
        var request = BattleSnakeSerializer.Parse(SampleJson);

        const string myId = "hero-id";
        const string enemyId = "enemy-id";

        // Exceptations
        _mockCluster
            .Setup(c => c.InitializeGame(
                It.Is<string[]>(buffer => 
                    buffer[0] == myId && 
                    buffer[1] == enemyId
                ),
                2))
            .Verifiable();

        _mockCluster.Setup(c => c.Reset()).Verifiable();

        // ACT
        _agent.Start(request);
    }
}