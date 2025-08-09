using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Thanos.Tests.Support.Model;

// Questa classe ora è corretta, non servono modifiche.
[method: JsonConstructor]
public class TestRequest(TestGame game, int turn, TestBoard board, TestSnake you)
{
    [JsonPropertyName("game")]
    public TestGame Game { get; } = game;
    
    [JsonPropertyName("turn")]
    public int Turn { get; } = turn;
    
    [JsonPropertyName("board")]
    public TestBoard Board { get; } = board;
    
    [JsonPropertyName("you")]
    public TestSnake You { get; } = you;
}

[method: JsonConstructor]
public class TestGame(Guid id, TestRuleset ruleset, string map, string source, int timeout)
{
    [JsonPropertyName("id")]
    public Guid Id { get; } = id;
    
    [JsonPropertyName("ruleset")]
    public TestRuleset Ruleset { get; } = ruleset;

    [JsonPropertyName("map")]
    public string Map { get; } = map;

    [JsonPropertyName("source")]
    public string Source { get; } = source;

    [JsonPropertyName("timeout")]
    public int Timeout { get; } = timeout;
}

[method: JsonConstructor]
public class TestRuleset(string name, string version, TestRulesetSettings settings)
{
    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("version")]
    public string Version { get; } = version;

    [JsonPropertyName("settings")]
    public TestRulesetSettings Settings { get; } = settings;
}

[method: JsonConstructor]
public class TestRulesetSettings(int foodSpawnChance, int minimumFood, int hazardDamagePerTurn, TestRoyale? royale, TestSquad? squad)
{
    [JsonPropertyName("foodSpawnChance")]
    public int FoodSpawnChance { get; } = foodSpawnChance;

    [JsonPropertyName("minimumFood")]
    public int MinimumFood { get; } = minimumFood;

    [JsonPropertyName("hazardDamagePerTurn")]
    public int HazardDamagePerTurn { get; } = hazardDamagePerTurn;

    [JsonPropertyName("royale")]
    public TestRoyale? Royale { get; } = royale;

    [JsonPropertyName("squad")]
    public TestSquad? Squad { get; } = squad;
}

[method: JsonConstructor]
public class TestRoyale(uint shrinkEveryNTurns)
{
    [JsonPropertyName("shrinkEveryNTurns")]
    public uint ShrinkEveryNTurns { get; } = shrinkEveryNTurns;
}

[method: JsonConstructor]
public class TestSquad(bool allowBodyCollisions, bool sharedElimination, bool sharedHealth, bool sharedLength)
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

[method: JsonConstructor]
public class TestCoordinate(uint x, uint y)
{
    [JsonPropertyName("x")]
    public uint X { get; } = x;

    [JsonPropertyName("y")]
    public uint Y { get; } = y;
}

[method: JsonConstructor]
public class TestBoard(uint height, uint width, TestCoordinate[] food, TestCoordinate[] hazards, TestSnake[] snakes)
{
    [JsonPropertyName("height")]
    public uint Height { get; } = height;

    [JsonPropertyName("width")]
    public uint Width { get; } = width;

    [JsonPropertyName("food")]
    public TestCoordinate[] Food { get; } = food;

    [JsonPropertyName("hazards")]
    public TestCoordinate[] Hazards { get; } = hazards;

    [JsonPropertyName("snakes")]
    public TestSnake[] Snakes { get; } = snakes;
}

[method: JsonConstructor]
public class TestSnake(string id, string name, uint health, TestCoordinate[] body, string latency, TestCoordinate head, uint length, string shout, TestCustomizations customizations)
{
    [JsonPropertyName("id")]
    public string Id { get; } = id;

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("health")]
    public uint Health { get; } = health;

    [JsonPropertyName("body")]
    public TestCoordinate[] Body { get; } = body;

    [JsonPropertyName("latency")]
    public string Latency { get; } = latency;

    [JsonPropertyName("head")]
    public TestCoordinate Head { get; } = head;

    [JsonPropertyName("length")]
    public uint Length { get; } = length;

    [JsonPropertyName("shout")]
    public string Shout { get; } = shout;

    [JsonPropertyName("customizations")]
    public TestCustomizations Customizations { get; } = customizations;
}

[method: JsonConstructor]
public class TestCustomizations(string color, string head, string tail)
{
    [JsonPropertyName("color")]
    public string Color { get; } = color;
    
    [JsonPropertyName("head")]
    public string Head { get; } = head;
    
    [JsonPropertyName("tail")]
    public string Tail { get; } = tail;
}