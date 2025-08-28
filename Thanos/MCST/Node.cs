using System.Runtime.CompilerServices;
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
    
    public void Initialize(int parentIndex, byte move)
    {
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
    /// Trova l'INDICE del figlio più promettente usando la formula UCT.
    /// </summary>
    public int SelectBestChild(NodeMemoryPool pool, double explorationParameter = 1.41)
    {
        var bestScore = double.MinValue;
        var bestChildIndex = -1;
        
        // Se non ci sono figli, non possiamo selezionare nulla.
        if (IsLeafNode) return -1;
        
        var logParentVisits = Math.Log(Visits);

        foreach (var childIndex in GetChildren(pool))
        {
            ref var childNode = ref pool[childIndex];

            // Un figlio mai visitato ha priorità assoluta per l'esplorazione.
            if (childNode.Visits == 0) return childIndex;

            var exploitation = childNode.Wins / childNode.Visits;
            var exploration = Math.Sqrt(logParentVisits / childNode.Visits);
            var uctScore = exploitation + explorationParameter * exploration;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChildIndex = childIndex;
            }
        }
        
        return bestChildIndex;
    }
    
    public ChildEnumerator GetChildren(NodeMemoryPool pool) => new(FirstChildIndex, pool);
    

    /// <summary>
    /// Ottiene un riferimento al nodo genitore.
    /// </summary>
    public ref Node GetParent(NodeMemoryPool pool)
    {
        if (ParentIndex == -1)
        {
            return ref Unsafe.NullRef<Node>();
        }
        return ref pool[ParentIndex];
    }
}

