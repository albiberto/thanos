using System.Runtime.CompilerServices;
using Thanos.War.State;
using Thanos.War.Structures;

namespace Thanos.War.Rules;

/// <summary>
/// Handles stochastic (random) environmental events.
/// Used primarily during MCTS Rollouts (Simulations).
/// </summary>
public static class EnvironmentRules
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SimulateFoodSpawn(ref GameState state, GameContext context)
    {
        // 1. Fast Exit: Accesso diretto a MinFood e Chance dal Context
        if (state.Food.PopCount() >= context.MinFood && Random.Shared.Next(0, 100) >= context.FoodSpawnChance) 
            return;

        // 2. Spawn Logic (Try-and-fail approach for performance)
        // Usiamo Area pre-calcolata dal Context
        var area = context.Area;

        for (var i = 0; i < 10; i++)
        {
            var spot = (ushort)Random.Shared.Next(0, area);
            
            // Check collisione: Non su un serpente, cibo o hazard
            if (state.Snakes.IsSet(spot) || state.Food.IsSet(spot) || state.Hazards.IsSet(spot)) 
                continue;

            state.Food.Set(spot);
            break;
        }
    }
}