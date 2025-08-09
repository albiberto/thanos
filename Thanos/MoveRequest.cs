namespace Thanos;

public readonly struct MoveRequest(Game game, int turn, BattleArena arena)
{
    public Game Game { get; } = game;
    public int Turn{ get; } = turn;
    public BattleArena Arena { get; } = arena;
}