using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

/// <summary>
///     Defines the context for the System.Text.Json Source Generator.
///     The generator will use this configuration to create ultra-optimized (de)serialization code for the specified types.
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
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, Converters = [typeof(JsonStringEnumConverter)])]
[JsonSerializable(typeof(Request))]
public partial class ThanosSerializerContext : JsonSerializerContext;

[method: JsonConstructor]
public readonly struct Request(Game game, int turn, Board board, You you)
{
    [JsonPropertyName("game")] public Game Game { get; } = game;
    [JsonPropertyName("turn")] public int Turn { get; } = turn;
    [JsonPropertyName("board")] public Board Board { get; } = board;
    [JsonPropertyName("you")] public You You { get; } = you;
}

[method: JsonConstructor]
public readonly struct Game(Ruleset ruleset, string map, string source)
{
    [JsonPropertyName("ruleset")] public Ruleset Ruleset { get; } = ruleset;
    [JsonPropertyName("map")] public string map { get; } = map;
    [JsonPropertyName("source")] public string Source { get; } = source;
    
    // [JsonPropertyName("id")] public Guid Id { get; } = id;
    // [JsonPropertyName("timeout")] public int Timeout { get; } = timeout;
}

[method: JsonConstructor]
public readonly struct Ruleset(RulesetSettings settings)
{
    [JsonPropertyName("settings")] public RulesetSettings Settings { get; } = settings;
    
    // [JsonPropertyName("name")] public string Name { get; } = name;
    // [JsonPropertyName("version")] public string Version { get; } = version;
}

[method: JsonConstructor]
public readonly struct RulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, Royale? royale, Squad? squad)
{
    [JsonPropertyName("foodSpawnChance")] public int FoodSpawnChance { get; } = foodSpawnChance;
    [JsonPropertyName("minimumFood")] public int MinimumFood { get; } = minimumFood;
    [JsonPropertyName("hazardDamagePerTurn")] public int HazardDamagePerTurn { get; } = hazardDamagePerTurn;
    [JsonPropertyName("royale")] public Royale? Royale { get; } = royale;
    [JsonPropertyName("squad")] public Squad? Squad { get; } = squad;
}

[method: JsonConstructor]
public readonly struct Royale(int shrinkEveryNTurns)
{
    [JsonPropertyName("shrinkEveryNTurns")] public int ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

[method: JsonConstructor]
public readonly struct Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    [JsonPropertyName("allowBodyCollisions")] public bool AllowBodyCollisions { get; } = allowBodyCollisions;
    [JsonPropertyName("sharedElimination")] public bool SharedElimination { get; } = sharedElimination;
    [JsonPropertyName("sharedHealth")] public bool SharedHealth { get; } = sharedHealth;
    [JsonPropertyName("sharedLength")] public bool SharedLength { get; } = sharedLength;
}

[method: JsonConstructor]
public readonly struct Board(int height, int width, ushort[] food, ushort[] hazards, Snake[] snakes)
{
    [JsonPropertyName("height")] public int Height { get; } = height;
    [JsonPropertyName("width")] public int Width { get; } = width;

    [JsonPropertyName("food")] public ushort[] Food { get; } = food;
    [JsonPropertyName("hazards")] public ushort[] Hazards { get; } = hazards;
    [JsonPropertyName("snakes")] public Snake[] Snakes { get; } = snakes;

    [JsonIgnore] public int Area => Width * Height;
    [JsonIgnore] public int SnakeCount => Snakes.Length;
}

[method: JsonConstructor]
public readonly struct You(Guid id)
{
    [JsonPropertyName("id")] public Guid Id { get; } = id;
}

[method: JsonConstructor]
public readonly struct Snake(Guid id, byte health, ushort[] body)
{
    [JsonPropertyName("id")] public Guid Id { get; } = id;
    [JsonPropertyName("health")] public byte Health { get; } = health;
    [JsonPropertyName("body")] public ushort[] Body { get; } = body;
    
    // [JsonPropertyName("name")] public string Name { get; } = name;
    // [JsonPropertyName("latency")] public string Latency { get; } = latency;
    // [JsonPropertyName("head")] public ushort Head { get; } = head;
    // [JsonPropertyName("length")] public int Length { get; } = length;
    // [JsonPropertyName("shout")] public string Shout { get; } = shout;
    // [JsonPropertyName("customizations")]
    // public Customizations Customizations { get; } = customizations;
}

[method: JsonConstructor]
public readonly struct Coordinate(int x, int y)
{
    [JsonPropertyName("x")] public int X { get; } = x;
    [JsonPropertyName("y")] public int Y { get; } = y;
}

// [method: JsonConstructor]
// public readonly struct Customizations(string color, string head, string tail)
// {
//     [JsonPropertyName("color")]
//     public string Color { get; } = color;
//
//     [JsonPropertyName("head")]
//     public string Head { get; } = head;
//
//     [JsonPropertyName("tail")]
//     public string Tail { get; } = tail;
// }

public enum GameMap : byte
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