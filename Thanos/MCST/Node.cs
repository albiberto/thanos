using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct Node
{
    // Blocco 1: Dati "caldi" per selezione e valutazione (16 byte)
    // Questi campi sono usati più di frequente durante l'attraversamento dell'albero.
    public int Visits;
    public float Wins;

    public int FirstChildIndex;
    public int NextSiblingIndex;

    // Blocco 2: Dati di stato e struttura, RIORDINATI per allineamento (16 byte)
    public long Hash; // 8 byte -> Messo per primo, si allineerà perfettamente.
    public int ParentIndex; // 4 byte
    public ushort Generation; // 2 byte
    public byte Move; // 1 byte
    public bool IsTerminal; // 1 byte

    public void PlacementRoot(int parentIndex, byte move, long hash)
    {
        Visits = 0;
        Wins = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        Generation = 0; // Fissa la generazione a 0 per la radice
        Hash = hash;
        Move = move;
        IsTerminal = false;
    }

    public void PlacementNew(int parentIndex, byte move, long hash, ushort parentGeneration)
    {
        Visits = 0;
        Wins = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        Generation = (ushort)(parentGeneration + 1); // Calcola la generazione del figlio
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

    public int SelectMostVisitedChild(NodeMemoryPool pool)
    {
        if (IsLeafNode) return -1;

        var bestChildIndex = -1;
        var maxVisits = -1;

        var currentChildIndex = FirstChildIndex;
        while (currentChildIndex != -1)
        {
            ref var childNode = ref pool[currentChildIndex];
            if (childNode.Visits > maxVisits)
            {
                maxVisits = childNode.Visits;
                bestChildIndex = currentChildIndex;
            }

            currentChildIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }
}