using System.Text.Json.Serialization;

namespace Thanos.Tests.Support.Model;

[method: JsonConstructor]
public class Game(Guid id, Ruleset ruleset, string map, string source, uint timeout)
{
    public Guid Id { get; } = id;
    public Ruleset Ruleset { get; } = ruleset;
    public string map { get; } = map;
    public string source { get; } = source;
    public uint timeout { get; } = timeout;
}

[method: JsonConstructor]
public class Ruleset(string name, string version, RulesetSettings settings)
{
    public string Name { get; } = name;
    public string Version { get; } = version;
    public RulesetSettings Settings { get; } = settings;
}

[method: JsonConstructor]
public class RulesetSettings(uint foodSpawnChance, uint minimumFood, uint hazardDamagePerTurn, Royale royale, Squad squad)
{
    public uint foodSpawnChance { get; } = foodSpawnChance;
    public uint minimumFood { get; } = minimumFood;
    public uint hazardDamagePerTurn { get; } = hazardDamagePerTurn;
    public Royale Royale { get; } = royale;
    public Squad Squad { get; } = squad;
}

[method: JsonConstructor]
public class Royale(uint shrinkEveryNTurns)
{
    public uint ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

[method: JsonConstructor]
public class Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    public bool AllowBodyCollisions { get; } = allowBodyCollisions;
    public bool SharedElimination { get; } = sharedElimination;
    public bool SharedHealth { get; } = sharedHealth;
    public bool SharedLength { get; } = sharedLength;
}