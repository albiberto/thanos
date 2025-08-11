using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Thanos.Enums;

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
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, Converters = [typeof(JsonStringEnumConverter)])]
[JsonSerializable(typeof(Request))]
public partial class ThanosSerializerContext : JsonSerializerContext;

[method: JsonConstructor]
public readonly struct Request(Game game, int turn, Board board, Snake you)
{
    [JsonPropertyName("game")] public readonly Game Game = game;
    [JsonPropertyName("turn")] public readonly int Turn = turn;
    [JsonPropertyName("board")] public readonly Board Board = board;
    [JsonPropertyName("you")] public readonly Snake You = you;
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

[method: JsonConstructor]
public readonly struct Game(Guid id, Ruleset ruleset, Map map, Source source, int timeout)
{
    [JsonPropertyName("id")] public readonly Guid Id = id;
    [JsonPropertyName("ruleset")] public readonly Ruleset Ruleset = ruleset;
    [JsonPropertyName("map")] public readonly Map Map = map;
    [JsonPropertyName("source")] public readonly Source Source = source;
    [JsonPropertyName("timeout")] public readonly int Timeout = timeout;
}

[method: JsonConstructor]
public readonly struct Ruleset(RulesetSettings settings)
{
    [JsonPropertyName("settings")] public readonly RulesetSettings Settings = settings;
}

[method: JsonConstructor]
public readonly struct RulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, Royale? royale, Squad? squad)
{
    [JsonPropertyName("foodSpawnChance")] public readonly int FoodSpawnChance = foodSpawnChance;
    [JsonPropertyName("minimumFood")] public readonly int MinimumFood = minimumFood;
    [JsonPropertyName("hazardDamagePerTurn")] public readonly int HazardDamagePerTurn = hazardDamagePerTurn;
    [JsonPropertyName("royale")] public readonly Royale? Royale = royale;
    [JsonPropertyName("squad")] public readonly Squad? Squad = squad;
}

[method: JsonConstructor]
public readonly struct Royale(int shrinkEveryNTurns)
{
    [JsonPropertyName("shrinkEveryNTurns")] public readonly int ShrinkEveryNTurns = shrinkEveryNTurns;
}

[method: JsonConstructor]
public readonly struct Squad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
{
    [JsonPropertyName("allowBodyCollisions")] public readonly bool AllowBodyCollisions = allowBodyCollisions;
    [JsonPropertyName("sharedElimination")] public readonly bool SharedElimination = sharedElimination;
    [JsonPropertyName("sharedHealth")] public readonly bool SharedHealth = sharedHealth;
    [JsonPropertyName("sharedLength")] public readonly bool SharedLength = sharedLength;
}

[method: JsonConstructor]
public readonly struct Board(uint height, uint width, Coordinate[] food, Coordinate[] hazards, Snake[] snakes)
{
    [JsonPropertyName("height")] public readonly uint Height = height;
    [JsonPropertyName("width")] public readonly uint Width = width;
    [JsonPropertyName("food")] public readonly Coordinate[] Food = food;
    [JsonPropertyName("hazards")] public readonly Coordinate[] Hazards = hazards;
    [JsonPropertyName("snakes")] public readonly Snake[] Snakes = snakes;

    [JsonIgnore] public readonly uint Area = height * width;
    [JsonIgnore] public readonly int Capacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2(height * width), Constants.MaxBodyLength);
}

[method: JsonConstructor]
public readonly struct Coordinate(int x, int y)
{
    [JsonPropertyName("x")] public readonly int X = x;
    [JsonPropertyName("y")] public readonly int Y = y;
}

[method: JsonConstructor]
public readonly struct Snake(string id, int health, Coordinate[] body, Coordinate head, int length)
{
    [JsonPropertyName("id")] public readonly string Id = id;
    [JsonPropertyName("health")] public readonly int Health = health;
    [JsonPropertyName("body")] public readonly Coordinate[] Body = body;
    [JsonPropertyName("head")] public readonly Coordinate Head = head;
    [JsonPropertyName("length")] public readonly int Length = length;
}

[method: JsonConstructor]
public readonly struct Customizations(string color, string head, string tail)
{
    [JsonPropertyName("color")] public readonly string Color = color;
    [JsonPropertyName("head")] public readonly string Head = head;
    [JsonPropertyName("tail")] public readonly string Tail = tail;
}