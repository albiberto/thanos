using System.Numerics;
using System.Text.Json;
using Thanos.Common;
using Thanos.Memory;
using Thanos.War;
using Thanos.War.Snake;

namespace Thanos.MCST;

public sealed class Worker(WarMemoryPool warPool, NodeMemoryPool nodePool)
{
    private static readonly byte[] AllMovesArray = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
    
    private readonly WarMemoryPool _warPool = warPool;
    private readonly NodeMemoryPool _nodePool = nodePool;

    public void RunIteration(int rootNodeIndex, in MemorySlot rootSlot)
    {
        var rootArena = rootSlot.Arena;
        Console.WriteLine("===========================================================");
        Console.WriteLine($"[ITERATION START] Root Node Index: {rootNodeIndex}");
        rootArena.Me.GetSpans(out var body11, out var body21);
        Console.WriteLine($"Body of snake length: {rootArena.Me.Length}, health: {rootArena.Me.Health}, body1: {string.Join(",", body11.ToArray())}, body2: {string.Join(",", body21.ToArray())}");
        Console.WriteLine($"Snakes Bitboard: {string.Join(',', rootArena.Grid.Snakes.GetRawData.ToArray())}");
        Console.WriteLine("===========================================================");
        
        // 1. Setup - Prepara uno stato di lavoro copiando lo stato della radice.
        var workingSlot = _warPool.GetNext();
        workingSlot.CloneFrom(in rootSlot);
        

        Console.WriteLine("===========================================================");
        Console.WriteLine($"[SETUP] Working slot prepared.");
        workingSlot.Arena.Me.GetSpans(out var body12, out var body22);
        Console.WriteLine($"Body of snake length: {workingSlot.Arena.Me.Length}, health: {workingSlot.Arena.Me.Health}, body1: {string.Join(",", body12.ToArray())}, body2: {string.Join(",", body22.ToArray())}");
        Console.WriteLine($"Snakes Bitboard: {string.Join(',', workingSlot.Arena.Grid.Snakes.GetRawData.ToArray())}");
        Console.WriteLine("===========================================================");
        
        // 2. Selection - Scende nell'albero fino a un nodo foglia.
        //    'Select' è l'unica fase che modifica l'arena principale per farla avanzare.
        var leafNodeIndex = Select(rootNodeIndex, workingSlot.Arena);
        ref var leafNode = ref _nodePool[leafNodeIndex];
        
        Console.WriteLine("===========================================================");
        Console.WriteLine("After Selection:");
        workingSlot.Arena.Me.GetSpans(out var body13, out var body23);
        Console.WriteLine($"Body of snake length: {workingSlot.Arena.Me.Length}, health: {workingSlot.Arena.Me.Health}, body1: {string.Join(",", body13.ToArray())}, body2: {string.Join(",", body23.ToArray())}");
        Console.WriteLine($"Snakes Bitboard: {string.Join(',', workingSlot.Arena.Grid.Snakes.GetRawData.ToArray())}");
        Console.WriteLine("===========================================================");

        double simulationResult;
        if (workingSlot.Arena.ILose || leafNode.IsTerminal)
        {
            simulationResult = workingSlot.Arena.Outcome();
        }
        else
        {
            // 3. Expansion - Se il nodo è una foglia, crea i suoi figli.
            //    Passiamo l'arena con 'in' per garantire che 'Expand' non la modifichi.
            if (leafNode.IsLeafNode)
            {
                // Passiamo l'intero slot di memoria, non solo l'arena
                Expand(leafNodeIndex, ref leafNode, in workingSlot);
            }

            // 4. Simulation (Rollout) - Simula una partita partendo dallo stato del nodo foglia.
            var simulationSlot = _warPool.GetNext();
            simulationSlot.CloneFrom(in workingSlot); // Crea una copia isolata
            var simulationArena = simulationSlot.Arena;
            Simulate(ref simulationArena); // Simula sulla copia
            
            var finalOutcome = simulationArena.Outcome();

            if (finalOutcome != 0.0f)
            {
                simulationResult = finalOutcome;
            }
            else
            {
                // Se la simulazione finisce in timeout, usiamo un'euristica minima.
                simulationResult = simulationArena.Me.Length * 0.1 + simulationArena.Me.Health * 0.01;
            }
        }
    
        // 5. Backpropagation - Propaga il risultato all'indietro.
        Backpropagate(leafNodeIndex, simulationResult);
    }
    
    // --- COMMENTO ---
    // 'Select' è CORRETTO così com'è. Il suo scopo è proprio modificare l'arena
    // passata per riferimento per farla corrispondere allo stato del nodo foglia trovato.
    private int Select(int startNodeIndex, WarArena arena)
    {
        var currentIndex = startNodeIndex;
        
        while (true)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            if (currentNode.IsLeafNode || currentNode.IsTerminal) return currentIndex;

            var nextNodeIndex = currentNode.SelectBestChild(_nodePool);
            if (nextNodeIndex == -1) return currentIndex;

            currentIndex = nextNodeIndex;
            ref var childNode = ref _nodePool[currentIndex];
            
            arena.ApplySingleMove(childNode.MoveThatLedToThisNode, true);
        }
    }
    
    private static string MoveToString(byte move) => move switch
    {
        Moves.Up => "Up",
        Moves.Down => "Down",
        Moves.Left => "Left",
        Moves.Right => "Right",
        _ => "None"
    };

    // --- COMMENTO ---
    // La firma di 'Expand' ora usa 'in WarArena'. Questo è un vincolo a livello di compilatore
    // che ci impedisce di modificare accidentalmente l'arena originale.
    // La firma ora accetta un MemorySlot per permettere la clonazione
private void Expand(int nodeIndex, ref Node node, in MemorySlot parentSlot)
{
    Console.WriteLine($"[EXPAND] Espansione del nodo {nodeIndex}: " +
                      $"Score={node.Wins:F2}, Visits={node.Visits}, " +
                      $"Move={MoveToString(node.MoveThatLedToThisNode)}");
                      
    if (node.IsTerminal) return;

    var parentArena = parentSlot.Arena;
    Console.WriteLine("===========================================================");
    Console.WriteLine("Expanding Arena State:");
    parentArena.Me.GetSpans(out var body11, out var body21);
    Console.WriteLine($"Body of snake length: {parentArena.Me.Length}, health: {parentArena.Me.Health}, body1: {string.Join(",", body11.ToArray())}, body2: {string.Join(",", body21.ToArray())}");
    Console.WriteLine($"Snakes Bitboard: {string.Join(',', parentArena.Grid.Snakes.GetRawData.ToArray())}");
    Console.WriteLine("===========================================================");
    
    var legalMoves = parentArena.GetLegalMoves();

    Console.WriteLine($"[EXPAND] Legal Moves Bitmask: {Convert.ToString(legalMoves, 2).PadLeft(4, '0')}");
    
    if (legalMoves == 0)
    {
        node.IsTerminal = true;
        return;
    }

    var lastChildIndex = -1;

    foreach (var move in AllMovesArray)
    {
        if ((legalMoves & move) == 0) continue;

        var childSlot = _warPool.GetNext();
        childSlot.CloneFrom(in parentSlot);
        var childArena = childSlot.Arena;
        
        Console.WriteLine("===========================================================");
        Console.WriteLine($"[EXPAND] Applying move {MoveToString(move)} to create child node.");
        childArena.Me.GetSpans(out var body12, out var body22);
        Console.WriteLine($"Body of snake length: {childArena.Me.Length}, health: {childArena.Me.Health}, body1: {string.Join(",", body12.ToArray())}, body2: {string.Join(",", body22.ToArray())}");
        Console.WriteLine($"Snakes Bitboard: {string.Join(',', childArena.Grid.Snakes.GetRawData.ToArray())}");
        Console.WriteLine("===========================================================");
        
        childArena.ApplySingleMove(move, true);
        
        Console.WriteLine("===========================================================");
        Console.WriteLine("After Applying Move:");
        childArena.Me.GetSpans(out var body13, out var body23);
        Console.WriteLine($"Body of snake length: {childArena.Me.Length}, health: {childArena.Me.Health}, body1: {string.Join(",", body13.ToArray())}, body2: {string.Join(",", body23.ToArray())}");
        Console.WriteLine($"Snakes Bitboard: {string.Join(',', childArena.Grid.Snakes.GetRawData.ToArray())}");
        Console.WriteLine("===========================================================");
        
        // Il resto della logica rimane simile...
        var hash = ZobristHasher.CalculateHash(in childArena);
        
        var childIndex = _nodePool.GetNextIndex();
        ref var childNode = ref _nodePool[childIndex];
        childNode.Initialize(nodeIndex, move, hash);
        
        Console.WriteLine($"    └── Creato figlio {childIndex} per la mossa {MoveToString(move)}");
        
        if (lastChildIndex == -1)
        {
            node.FirstChildIndex = childIndex;
        }
        else
        {
            ref var lastChildNode = ref _nodePool[lastChildIndex];
            lastChildNode.NextSiblingIndex = childIndex;
        }
        
        lastChildIndex = childIndex;
    }
}

    // --- COMMENTO ---
    // 'Simulate' è corretto con 'ref', perché il suo scopo è proprio quello di
    // modificare uno stato fino a raggiungere un esito. La cosa importante è che
    // 'RunIteration' gli passi una COPIA dello stato su cui lavorare.
    private static void Simulate(ref WarArena arena)
    {
        const int turnLimit = 100;

        for (var i = 0; i < turnLimit; i++)
        {
            // --- Modifica: Controlla l'esito, non solo se sei morto ---
            if (arena.Outcome() != 0.0f) return;

            var legalMovesMask = arena.GetLegalMoves();
            if (legalMovesMask == 0) return;
        
            var move = RolloutMoveRandom(legalMovesMask); // Usiamo la policy casuale per semplicità
            arena.ApplySingleMove(move);
        }
    }
    
    // ... Gli altri metodi (SelectRolloutMove, RolloutMoveRandom, Backpropagate) possono rimanere invariati ...

    private static byte RolloutMoveRandom(byte legalMoves)
    {
        if (legalMoves == 0) return Moves.Up; // Fallback
        if (BitOperations.IsPow2(legalMoves)) return legalMoves;
        
        var count = BitOperations.PopCount(legalMoves);
        var randomIndex = Random.Shared.Next(count);

        byte move = 0;
        for (var i = 0; i <= randomIndex; i++)
        {
            move = (byte)(1 << BitOperations.TrailingZeroCount(legalMoves));
            legalMoves &= (byte)~move;
        }
        return move;
    }

    private void Backpropagate(int startNodeIndex, double rawScore)
    {
        const double scalingFactor = 100.0;
        var normalizedResult = Math.Tanh(rawScore / scalingFactor);

        var currentIndex = startNodeIndex;
        while (currentIndex != -1)
        {
            ref var currentNode = ref _nodePool[currentIndex];
            currentNode.UpdateStats(normalizedResult);
            currentIndex = currentNode.ParentIndex;
        }
    }
}