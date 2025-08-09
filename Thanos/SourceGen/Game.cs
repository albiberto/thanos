using System.Text.Json.Serialization;

// Un namespace dedicato per i modelli usati dal Source Generator
namespace Thanos.SourceGen;

// NOTA: I modelli del JSON completo (es. MoveRequest) andrebbero qui
// public readonly struct MoveRequest(...) { ... }

public readonly struct Game
{
    // L'attributo [JsonPropertyName] mappa la proprietà C# (PascalCase)
    // alla chiave JSON (camelCase)
    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("ruleset")]
    public Ruleset Ruleset { get; }

    [JsonPropertyName("map")]
    public string Map { get; }

    [JsonPropertyName("source")]
    public string Source { get; }

    [JsonPropertyName("timeout")]
    public int Timeout { get; }

    // Il Source Generator è abbastanza intelligente da trovare il costruttore pubblico
    // senza bisogno dell'attributo [JsonConstructor]
    public Game(Guid id, Ruleset ruleset, string map, string source, int timeout)
    {
        Id = id;
        Ruleset = ruleset;
        Map = map;
        Source = source;
        Timeout = timeout;
    }
}

public readonly struct Ruleset(string name, string version, RulesetSettings settings)
{
    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("version")]
    public string Version { get; } = version;

    [JsonPropertyName("settings")]
    public RulesetSettings Settings { get; } = settings;
}

public readonly struct RulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, Royale? royale, Squad? squad)
{
    [JsonPropertyName("foodSpawnChance")]
    public int FoodSpawnChance { get; } = foodSpawnChance;

    [JsonPropertyName("minimumFood")]
    public int MinimumFood { get; } = minimumFood;

    [JsonPropertyName("hazardDamagePerTurn")]
    public int HazardDamagePerTurn { get; } = hazardDamagePerTurn;

    [JsonPropertyName("royale")]
    public Royale? Royale { get; } = royale;

    [JsonPropertyName("squad")]
    public Squad? Squad { get; } = squad;
}

public readonly struct Royale(int shrinkEveryNTurns)
{
    [JsonPropertyName("shrinkEveryNTurns")]
    public int ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

public readonly struct Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    [JsonPropertyName("allowBodyCollisions")]
    public bool AllowBodyCollisions { get; } = allowBodyCollisions;

    [JsonPropertyName("sharedElimination")]
    public bool SharedElimination { get; } = sharedElimination;

    [JsonPropertyName("sharedHealth")]
    public bool SharedHealth { get; } = sharedHealth;

    [JsonPropertyName("sharedLength")]
    public bool SharedLength { get; } = sharedLength;
}