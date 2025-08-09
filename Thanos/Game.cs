using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Rappresenta le informazioni generali della partita.
/// Convertito in uno struct immutabile.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = Constants.CacheLineSize)]
public readonly unsafe struct Game(Guid id, Ruleset ruleset, Map map, Source source, int timeout)
{
    private const int Size = 16 /*GUID*/ + Ruleset.Size + sizeof(Map) + sizeof(Source) + sizeof(uint);

    public Guid Id { get; } = id;
    public Ruleset Ruleset { get; } = ruleset;
    public Map Map { get; } = map;
    public Source Source { get; } = source;
    public int Timeout { get; } = timeout;
}

public readonly struct Ruleset(RulesetSettings settings)
{
    public const int Size = RulesetSettings.Size;
    
    public RulesetSettings Settings { get; } = settings;
}


/// <summary>
/// Contiene le impostazioni specifiche delle regole.
/// </summary>
public readonly struct RulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, Royale royale, Squad squad)
{
    public const int Size = sizeof(uint) * 3 + Royale.Size + Squad.Size;
    
    public int FoodSpawnChance { get; } = foodSpawnChance;
    public int MinimumFood { get; } = minimumFood;
    public int HazardDamagePerTurn { get; } = hazardDamagePerTurn;

    public Royale Royale { get; } = royale;
    public Squad Squad { get; } = squad;
}

/// <summary>
/// Impostazioni per la modalità Royale (restringimento della mappa).
/// </summary>
public readonly struct Royale(int shrinkEveryNTurns)
{
    public const int Size = sizeof(int);
    
    public int ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

/// <summary>
/// Impostazioni per la modalità Squad (squadre).
/// </summary>
public readonly struct Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    public const int Size = sizeof(bool) * 4;
    
    public bool AllowBodyCollisions { get; } = allowBodyCollisions;
    public bool SharedElimination { get; } = sharedElimination;
    public bool SharedHealth { get; } = sharedHealth;
    public bool SharedLength { get; } = sharedLength;
}

public enum Map : byte
{
    Standard = 0,
    Royale = 1,
    Constrictor = 2,
    SnailMode = 3,
    Unknown = 255
}

public enum Source : byte
{
    Tournament,
    League,
    Arena,
    Challenge,
    Custom,
    Unknown = 255
}