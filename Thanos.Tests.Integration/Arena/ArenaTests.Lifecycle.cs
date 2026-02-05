using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Arena;

public partial class ArenaTests
{
    [Test]
    public void InitializeFromRequest_WhenValidRequest_ShouldMapAllComponentsCorrectly()
    {
        // ARRANGE
        using var ctx = new ArenaTestContext(_lookups, _layout);
        var arena = ctx.Arena;

        // Mocking a Request using the serializer to ensure real-world data format
        var request = BattleSnakeSerializer.Parse(Support.Constants.MediumJson);
        string[] orderedIds = [Support.Constants.Me, Support.Constants.Enemy1, Support.Constants.Enemy2, Support.Constants.Enemy3];

        // ACT
        arena.InitializeFromRequest(in request, orderedIds);

        // ASSERT
        // 1. SnakesSystem Integrity
        That(arena.System.Count, Is.EqualTo(4), "Arena must manage exactly 4 snakes based on orderedIds.");
        That(arena.System[0].IsDead, Is.False, "Hero should be alive.");
        That(arena.System[0].Hp, Is.EqualTo(90), "Hero HP mismatch.");
        That(arena.System[0].Length, Is.EqualTo(3), "Hero length mismatch.");

        // 2. Global Bitboard Synchronization
        // The Arena.Snakes bitboard must be the union of all individual snake bodies
        var expectedPopCount = 0;
        for (var i = 0; i < 4; i++) expectedPopCount += arena.System[i].Length;

        That(arena.Snakes.PopCount(), Is.EqualTo(expectedPopCount),
            "Global Snakes bitboard must match the sum of individual snake lengths.");

        // 3. Environment Bitboards
        That(arena.Food.PopCount(), Is.EqualTo(4), "MediumRequest.json contains 4 food items.");
        That(arena.Hazards.PopCount(), Is.EqualTo(11), "MediumRequest.json contains 11 hazard items.");

        // 4. Spatial Verification (Sample Check)
        // Food at {5,5} -> Index 60 in 11x11
        That(arena.Food.IsSet(60), Is.True, "Food at {5,5} (Index 60) was not set.");
    }

    [Test]
    public void CloneFrom_WhenInvoked_ShouldPerformDeepMemoryCopy()
    {
        // ARRANGE
        using var sourceCtx = new ArenaTestContext(_lookups, _layout);
        using var destCtx = new ArenaTestContext(_lookups, _layout);

        var source = sourceCtx.Arena;
        var destination = destCtx.Arena;

        var request = BattleSnakeSerializer.Parse(Support.Constants.MediumJson);
        string[] orderedIds = [Support.Constants.Me, Support.Constants.Enemy1, Support.Constants.Enemy2, Support.Constants.Enemy3];
        source.InitializeFromRequest(in request, orderedIds);

        // ACT
        destination.CloneFrom(in source);

        // ASSERT
        // 1. Vital Signs Equality
        That(destination.System[0].Hp, Is.EqualTo(source.System[0].Hp));
        That(destination.Snakes.PopCount(), Is.EqualTo(source.Snakes.PopCount()));
        That(destination.Food.PopCount(), Is.EqualTo(source.Food.PopCount()));

        // 2. Isolation (The Paranoia Check)
        // Modifying the source MUST NOT affect the destination
        source.Food.Clear();
        That(destination.Food.PopCount(), Is.EqualTo(4), "Clone must be a deep copy, not a reference copy.");

        source.System[0].Kill();
        That(destination.System[0].IsDead, Is.False, "Snake life state must be isolated after clone.");
    }

    [Test]
    public void InitializeFromRequest_WhenSnakeIdIsMissingInOrderedIds_ShouldSkipThatSnake()
    {
        // SCENARIO: The request has 4 snakes, but our engine only cares about 2.
        // ARRANGE
        using var ctx = new ArenaTestContext(_lookups, _layout);
        var arena = ctx.Arena;
        var request = BattleSnakeSerializer.Parse(Support.Constants.MediumJson);

        // We only provide 2 IDs
        string[] orderedIds = [Support.Constants.Me, Support.Constants.Enemy1];

        // ACT
        arena.InitializeFromRequest(in request, orderedIds);

        // ASSERT
        That(arena.System[0].IsDead, Is.False, "Hero (0) should be initialized.");
        That(arena.System[1].IsDead, Is.False, "Enemy1 (1) should be initialized.");

        // SnakesSystem handles cleanup in Initialize(), so 2 and 3 must be dead
        That(arena.System[2].IsDead, Is.True, "Snake 2 was not in orderedIds and should remain dead.");
        That(arena.System[3].IsDead, Is.True, "Snake 3 was not in orderedIds and should remain dead.");
    }
}