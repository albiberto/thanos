using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

/// <summary>
///     Defines the context for the System.Text.Json Source Generator.
///     The generator will use this configuration to create ultra-optimized (de)serialization code for the specified types.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, Converters = [typeof(JsonStringEnumConverter)])]
[JsonSerializable(typeof(Request))]
public partial class ThanosSerializerContext : JsonSerializerContext;

[method: JsonConstructor]
public readonly struct Request(Game game, int turn, Board board, Snake you)
{
    [JsonPropertyName("game")] public Game Game { get; } = game;

    [JsonPropertyName("turn")] public int Turn { get; } = turn;

    [JsonPropertyName("board")] public Board Board { get; } = board;

    [JsonPropertyName("you")] public Snake You { get; } = you;
}

[method: JsonConstructor]
public readonly struct Game(Guid id, Ruleset ruleset, Map map, Source source, int timeout)
{
    [JsonPropertyName("id")] public Guid Id { get; } = id;

    [JsonPropertyName("ruleset")] public Ruleset Ruleset { get; } = ruleset;

    [JsonPropertyName("map")] public Map Map { get; } = map;

    [JsonPropertyName("source")] public Source Source { get; } = source;

    [JsonPropertyName("timeout")] public int Timeout { get; } = timeout;
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

/// <summary>
///     Represents the ruleset for the game.
/// </summary>
/// <remarks>
///     ===================================================================================
///     ### PERFORMANCE NOTE: REMOVAL OF STRINGS ###
///     The 'Name' and 'Version' properties, while present in the source JSON, have been
///     intentionally removed from this data model. In high-performance, low-latency
///     scenarios, every memory allocation matters.
///     Deserializing strings introduces the following performance costs:
///     1.  **Heap Allocation**: Every string (e.g., "standard", "v1.1.15") creates a
///     new object on the managed heap, increasing pressure on the Garbage Collector.
///     2.  **Garbage Collector (GC) Pressure**: More objects mean the GC must run more
///     frequently, causing potential micro-pauses that add to latency.
///     3.  **Indirection & Poor Data Locality**: A struct containing a string only
///     stores a pointer to the string object on the heap. Accessing the string's
///     data requires an extra memory jump (indirection), which hurts CPU
///     cache efficiency.
///     Given that the 'Name' and 'Version' values are not used by the core game logic,
///     removing them eliminates these costs for a net gain in throughput and latency.
///     ===================================================================================
/// </remarks>
[method: JsonConstructor]
public readonly struct Ruleset(RulesetSettings settings)
{
    // [JsonPropertyName("name")]
    // public string Name { get; } = name;
    //
    // [JsonPropertyName("version")]
    // public string Version { get; } = version;

    [JsonPropertyName("settings")] public RulesetSettings Settings { get; } = settings;
}

[method: JsonConstructor]
public readonly struct RulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, Royale? royale, Squad? squad)
{
    [JsonPropertyName("foodSpawnChance")] public int FoodSpawnChance { get; } = foodSpawnChance;

    [JsonPropertyName("minimumFood")] public int MinimumFood { get; } = minimumFood;

    [JsonPropertyName("hazardDamagePerTurn")]
    public int HazardDamagePerTurn { get; } = hazardDamagePerTurn;

    [JsonPropertyName("royale")] public Royale? Royale { get; } = royale;

    [JsonPropertyName("squad")] public Squad? Squad { get; } = squad;
}

[method: JsonConstructor]
public readonly struct Royale(int shrinkEveryNTurns)
{
    [JsonPropertyName("shrinkEveryNTurns")]
    public int ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

[method: JsonConstructor]
public readonly struct Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    [JsonPropertyName("allowBodyCollisions")]
    public bool AllowBodyCollisions { get; } = allowBodyCollisions;

    [JsonPropertyName("sharedElimination")]
    public bool SharedElimination { get; } = sharedElimination;

    [JsonPropertyName("sharedHealth")] public bool SharedHealth { get; } = sharedHealth;

    [JsonPropertyName("sharedLength")] public bool SharedLength { get; } = sharedLength;
}

[method: JsonConstructor]
public readonly struct Board(uint height, uint width, Coordinate[] food, Coordinate[] hazards, Snake[] snakes)
{
    [JsonPropertyName("height")] public uint Height { get; } = height;

    [JsonPropertyName("width")] public uint Width { get; } = width;

    [JsonPropertyName("food")] public Coordinate[] Food { get; } = food;

    [JsonPropertyName("hazards")] public Coordinate[] Hazards { get; } = hazards;

    [JsonPropertyName("snakes")] public Snake[] Snakes { get; } = snakes;
}

[method: JsonConstructor]
public readonly struct Coordinate(uint x, uint y)
{
    [JsonPropertyName("x")] public uint X { get; } = x;

    [JsonPropertyName("y")] public uint Y { get; } = y;
}

[method: JsonConstructor]
public readonly struct Snake(string id, string name, uint health, Coordinate[] body, string latency, Coordinate head, uint length, string shout, Customizations customizations)
{
    [JsonPropertyName("id")] public string Id { get; } = id;

    [JsonPropertyName("name")] public string Name { get; } = name;

    [JsonPropertyName("health")] public uint Health { get; } = health;

    [JsonPropertyName("body")] public Coordinate[] Body { get; } = body;

    [JsonPropertyName("latency")] public string Latency { get; } = latency;

    [JsonPropertyName("head")] public Coordinate Head { get; } = head;

    [JsonPropertyName("length")] public uint Length { get; } = length;

    [JsonPropertyName("shout")] public string Shout { get; } = shout;

    [JsonPropertyName("customizations")] public Customizations Customizations { get; } = customizations;
}

[method: JsonConstructor]
public readonly struct Customizations(string color, string head, string tail)
{
    [JsonPropertyName("color")] public string Color { get; } = color;

    [JsonPropertyName("head")] public string Head { get; } = head;

    [JsonPropertyName("tail")] public string Tail { get; } = tail;
}