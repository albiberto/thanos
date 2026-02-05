using System.Runtime.CompilerServices;
using Thanos.Shared;
using Thanos.War.Snake;
using Thanos.War.Structures;

namespace Thanos.War.State;

// PURE DATA VIEW
public readonly ref struct GameState(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes,
    NeighborsMatrix neighborsMatrix)
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;
    public readonly NeighborsMatrix Neighbors = neighborsMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in GameState source)
    {
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }
}