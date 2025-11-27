namespace Thanos.MCST;

public readonly struct RootMoveStat(byte move, int visits, float score)
{
    public byte Move { get; } = move;
    public int Visits { get; } = visits;
    public float Score { get; } = score; // Utile per debug o tie-breaking
}