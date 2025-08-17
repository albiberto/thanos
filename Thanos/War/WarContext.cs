using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly struct WarContext
{
    public readonly int Turn;
    public readonly int Width, Height, Area, SnakeCount;
    public readonly int FoodSpawnChance, MinimumFood, HazardDamagePerTurn;
    public readonly int? ShrinkEveryNTurns;
    public readonly bool? AllowBodyCollisions, SharedElimination, SharedHealth, SharedLength;
    public readonly int Timeout;
    
    public static readonly WarContext Worst = new(Constants.MaxWidth, Constants.MaxHeight, Constants.MaxSnakes);

    private WarContext(int width, int height, int snakeCount)
    {
        Width = width;
        Height = height;
        Area = width * height;

        SnakeCount = snakeCount;
    }

    public WarContext(in Request request) : this(request.Board.Width, request.Board.Height, request.Board.Snakes.Length)
    {
        Turn = request.Turn;
        Timeout = Math.Min(request.Game.Timeout * Constants.TimeoutRatio / 100, Constants.MinTimeout);
        
        FoodSpawnChance = request.Game.Ruleset.Settings.FoodSpawnChance;
        MinimumFood = request.Game.Ruleset.Settings.MinimumFood;
        HazardDamagePerTurn = request.Game.Ruleset.Settings.HazardDamagePerTurn;
        
        ShrinkEveryNTurns = request.Game.Ruleset.Settings.Royale?.ShrinkEveryNTurns;
        
        AllowBodyCollisions = request.Game.Ruleset.Settings.Squad?.AllowBodyCollisions;
        SharedElimination = request.Game.Ruleset.Settings.Squad?.SharedElimination;
        SharedHealth = request.Game.Ruleset.Settings.Squad?.SharedHealth;
        SharedLength = request.Game.Ruleset.Settings.Squad?.SharedLength;
    } 
}