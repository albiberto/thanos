using System;
using System.Collections.Generic;
using Thanos.War;

namespace Thanos.MCST;

public class MctsNode : IDisposable
{
    private static readonly double ExplorationConstant = 1.414; // sqrt(2)

    public WarArena GameState { get; private set; }
    public MctsNode? Parent { get; }
    public List<MctsNode> Children { get; } = new();
    public MoveDirection MoveThatLedToThisNode { get; }

    public int Visits { get; private set; }
    public double Wins { get; private set; }
    public bool IsTerminalNode { get; } // Un nodo è "terminale" se la partita è finita

    /// <summary>
    /// Costruttore per il nodo radice.
    /// </summary>
    public MctsNode(in WarArena initialGameState)
    {
        GameState = initialGameState;
        Parent = null;
        MoveThatLedToThisNode = MoveDirection.None;
        // CORREZIONE: Controlla l'esito per vedere se la partita è già finita
        IsTerminalNode = GameState.AssessOutcome().outcome != WarOutcome.Ongoing;
    }
    
    /// <summary>
    /// Costruttore per i nodi figli.
    /// </summary>
    public MctsNode(in WarArena newGameState, MctsNode parent, MoveDirection move)
    {
        GameState = newGameState;
        Parent = parent;
        MoveThatLedToThisNode = move;
        // CORREZIONE: Controlla l'esito per vedere se la partita è finita
        IsTerminalNode = GameState.AssessOutcome().outcome != WarOutcome.Ongoing;
    }

    /// <summary>
    /// Seleziona il figlio più promettente usando la formula UCB1.
    /// </summary>
    public MctsNode? FindBestChildUcb1()
    {
        MctsNode? bestChild = null;
        double bestScore = double.MinValue;

        foreach (var child in Children)
        {
            if (child.Visits == 0) return child;

            double ucb1 = (child.Wins / child.Visits) + 
                          ExplorationConstant * Math.Sqrt(Math.Log(Visits) / child.Visits);
            
            if (ucb1 > bestScore)
            {
                bestScore = ucb1;
                bestChild = child;
            }
        }
        return bestChild;
    }
    
    public void UpdateStats(double result)
    {
        Visits++;
        Wins += result;
    }
    
    public void Dispose()
    {
        GameState.Dispose();
        foreach (var child in Children)
        {
            child.Dispose();
        }
    }
}