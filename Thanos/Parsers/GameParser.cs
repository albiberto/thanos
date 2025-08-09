using System.Text.Json;

namespace Thanos.Parsers;

public static class GameParser
{
    #region Pre-compiled UTF-8 Property Names

    // Proprietà per Game
    private static ReadOnlySpan<byte> IdBytes => "id"u8;
    private static ReadOnlySpan<byte> RulesetBytes => "ruleset"u8;
    private static ReadOnlySpan<byte> MapBytes => "map"u8;
    private static ReadOnlySpan<byte> SourceBytes => "source"u8;
    private static ReadOnlySpan<byte> TimeoutBytes => "timeout"u8;

    // Proprietà per Ruleset
    private static ReadOnlySpan<byte> SettingsBytes => "settings"u8;

    // Proprietà per RulesetSettings
    private static ReadOnlySpan<byte> FoodSpawnChanceBytes => "foodSpawnChance"u8;
    private static ReadOnlySpan<byte> MinimumFoodBytes => "minimumFood"u8;
    private static ReadOnlySpan<byte> HazardDamagePerTurnBytes => "hazardDamagePerTurn"u8;
    private static ReadOnlySpan<byte> RoyaleBytes => "royale"u8;
    private static ReadOnlySpan<byte> SquadBytes => "squad"u8;

    // Proprietà per Royale
    private static ReadOnlySpan<byte> ShrinkEveryNTurnsBytes => "shrinkEveryNTurns"u8;

    // Proprietà per Squad
    private static ReadOnlySpan<byte> AllowBodyCollisionsBytes => "allowBodyCollisions"u8;
    private static ReadOnlySpan<byte> SharedEliminationBytes => "sharedElimination"u8;
    private static ReadOnlySpan<byte> SharedHealthBytes => "sharedHealth"u8;
    private static ReadOnlySpan<byte> SharedLengthBytes => "sharedLength"u8;
    
    // Valori per l'enum Map
    private static ReadOnlySpan<byte> MapStandardBytes => "standard"u8;
    private static ReadOnlySpan<byte> MapRoyaleBytes => "royale"u8;
    private static ReadOnlySpan<byte> MapConstrictorBytes => "constrictor"u8;
    private static ReadOnlySpan<byte> MapSnailModeBytes => "snailMode"u8;

    // Valori per l'enum Source
    private static ReadOnlySpan<byte> SourceTournamentBytes => "tournament"u8;
    private static ReadOnlySpan<byte> SourceLeagueBytes => "league"u8;
    private static ReadOnlySpan<byte> SourceArenaBytes => "arena"u8;
    private static ReadOnlySpan<byte> SourceChallengeBytes => "challenge"u8;
    private static ReadOnlySpan<byte> SourceCustomBytes => "custom"u8;


    #endregion

    public static Game Parse(ref Utf8JsonReader reader)
    {
        var id = Guid.Empty;
        var ruleset = new Ruleset();
        var map = Map.Unknown;
        var source = Source.Unknown;
        var timeout = 500;

        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals(IdBytes))
            {
                reader.Read();
                if (!reader.TryGetGuid(out id)) throw new JsonException("Expected a valid GUID for 'id'.");
            }
            else if (reader.ValueTextEquals(RulesetBytes))
            {
                reader.Read();
                ruleset = ParseRuleset(ref reader);
            }
            else if (reader.ValueTextEquals(MapBytes))
            {
                reader.Read();
                map = ParseMap(ref reader);
            }
            else if (reader.ValueTextEquals(SourceBytes))
            {
                reader.Read();
                source = ParseSource(ref reader);
            }
            else if (reader.ValueTextEquals(TimeoutBytes))
            {
                reader.Read();
                timeout = reader.GetInt32();
            }
            else
            {
                reader.Skip();
            }
        }

        return new Game(id, ruleset, map, source, timeout);
    }

    private static Ruleset ParseRuleset(ref Utf8JsonReader reader)
    {
        RulesetSettings settings = default;

        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of Ruleset object.");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals(SettingsBytes))
            {
                reader.Read();
                settings = ParseRulesetSettings(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }

        return new Ruleset(settings);
    }

    private static RulesetSettings ParseRulesetSettings(ref Utf8JsonReader reader)
    {
        int foodSpawnChance = 0, minimumFood = 0, hazardDamagePerTurn = 0;
        Royale? royale = null;
        Squad? squad = null;

        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of RulesetSettings object.");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals(FoodSpawnChanceBytes))
            {
                reader.Read();
                foodSpawnChance = reader.GetInt32();
            }
            else if (reader.ValueTextEquals(MinimumFoodBytes))
            {
                reader.Read();
                minimumFood = reader.GetInt32();
            }
            else if (reader.ValueTextEquals(HazardDamagePerTurnBytes))
            {
                reader.Read();
                hazardDamagePerTurn = reader.GetInt32();
            }
            else if (reader.ValueTextEquals(RoyaleBytes))
            {
                reader.Read();
                royale = ParseRoyale(ref reader);
            }
            else if (reader.ValueTextEquals(SquadBytes))
            {
                reader.Read();
                squad = ParseSquad(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }

        return new RulesetSettings(foodSpawnChance, minimumFood, hazardDamagePerTurn, royale, squad);
    }

    private static Royale ParseRoyale(ref Utf8JsonReader reader)
    {
        var shrink = 0;

        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of Royale object.");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals(ShrinkEveryNTurnsBytes))
            {
                reader.Read();
                shrink = reader.GetInt32();
            }
            else
            {
                reader.Skip();
            }

        return new Royale(shrink);
    }

    private static Squad ParseSquad(ref Utf8JsonReader reader)
    {
        bool allowBodyCollisions = false, sharedElimination = false, sharedHealth = false, sharedLength = false;

        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start of Squad object.");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals(AllowBodyCollisionsBytes))
            {
                reader.Read();
                allowBodyCollisions = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals(SharedEliminationBytes))
            {
                reader.Read();
                sharedElimination = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals(SharedHealthBytes))
            {
                reader.Read();
                sharedHealth = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals(SharedLengthBytes))
            {
                reader.Read();
                sharedLength = reader.GetBoolean();
            }
            else
            {
                reader.Skip();
            }
        }

        return new Squad(allowBodyCollisions, sharedElimination, sharedHealth, sharedLength);
    }

    private static Map ParseMap(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected a string for 'map'.");

        // Usa i campi statici pre-compilati
        if (reader.ValueTextEquals(MapStandardBytes)) return Map.Standard;
        if (reader.ValueTextEquals(MapRoyaleBytes)) return Map.Royale;
        if (reader.ValueTextEquals(MapConstrictorBytes)) return Map.Constrictor;
        if (reader.ValueTextEquals(MapSnailModeBytes)) return Map.SnailMode;

        return Map.Unknown;
    }

    private static Source ParseSource(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected a string for 'source'.");

        // Usa i campi statici pre-compilati
        if (reader.ValueTextEquals(SourceTournamentBytes)) return Source.Tournament;
        if (reader.ValueTextEquals(SourceLeagueBytes)) return Source.League;
        if (reader.ValueTextEquals(SourceArenaBytes)) return Source.Arena;
        if (reader.ValueTextEquals(SourceChallengeBytes)) return Source.Challenge;
        if (reader.ValueTextEquals(SourceCustomBytes)) return Source.Custom;

        return Source.Unknown;
    }
}