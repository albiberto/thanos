using System;
using System.Collections.Generic;
using Thanos.War;

namespace Thanos.MCST;

public unsafe class MctsEngine
{
    private readonly Random _random = new();
    private readonly List<MoveDirection> _legalMovesCache = new(4); // Cache per evitare allocazioni

    public MoveDirection FindBestMove(in WarArena rootState, WarContext* context)
    {
        var rootArena = new WarArena(in rootState);
        var rootNode = new MctsNode(rootArena);

        try
        {
            // Esegui per un numero fisso di iterazioni
            const int iterations = 10000;
            for (var i = 0; i < iterations; i++)
            {
                // 1. Selezione
                var promisingNode = SelectPromisingNode(rootNode);

                // 2. Espansione
                if (!promisingNode.IsTerminalNode)
                {
                    ExpandNode(promisingNode, context);
                }
                
                // 3. Simulazione
                var nodeToExplore = promisingNode;
                if (promisingNode.Children.Count > 0)
                {
                    nodeToExplore = promisingNode.Children[_random.Next(promisingNode.Children.Count)];
                }
                var result = SimulateRandomPlayout(nodeToExplore);

                // 4. Propagazione all'indietro
                Backpropagate(nodeToExplore, result);
            }

            // Scelta finale: la mossa che porta al figlio più visitato
            MctsNode? bestChild = null;
            var maxVisits = -1;
            foreach (var child in rootNode.Children)
            {
                if (child.Visits > maxVisits)
                {
                    maxVisits = child.Visits;
                    bestChild = child;
                }
            }
            return bestChild?.MoveThatLedToThisNode ?? MoveDirection.Up;
        }
        finally
        {
            rootNode.Dispose();
        }
    }

    private MctsNode SelectPromisingNode(MctsNode node)
    {
        // Scende l'albero usando la selezione UCB1 finché non trova una foglia
        while (node.Children.Count > 0 && !node.IsTerminalNode)
        {
            node = node.FindBestChildUcb1() ?? node;
        }
        return node;
    }

    
    private unsafe void ExpandNode(MctsNode node, WarContext* context)
    {
        // Ottiene le mosse legali dallo stato del nodo
        node.WarArea.GetLegalMoves(0, _legalMovesCache);

        foreach (var move in _legalMovesCache)
        {
            // --- LA CORREZIONE È QUI ---
            // 1. Prima mettiamo lo stato in una variabile locale stabile
            var parentState = node.WarArea;
            // 2. Poi passiamo l'indirizzo di quella variabile al costruttore di copia
            var newState = new WarArena(in parentState);
        
            // Per ora, assumiamo che tutti gli altri serpenti facciano una mossa casuale
            // Questa logica va migliorata per creare un array di mosse per tutti
            var movesForAllSnakes = new Span<MoveDirection>(new MoveDirection[] { move });
            newState.Simulate(movesForAllSnakes);
        
            var childNode = new MctsNode(newState, node, move);
            node.Children.Add(childNode);
        }

        // Segna il nodo come "completamente espanso"
        // così la Selezione sa di dover scendere nei suoi figli.
        // (Questa logica potrebbe essere più complessa, ma per ora va bene).
        // node.SetExpanded(); // Se hai questo metodo nel nodo
    }

private double SimulateRandomPlayout(MctsNode node)
{
    // 1. CORREZIONE: Assegna la proprietà a una variabile locale prima di passarla con 'in'
    var initialState = node.WarArea;
    var tempState = new WarArena(in initialState);

    try
    {
        const int maxDepth = 50; 
        for (int i = 0; i < maxDepth; i++)
        {
            var outcomeResult = tempState.AssessOutcome();
            if (outcomeResult.outcome != WarOutcome.Ongoing)
            {
                // La partita è finita, restituisci il risultato dal nostro punto di vista (snake 0)
                if (outcomeResult.outcome == WarOutcome.Victory && outcomeResult.winnerIndex == 0)
                    return 1.0; // Abbiamo vinto
                
                return 0.0; // Abbiamo perso o pareggiato
            }

            // 2. CORREZIONE: Genera mosse casuali per TUTTI i serpenti vivi
            
            // Crea un array di mosse grande quanto il numero di serpenti INIZIALI.
            // Questo array mappa 1:1 con gli indici dei serpenti (0, 1, 2, ...).
            var randomMoves = new MoveDirection[tempState.Context->InitialActiveSnakes];

            // Itera su tutti i possibili slot dei serpenti
            for (int snakeIndex = 0; snakeIndex < tempState.Context->InitialActiveSnakes; snakeIndex++)
            {
                // Se il serpente è vivo, scegli una mossa casuale per lui
                if (!tempState.GetSnake(snakeIndex).Dead)
                {
                    tempState.GetLegalMoves(snakeIndex, _legalMovesCache);
                    if (_legalMovesCache.Count > 0)
                    {
                        randomMoves[snakeIndex] = _legalMovesCache[_random.Next(_legalMovesCache.Count)];
                    }
                    else
                    {
                        // Il serpente è intrappolato, forziamo una mossa (morirà comunque)
                        randomMoves[snakeIndex] = MoveDirection.Up;
                    }
                }
            }

            // Simula il turno con le mosse casuali per tutti
            tempState.Simulate(randomMoves);
        }
        
        // La partita è troppo lunga, la consideriamo un pareggio
        return 0.5;
    }
    finally
    {
        tempState.Dispose();
    }
}

    private void Backpropagate(MctsNode node, double result)
    {
        var tempNode = node;
        while (tempNode != null)
        {
            tempNode.UpdateStats(result);
            tempNode = tempNode.Parent;
        }
    }
}