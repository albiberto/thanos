// Thanos/Game.cs

using System.Runtime.InteropServices;
using System.Text.Json;

namespace Thanos;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Game
{
    // Dati readonly estratti dal JSON, es:
    public readonly int HazardDamage;
    // Potremmo aggiungere altri campi readonly se necessario (es. timeout, ruleset name)
    
    // Costruttore per il nostro "Placement New"
    public Game(JsonElement gameJson)
    {
        HazardDamage = gameJson
            .GetProperty("ruleset")
            .GetProperty("settings")
            .GetProperty("hazardDamagePerTurn")
            .GetInt32();
    }
}

// Thanos/Board.cs

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Board
{
    // I due componenti principali come richiesto
    public BattleField Field;
    public BattleSnake* Snakes; // Puntatore all'inizio dell'array di serpenti
}