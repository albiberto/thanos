using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Node
{
    public Node* Parent;
    
    public Node* Child1, Child2, Child3, Child4;
    public uint ChildrenCount;
    
    public long Visits;
    public double Wins;
    
    public byte MoveThatLedToThisNode;
    public bool IsTerminal;
    
    /// <summary>
    /// INDEXER: Fornisce un accesso simile a un array ai campi Child1-4.
    /// Questa è la chiave per avere codice pulito nei metodi sottostanti.
    /// </summary>
    public Node* this[int index]
    {
        get
        {
            return index switch
            {
                0 => Child1,
                1 => Child2,
                2 => Child3,
                3 => Child4,
                _ => throw new IndexOutOfRangeException()
            };
        }
        set
        {
            switch (index)
            {
                case 0: Child1 = value; break;
                case 1: Child2 = value; break;
                case 2: Child3 = value; break;
                case 3: Child4 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public readonly bool IsLeaf => ChildrenCount == 0;
    
    /// <summary>
    /// Imposta questo nodo come "terminale", indicando che la partita da questo stato è finita.
    /// </summary>
    public void SetTerminal() => IsTerminal = true;
    
    public Node* FindChildByMove(byte move)
    {
        for (var i = 0; i < ChildrenCount; i++)
        {
            if (this[i]->MoveThatLedToThisNode == move) return this[i];
        }
        
        return null; // Figlio non trovato
    }

    public Node* GetBestChild()
    {
        Node* bestChild = null;
        var bestScore = double.NegativeInfinity;
        const double explorationConstant = 1.414;

        // Grazie all'indexer, il ciclo rimane identico, pulito e senza allocazioni!
        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = this[i]; // Usa l'indexer

            if (child->Visits == 0) return child;
            
            var exploitationScore = child->Wins / child->Visits;
            var explorationScore = explorationConstant * Math.Sqrt(Math.Log(Visits) / child->Visits);
            var uctScore = exploitationScore + explorationScore;

            if (uctScore > bestScore)
            {
                bestScore = uctScore;
                bestChild = child;
            }
        }
        return bestChild;
    }
    
    public void AddChild(Node* child, byte move)
    {
        if (ChildrenCount >= 4) return;

        // Grazie all'indexer, la logica per aggiungere un figlio rimane semplice.
        this[(int)ChildrenCount] = child; // Usa l'indexer

        child->Parent = (Node*)Unsafe.AsPointer(ref this);
        child->MoveThatLedToThisNode = move;
    
        ChildrenCount++;
    }
}