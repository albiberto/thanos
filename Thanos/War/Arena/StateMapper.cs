using System.Runtime.CompilerServices;
using Thanos.SourceGen;

namespace Thanos.War.Arena;

public static class StateMapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitializeFromRequest(ref GameState state, in Request request, ReadOnlySpan<string> orderedIds)
    {
        // 1. Clean Local State
        state.System.Initialize();

        // 2. Clean Global State
        state.Food.Clear();
        state.Hazards.Clear();
        state.Snakes.Clear();

        var board = request.Board;

        // 3. Initialize Snakes & Sync Global Bitboard
        foreach (var snakeData in board.Snakes)
            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (!string.Equals(orderedIds[i], snakeData.Id, StringComparison.Ordinal)) continue;

                var snake = state.System[i];
                snake.Initialize(snakeData);

                // Project body onto global map
                state.Snakes.Or(snake.Body);
                break;
            }

        // 4. Initialize Environment
        foreach (var coordinate in board.Food) state.Food.Set(coordinate);
        foreach (var coordinate in board.Hazards) state.Hazards.Set(coordinate);
    }
}