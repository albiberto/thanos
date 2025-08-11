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
    private readonly WarContext* _context;
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
        _context = context;
        CurrentLiveSnakes = _context->InitialActiveSnakes;
        ref readonly var board = ref request.Board;
        ref readonly var me = ref request.You;

        // 1. Alloca memoria usando le dimensioni pre-calcolate dal contesto
        _fieldMemory = (ulong*)NativeMemory.AlignedAlloc(_context->BitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(_context->SnakesMemorySize, Constants.CacheLineSize);
        
        // 2. Inizializzazione degli specialisti
        _field = new WarField(_fieldMemory, _context->Width, _context->Area);
        _field.InitializeStaticBoards(in board);
        
        InitializeSnakes(in me, in board, _context->Capacity);
    }

    /// <summary>
    /// Costruttore di Copia: duplica solo lo stato mutevole, condivide il contesto.
    /// </summary>
    public WarArena(in WarArena other)
    {
        // 1. Condividi il puntatore al contesto immutabile (nessun ricalcolo!)
        _context = other._context;
        CurrentLiveSnakes = other.CurrentLiveSnakes;

        // 2. Alloca NUOVA memoria usando le dimensioni dal contesto
        _fieldMemory = (ulong*)NativeMemory.AlignedAlloc(_context->BitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory = (byte*)NativeMemory.AlignedAlloc(_context->SnakesMemorySize, Constants.CacheLineSize);

        // 3. Esegui la copia profonda dei dati mutevoli
        Buffer.MemoryCopy(other._fieldMemory, _fieldMemory, _context->BitboardsMemorySize, _context->BitboardsMemorySize);
        Buffer.MemoryCopy(other._snakesMemory, _snakesMemory, _context->SnakesMemorySize, _context->SnakesMemorySize);
        
        // 4. Inizializza il nuovo WarField con il nuovo puntatore
        _field = new WarField(_fieldMemory, _context->Width, _context->Area);

        // 5. Ricrea i puntatori per la nuova memoria
        for (var i = 0; i < _context->InitialActiveSnakes; i++)
        {
            _snakePointers[i] = (long)(_snakesMemory + i * _context->SnakeStride);
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
        var snakePtr = (WarSnake*)(_snakesMemory + snakeIndex * _context->SnakeStride);
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
            for (var i = 0; i < _context->InitialActiveSnakes; i++)
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


    public ref WarSnake GetMySnake() => ref *(WarSnake*)_snakePointers[0];
    
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];
    
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
        if (_context != null) NativeMemory.AlignedFree(_context);
    }
}