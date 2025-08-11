using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.War;

namespace Thanos.MCST;

/// <summary>
/// Un nodo dell'albero MCTS, ottimizzato per l'allocazione in un MemoryPool.
/// È una struct non gestita, quindi non ci sono allocazioni sul GC heap.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Node
{
    private const double ExplorationConstant = 1.414; // sqrt(2)

    // Dati per la logica MCTS
    public Node* Parent;
    public double Wins;
    public int Visits;
    public MoveDirection MoveThatLedToThisNode;

    // Struttura ad albero (sostituisce List<MctsNode>)
    public Node** Children; // Un puntatore a un array di puntatori ai figli
    public int ChildCount;

    // Stato del gioco
    public WarArena* WarArena; // Puntatore all'arena di questo nodo
    public bool IsTerminal;
    
    public void Initialize(WarArena* arena, Node* parent, MoveDirection move)
    {
        WarArena = arena;
        Parent = parent;
        MoveThatLedToThisNode = move;
        Wins = 0;
        Visits = 0;
        Children = null;
        ChildCount = 0;
        IsTerminal = WarArena->AssessOutcome().outcome != WarOutcome.Ongoing;
    }

    public void UpdateStats(double result)
    {
        Visits++;
        Wins += result;
    }

    public Node* FindBestChildUcb1()
    {
        Node* bestChild = null;
        double bestScore = double.MinValue;

        for (int i = 0; i < ChildCount; i++)
        {
            var child = Children[i];
            if (child->Visits == 0)
            {
                // Questo figlio non è mai stato esplorato, è la scelta prioritaria
                return child;
            }

            double ucb1 = (child->Wins / child->Visits) +
                          ExplorationConstant * Math.Sqrt(Math.Log(Visits) / child->Visits);

            if (ucb1 > bestScore)
            {
                bestScore = ucb1;
                bestChild = child;
            }
        }
        return bestChild;
    }
}