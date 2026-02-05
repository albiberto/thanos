using System.Runtime.CompilerServices;
using Thanos.Shared;
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
    public static void SimulateFoodSpawn(ref GameState state, int foodSpawnChance, int minimumFood)
    {
        // 1. Fast Exit: Se c'è abbastanza cibo o il dado dice no.
        // Nota: Random.Shared è thread-safe in .NET 6+, ma per determinismo estremo in futuro 
        // potremmo voler passare un oggetto 'FastRandom' custom.
        if (state.Food.PopCount() >= minimumFood && Random.Shared.Next(0, 100) >= foodSpawnChance) 
            return;

        // 2. Spawn Logic (Try-and-fail approach for performance)
        // Tentiamo max 10 volte di trovare un posto libero. 
        // È molto più veloce che calcolare l'elenco delle celle libere e sceglierne una.
        var area = state.Area;
        for (var i = 0; i < 10; i++)
        {
            var spot = (ushort)Random.Shared.Next(0, area);
            
            // Check collisione: Non su un serpente (corpo o testa) e non su cibo esistente
            if (state.Snakes.IsSet(spot) || state.Food.IsSet(spot) || state.Hazards.IsSet(spot)) 
                continue;

            state.Food.Set(spot);
            break;
        }
    }
}