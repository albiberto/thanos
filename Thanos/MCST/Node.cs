using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential)]
public struct Node
{
    public int ParentIndex;
    public int FirstChildIndex;
    public int NextSiblingIndex;

    public double Wins;
    public int Visits;
    public byte MoveThatLedToThisNode;
    public bool IsTerminal;

    public long StateHash;

    public void Initialize(int parentIndex, byte move, long stateHash)
    {
        StateHash = stateHash;

        ParentIndex = parentIndex;
        MoveThatLedToThisNode = move;

        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        Wins = 0;
        Visits = 0;
        IsTerminal = false;
    }

    public readonly bool IsLeafNode => FirstChildIndex == -1;

    public void UpdateStats(double result)
    {
        Visits++;
        Wins += result;
    }

    /// <summary>
    ///     Trova l'INDICE del figlio che è stato visitato più volte.
    ///     Questo è il metodo più robusto per la decisione finale.
    /// </summary>
    public int SelectMostVisitedChild(NodeMemoryPool pool)
    {
        // Se questo nodo non ha figli, non c'è nulla da scegliere.
        if (IsLeafNode) return -1;

        var bestChildIndex = -1;
        var maxVisits = -1;

        // Itera su tutti i figli
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

    public ChildEnumerator GetChildren(NodeMemoryPool pool) => new(FirstChildIndex, pool);
}