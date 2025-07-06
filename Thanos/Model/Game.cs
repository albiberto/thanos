/// <summary>
///     Game settings - nomi identici al JSON
/// </summary>
public sealed class Game
{
    public string id { get; set; } = "";
    public Ruleset ruleset { get; set; } = new();
    public string map { get; set; } = "";
    public uint timeout { get; set; }
    public string source { get; set; } = "";
}

/// <summary>
///     Ruleset configuration - zero overhead
/// </summary>
public sealed class Ruleset
{
    public string name { get; set; } = "";
    public string version { get; set; } = "";
    public RulesetSettings settings { get; set; } = new();
}

/// <summary>
///     Ruleset settings - deserializzazione diretta
/// </summary>
public sealed class RulesetSettings
{
    public uint foodSpawnChance { get; set; }
    public uint minimumFood { get; set; }
    public uint hazardDamagePerTurn { get; set; }
    public string hazardMap { get; set; } = "";
    public string hazardMapAuthor { get; set; } = "";
    public RoyaleSettings royale { get; set; } = new();
    public SquadSettings squad { get; set; } = new();
}

/// <summary>
///     Royale mode settings - performance pura
/// </summary>
public sealed class RoyaleSettings
{
    public uint shrinkEveryNTurns { get; set; }
    public uint damagePerTurn { get; set; }
}

/// <summary>
///     Squad mode settings - zero attributi
/// </summary>
public sealed class SquadSettings
{
    public bool allowBodyCollisions { get; set; }
    public bool sharedElimination { get; set; }
    public bool sharedHealth { get; set; }
    public bool sharedLength { get; set; }
}