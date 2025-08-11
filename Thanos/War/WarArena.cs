using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.SourceGen;

namespace Thanos.War;

/// <summary>
/// Rappresenta una singola istanza simulabile dello stato di gioco (un "universo parallelo").
/// Questa struct è progettata per essere estremamente efficiente, utilizzando memoria non gestita
/// e operazioni a basso livello per minimizzare l'overhead.
/// Ogni WarArena gestisce la propria memoria interna per lo stato dei serpenti e della plancia.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    // --- Campi Pubblici ---

    /// <summary>
    /// Puntatore al contesto di gioco immutabile, condiviso tra tutte le arene di una ricerca.
    /// </summary>
    public readonly WarContext* Context;

    /// <summary>
    /// Il numero di serpenti ancora in gioco in questa arena.
    /// </summary>
    public uint CurrentLiveSnakes;

    // --- Stato Interno (Puntatori e Dati) ---
    private byte* _snakesMemory;
    private ulong* _fieldMemory;
    private WarField _field;
    private fixed long _snakePointers[Constants.MaxSnakes];

    // -------------------------------------------------------------------
    // --- COSTRUTTORI E GESTIONE MEMORIA ---
    // -------------------------------------------------------------------

    /// <summary>
    /// Costruttore Principale. Usato solo per creare l'arena "radice" all'inizio del turno
    /// a partire dai dati della richiesta JSON.
    /// </summary>
    public WarArena(in Request request, WarContext* context)
    {
        Context = context;
        CurrentLiveSnakes = Context->WarSnakeCount;
        ref readonly var board = ref request.Board;

        // Alloca la memoria non gestita per questa arena.
        _fieldMemory = (ulong*)NativeMemory.AlignedAlloc(Context->BitboardsMemorySize, Constants.SizeOfCacheLine);
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(Context->SnakesMemorySize, Constants.SizeOfCacheLine);

        // Inizializza gli specialisti e lo stato iniziale.
        _field = new WarField(_fieldMemory, Context->Width, Context->Height, Context->Area);
        _field.InitializeStaticBoards(in board);
        
        InitializeAllSnakes(in request.You, in board, Context->SnakeBodyCapacity);
    }

    /// <summary>
    /// Crea una copia profonda (clone) di questa arena in una locazione di memoria di destinazione.
    /// Questo è il metodo chiave per l'esplorazione MCTS.
    /// </summary>
    /// <param name="destination">Un puntatore a una `WarArena` la cui memoria è già stata allocata (tipicamente da un MemoryPool).</param>
    public void CloneTo(WarArena* destination)
    {
        // 1. Copia i campi semplici e il puntatore al contesto condiviso.
        destination->CurrentLiveSnakes = CurrentLiveSnakes;

        // 2. Alloca NUOVA memoria per i dati interni (bitboard e serpenti) dell'arena di destinazione.
        // Ogni arena clonata possiede e gestisce la propria memoria per lo stato di gioco.
        var newFieldMem = (ulong*)NativeMemory.AlignedAlloc(Context->BitboardsMemorySize, Constants.SizeOfCacheLine);
        var newSnakesMem = (byte*)NativeMemory.AlignedAlloc(Context->SnakesMemorySize, Constants.SizeOfCacheLine);
    
        // 3. Esegui una copia profonda dei dati dalla sorgente (this) alla nuova memoria.
        Buffer.MemoryCopy(_fieldMemory, newFieldMem, Context->BitboardsMemorySize, Context->BitboardsMemorySize);
        Buffer.MemoryCopy(_snakesMemory, newSnakesMem, Context->SnakesMemorySize, Context->SnakesMemorySize);
    
        // 4. Inizializza la struct di destinazione per usare la nuova memoria.
        destination->InitializeFromPointers(newFieldMem, newSnakesMem);
    }
    
    /// <summary>
    /// Rilascia la memoria non gestita (_fieldMemory e _snakesMemory) posseduta da questa arena.
    /// Se questa è l'arena radice, libera anche il contesto condiviso.
    /// </summary>
    public void Dispose()
    {
        // Libera la memoria specifica di questa istanza.
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
        
        // Il contesto viene liberato solo dall'arena radice per evitare deallocazioni multiple.
        // Un modo per gestirlo è controllare se il puntatore non è nullo e poi "nullarlo"
        // nelle copie, ma la gestione esterna (come nel codice di esempio precedente) è più sicura.
        // Assumiamo che il chiamante gestisca la vita del contesto.
    }
    
    // -------------------------------------------------------------------
    // --- LOGICA DI GIOCO E SIMULAZIONE ---
    // -------------------------------------------------------------------

    /// <summary>
    /// Simula un singolo turno di gioco, applicando le mosse fornite a tutti i serpenti.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Simulate(ReadOnlySpan<MoveDirection> moves)
    {
        var initialSnakes = Context->WarSnakeCount;
        
        // Usa lo stack per buffer temporanei per evitare allocazioni sull'heap.
        var newHeads = stackalloc ushort[(int)initialSnakes];
        var oldTails = stackalloc ushort[(int)initialSnakes];
        var hasEaten = stackalloc bool[(int)initialSnakes];
        var isDead = stackalloc bool[(int)initialSnakes];

        // --- PASSAGGIO 1: DETERMINAZIONE (Calcola gli esiti senza modificare lo stato) ---
        DetermineOutcomes(moves, newHeads, oldTails, hasEaten, isDead);

        // --- PASSAGGIO 2: APPLICAZIONE (Modifica lo stato in base agli esiti calcolati) ---
        ApplyOutcomes(newHeads, oldTails, hasEaten, isDead);
    }
    
    /// <summary>
    /// Valuta lo stato attuale della battaglia per determinare se è terminata.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (WarOutcome outcome, int winnerIndex) AssessOutcome()
    {
        if (CurrentLiveSnakes <= 1)
        {
            if (CurrentLiveSnakes == 1)
            {
                for (var i = 0; i < Context->WarSnakeCount; i++)
                {
                    if (!GetSnake(i)->Dead)
                        return (WarOutcome.Victory, i);
                }
            }
            return (WarOutcome.Draw, -1);
        }
        return (WarOutcome.Ongoing, -1);
    }
    
    /// <summary>
    /// Riempie una lista con le mosse legali per un dato serpente.
    /// </summary>
    public void GetLegalMoves(int snakeIndex, List<MoveDirection> legalMoves)
    {
        legalMoves.Clear();
        var snakePtr = GetSnake(snakeIndex);
        if (snakePtr->Dead) return;

        var head = snakePtr->Head;

        // Controlla le 4 direzioni cardinali
        TryAddMove(head, MoveDirection.Up, legalMoves);
        TryAddMove(head, MoveDirection.Down, legalMoves);
        TryAddMove(head, MoveDirection.Left, legalMoves);
        TryAddMove(head, MoveDirection.Right, legalMoves);
    }
    
    /// <summary>
    /// Ottiene un puntatore a un serpente specifico tramite il suo indice.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WarSnake* GetSnake(int index) => (WarSnake*)_snakePointers[index];

    // -------------------------------------------------------------------
    // --- METODI HELPER PRIVATI ---
    // -------------------------------------------------------------------

    private void DetermineOutcomes(ReadOnlySpan<MoveDirection> moves, ushort* newHeads, ushort* oldTails, bool* hasEaten, bool* isDead)
    {
        var initialSnakes = Context->WarSnakeCount;

        // Calcola le posizioni future e inizializza gli stati
        for (var i = 0; i < initialSnakes; i++)
        {
            var snake = GetSnake(i);
            if (snake->Dead) { isDead[i] = true; continue; }

            isDead[i] = false;
            hasEaten[i] = false;
            oldTails[i] = snake->GetTailPosition();
            newHeads[i] = _field.GetNeighbor(snake->Head, moves[i]);
        }

        // Rileva collisioni e morti
        for (var i = 0; i < initialSnakes; i++)
        {
            if (isDead[i]) continue;
            var head = newHeads[i];

            // Morte per fame, collisione con ostacoli, o collisione testa-a-testa
            if ((GetSnake(i)->Health <= 1 && !_field.IsFood(head)) || _field.IsOccupied(head))
            {
                isDead[i] = true;
                continue;
            }

            for (var j = i + 1; j < initialSnakes; j++)
            {
                if (!isDead[j] && head == newHeads[j])
                {
                    if (GetSnake(i)->Length <= GetSnake(j)->Length) isDead[i] = true;
                    if (GetSnake(j)->Length <= GetSnake(i)->Length) isDead[j] = true;
                }
            }
            if (!isDead[i] && _field.IsFood(head)) hasEaten[i] = true;
        }
    }
    
    private void ApplyOutcomes(ushort* newHeads, ushort* oldTails, bool* hasEaten, bool* isDead)
    {
        for (var i = 0; i < Context->WarSnakeCount; i++)
        {
            var snake = GetSnake(i);
            if (snake->Dead) continue; // Salta serpenti già morti all'inizio

            if (isDead[i])
            {
                snake->Kill();
                _field.RemoveSnake(snake->Body, snake->Length);
                CurrentLiveSnakes--;
            }
            else
            {
                snake->Move(newHeads[i], hasEaten[i]);
                _field.UpdateSnakePosition(oldTails[i], newHeads[i], hasEaten[i]);
            }
        }
    }

    private void InitializeAllSnakes(in Snake me, in Board board, int capacity)
    {
        InitializeSingleSnake(in me, 0, capacity);
        byte opponentIndex = 1;
        foreach (ref readonly var snakeData in board.Snakes.AsSpan())
        {
            if (snakeData.Id == me.Id) continue;
            InitializeSingleSnake(in snakeData, opponentIndex++, capacity);
        }
    }
    
    private void InitializeSingleSnake(in Snake snakeDto, byte snakeIndex, int capacity)
    {
        var snakePtr = (WarSnake*)(_snakesMemory + snakeIndex * Context->SnakeStride);
        _snakePointers[snakeIndex] = (long)snakePtr;
        snakePtr->Initialize(in snakeDto, in _field, capacity);
    }
    
    private void TryAddMove(ushort head, MoveDirection direction, List<MoveDirection> legalMoves)
    {
        var neighborPos = _field.GetNeighbor(head, direction);
        if (!_field.IsOccupied(neighborPos))
        {
            legalMoves.Add(direction);
        }
    }

    /// <summary>
    /// Metodo helper per inizializzare una struct WarArena usando puntatori a memoria pre-allocata.
    /// </summary>
    private void InitializeFromPointers(ulong* fieldMemory, byte* snakesMemory)
    {
        _fieldMemory = fieldMemory;
        _snakesMemory = snakesMemory;
        _field = new WarField(_fieldMemory, Context->Width, Context->Height, Context->Area);

        for (var i = 0; i < Context->WarSnakeCount; i++)
        {
            _snakePointers[i] = (long)(_snakesMemory + i * Context->SnakeStride);
        }
    }
}