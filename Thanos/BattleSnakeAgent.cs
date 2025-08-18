using System;
using System.Collections.Generic;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.Memory;
using Thanos.SourceGen; // Assicurati di avere gli using corretti
using Thanos.War;

namespace Thanos;

public sealed class BattleSnakeAgent : IDisposable
{
    // Campi privati per la gestione dello stato e della logica
    private MemoryPool? _pool;
    private readonly MonteCarloEngine _engine;
    private GameContext _context;
    
    // Mappa per convertire gli ID stringa dei serpenti in interi performanti
    private readonly Dictionary<string, int> _snakeIdMap = new();
    private int _nextSnakeIntId = 0;
    
    // Puntatori per la gestione dell'albero MCTS
    private unsafe Node* _root;
    private unsafe Node* _previousChoiceNode; // Traccia il nodo scelto nel turno precedente

    public BattleSnakeAgent(int maxNodes = Constants.MaxNodes)
    {
        var worstContext = GameContext.Worst;
        _pool = new MemoryPool(worstContext, maxNodes);
        _engine = new MonteCarloEngine(_pool);
    }

    /// <summary>
    /// Chiamato una sola volta all'inizio della partita.
    /// Inizializza il contesto di gioco, la mappa degli ID e il memory pool.
    /// </summary>
    public unsafe void Start(in Request request)
    {
        // 1. Popola la mappa degli ID (o la pulisce se è una nuova partita)
        _snakeIdMap.Clear();
        _nextSnakeIntId = 0;
        foreach (var snake in request.Board.Snakes)
        {
            if (!_snakeIdMap.ContainsKey(snake.Id))
            {
                _snakeIdMap[snake.Id] = _nextSnakeIntId++;
            }
        }
    
        // 2. Crea il contesto specifico per questa partita
        _context = new GameContext(in request, _snakeIdMap);
    
        // 3. CONFIGURA il pool (già esistente) con il contesto della nuova partita
        _pool.Reset(in _context, _snakeIdMap);

        // 4. Crea il nodo radice
        if (_pool.TryGetNext(out var rootSlot))
        {
            rootSlot.InitializeFromRequest(in request);
            _root = rootSlot.GetNodePtr();
        }
    }

    /// <summary>
    /// Chiamato a ogni turno per decidere la mossa.
    /// Implementa la logica di riutilizzo dell'albero basata su hash.
    /// </summary>
    public unsafe byte Move(in Request request)
    {
        Node* newRoot = null;

        // --- FASE 1: RIUTILIZZO DELL'ALBERO ---
        if (_previousChoiceNode != null)
        {
            // Calcola l'hash dello stato REALE ricevuto dal server
            var requestHash = CalculateRequestHash(in request);

            // Cerca tra i "nipoti" (i figli della nostra scelta precedente) un nodo che corrisponda
            for (var i = 0; i < _previousChoiceNode->ChildrenCount; i++)
            {
                var childNode = (*_previousChoiceNode)[i];
                var childSlot = _pool!.GetSlotFromPointer(childNode);
                var childArena = childSlot.GetArena();
                
                if (childArena.GetStateHash == requestHash)
                {
                    newRoot = childNode; // Cache Hit! Trovato il nodo corrispondente.
                    break;
                }
            }
        }

        // --- FASE 2: GESTIONE "CACHE MISS" ---
        // Se non abbiamo trovato un nodo (o era il primo turno), creane uno nuovo
        if (newRoot == null)
        {
            if (_pool!.TryGetNext(out var rootSlot))
            {
                rootSlot.InitializeFromRequest(in request);
                newRoot = rootSlot.GetNodePtr();
            }
            else
            {
                // Emergenza: se non c'è memoria, vai su
                return Moves.Up;
            }
        }

        _root = newRoot;
        _root->Parent = null; // Il nuovo root non ha genitore

        // --- FASE 3: RICERCA MCTS ---
        var bestMoveByte = _engine.FindBestMove(_root, in _context, request.Game.Timeout);
        
        // --- FASE 4: PREPARAZIONE PER IL PROSSIMO TURNO ---
        // Memorizza il NODO corrispondente alla nostra mossa per il riutilizzo al prossimo turno
        _previousChoiceNode = _root->FindChildByMove(bestMoveByte);
        
        return bestMoveByte;
    }

    /// <summary>
    /// Helper per calcolare l'hash Zobrist di uno stato di gioco da una Request.
    /// Necessario per il confronto nel riutilizzo dell'albero.
    /// </summary>
    private long CalculateRequestHash(in Request request)
    {
        long hash = 0;
        foreach (var snake in request.Board.Snakes)
        {
            if (!_snakeIdMap.TryGetValue(snake.Id, out var snakeIntId)) continue;

            foreach (var bodyPart in snake.Body)
            {
                var coord1D = (ushort)(bodyPart.Y * _context.Width + bodyPart.X);
                hash ^= ZobristTable.GetSnakeValue(snakeIntId, coord1D);
            }
        }
        return hash;
    }

    public static void End(in Request request) => Console.WriteLine($"End: {request.Game.Id} - {request.Turn}");
    
    public void Dispose() => _pool?.Dispose();
}