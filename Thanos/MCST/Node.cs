using System.Runtime.InteropServices;
using Thanos.Common;
using Thanos.Memory;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct Node
{
    public int Visits;
    public float Wins;
    public int FirstChildIndex;
    public int NextSiblingIndex;
    public long Hash;
    public int ParentIndex;
    public byte PlayerIndex;
    public byte Move;
    public bool IsTerminal;

    public void PlacementRoot(long hash)
    {
        Visits = 0;
        Wins = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        PlayerIndex = 0;
        Hash = hash;
        Move = Moves.None;
        IsTerminal = false;
    }

    public void PlacementNew(int parentIndex, byte move, long hash, byte playerIndex)
    {
        Visits = 0;
        Wins = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        PlayerIndex = playerIndex;
        Hash = hash;
        Move = move;
        IsTerminal = false;
    }

    public readonly bool IsLeafNode => FirstChildIndex == -1;

    public void UpdateStats(float result)
    {
        Visits++;
        Wins += result;
    }

    public void NewRoot() => ParentIndex = -1;
}