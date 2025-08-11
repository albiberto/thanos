using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    // Riferimento al contesto condiviso e immutabile
    public readonly WarContext* Context;
    public uint CurrentLiveSnakes;
    
    // Stato mutevole
    private readonly byte* _snakesMemory;
    private readonly ulong* _fieldMemory;
    private readonly WarField _field;
    private fixed long _snakePointers[Constants.MaxSnakes];

    /// <summary>
    /// Costruttore Principale: crea lo stato iniziale dal DTO e dal contesto.
    /// </summary>
    public WarArena(in Request request, WarContext* context)
    {
        Context = context;
        CurrentLiveSnakes = Context->InitialActiveSnakes;
        ref readonly var board = ref request.Board;
        ref readonly var me = ref request.You;

        // 1. Alloca memoria usando le dimensioni pre-calcolate dal contesto
        _fieldMemory = (ulong*)NativeMemory.AlignedAlloc(Context->BitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(Context->SnakesMemorySize, Constants.CacheLineSize);
        
        // 2. Inizializzazione degli specialisti
        _field = new WarField(_fieldMemory, Context->Width, Context->Height, Context->Area);
        _field.InitializeStaticBoards(in board);
        
        InitializeSnakes(in me, in board, Context->Capacity);
    }

    /// <summary>
    /// Costruttore di Copia: duplica solo lo stato mutevole, condivide il contesto.
    /// </summary>
    public WarArena(in WarArena other)
    {
        // 1. Condividi il puntatore al contesto immutabile (nessun ricalcolo!)
        Context = other.Context;
        CurrentLiveSnakes = other.CurrentLiveSnakes;

        // 2. Alloca NUOVA memoria usando le dimensioni dal contesto
        _fieldMemory = (ulong*)NativeMemory.AlignedAlloc(Context->BitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(Context->SnakesMemorySize, Constants.CacheLineSize);

        // 3. Esegui la copia profonda dei dati mutevoli
        Buffer.MemoryCopy(other._fieldMemory, _fieldMemory, Context->BitboardsMemorySize, Context->BitboardsMemorySize);
        Buffer.MemoryCopy(other._snakesMemory, _snakesMemory, Context->SnakesMemorySize, Context->SnakesMemorySize);
        
        // 4. Inizializza il nuovo WarField con il nuovo puntatore
        _field = new WarField(_fieldMemory, Context->Width, Context->Height, Context->Area);

        // 5. Ricrea i puntatori per la nuova memoria
        for (var i = 0; i < Context->InitialActiveSnakes; i++)
        {
            _snakePointers[i] = (long)(_snakesMemory + i * Context->SnakeStride);
        }
    }
    
    private void InitializeSnakes(in Snake me, in Board board, int capacity)
    {
        InitializeSingleSnake(in me, 0, capacity);

        byte opponentIndex = 1;
        foreach (ref readonly var snakeData in board.Snakes.AsSpan())
        {
            if (snakeData.Id == me.Id) continue;
            InitializeSingleSnake(in snakeData, opponentIndex, capacity);
            opponentIndex++;
        }
    }
    
    private void InitializeSingleSnake(in Snake snakeDto, byte snakeIndex, int capacity)
    {
        var snakePtr = (WarSnake*)(_snakesMemory + snakeIndex * Context->SnakeStride);
        _snakePointers[snakeIndex] = (long)snakePtr;
        
        ref var warSnake = ref *snakePtr;
        warSnake.Initialize(in snakeDto, in _field, capacity);
    }

    public void Simulate(ReadOnlySpan<MoveDirection> moves)
    {
        
    }
    
    /// <summary>
    /// Valuta lo stato attuale della battaglia per determinare se è terminata.
    /// Restituisce l'esito e l'indice del vincitore, se presente.
    /// </summary>
    /// <returns>Una tupla con l'esito (Ongoing, Victory, Draw) e l'indice del vincitore (-1 se non applicabile).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (WarOutcome outcome, int winnerIndex) AssessOutcome()
    {
        // NON usiamo più un contatore locale, ma il campo della struct
        if (CurrentLiveSnakes == 1)
        {
            // Troviamo l'unico sopravvissuto
            for (var i = 0; i < Context->InitialActiveSnakes; i++)
            {
                if (!GetSnake(i).Dead)
                    return (WarOutcome.Victory, i);
            }
        }

        if (CurrentLiveSnakes == 0)
        {
            return (WarOutcome.Draw, -1);
        }

        return (WarOutcome.Ongoing, -1);
    }
    
    /// <summary>
    /// Calcola le mosse legali per un dato serpente.
    /// Una mossa è illegale se porta a una collisione immediata con un muro o un ostacolo.
    /// </summary>
    public void GetLegalMoves(int snakeIndex, List<MoveDirection> legalMoves)
    {
        legalMoves.Clear();
        ref var snake = ref GetSnake(snakeIndex);
        if (snake.Dead) return;

        var head = snake.Head;

        // Controlla le 4 direzioni
        var upPos = _field.GetNeighbor(head, MoveDirection.Up);
        if (!_field.IsOccupied(upPos)) legalMoves.Add(MoveDirection.Up);

        var downPos = _field.GetNeighbor(head, MoveDirection.Down);
        if (!_field.IsOccupied(downPos)) legalMoves.Add(MoveDirection.Down);

        var leftPos = _field.GetNeighbor(head, MoveDirection.Left);
        if (!_field.IsOccupied(leftPos)) legalMoves.Add(MoveDirection.Left);
    
        var rightPos = _field.GetNeighbor(head, MoveDirection.Right);
        if (!_field.IsOccupied(rightPos)) legalMoves.Add(MoveDirection.Right);
    }


    public ref WarSnake GetMySnake() => ref *(WarSnake*)_snakePointers[0];
    
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];
    
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
        if (Context != null) NativeMemory.AlignedFree(Context);
    }
}