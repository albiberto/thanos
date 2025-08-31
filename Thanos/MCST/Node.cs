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
    
    public long StateHash; // <-- NUOVO CAMPO
    
    public void Initialize(int parentIndex, byte move)
    {
        StateHash = 0; // Inizializza a 0
        
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
    
    /// <summary>
    /// Trova l'INDICE del figlio che è stato visitato più volte.
    /// Questo è il metodo più robusto per la decisione finale.
    /// </summary>
    public int SelectMostVisitedChild(NodeMemoryPool pool)
    {
        // Se questo nodo non ha figli, non c'è nulla da scegliere.
        if (IsLeafNode)
        {
            return -1;
        }

        int bestChildIndex = -1;
        int maxVisits = -1;

        // Itera su tutti i figli
        int currentChildIndex = FirstChildIndex;
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

