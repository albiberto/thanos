using System.Runtime.CompilerServices;
using Thanos.War.Snake;
using Thanos.War.Structures;

namespace Thanos.War.State;

public readonly ref struct GameState(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes) // Removed NeighborsMatrix
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in GameState source)
    {
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }
}