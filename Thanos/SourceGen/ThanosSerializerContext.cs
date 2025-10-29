using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
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
}

[method: JsonConstructor]
public readonly struct Ruleset(RulesetSettings settings)
{
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
public readonly struct Board(int height, int width, ushort[] food, ushort[] hazards, Snake[] snakes)
{
    [JsonPropertyName("height")] public int Height { get; } = height;
    [JsonPropertyName("width")] public int Width { get; } = width;

    [JsonPropertyName("food")] public ushort[] Food { get; } = food;
    [JsonPropertyName("hazards")] public ushort[] Hazards { get; } = hazards;
    [JsonPropertyName("snakes")] public Snake[] Snakes { get; } = snakes;

    [JsonIgnore] public int Area => Width * Height;
}

[method: JsonConstructor]
public readonly struct You(string id)
{
    [JsonPropertyName("id")] public string Id { get; } = id;
}

[method: JsonConstructor]
public readonly struct Snake(string id, byte health, ushort[] body)
{
    [JsonPropertyName("id")] public string Id { get; } = id;
    [JsonPropertyName("health")] public byte Health { get; } = health;
    [JsonPropertyName("body")] public ushort[] Body { get; } = body;
}

[method: JsonConstructor]
public readonly struct Coordinate(byte x, byte y)
{
    [JsonPropertyName("x")] public byte X { get; } = x;
    [JsonPropertyName("y")] public byte Y { get; } = y;
}