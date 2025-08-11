using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena : IDisposable
{
    private readonly uint _activeSnakes;

    private readonly byte* _snakesMemory;
    private readonly ulong* _fieldMemory;

    private readonly int _snakeSize;

    private fixed long _snakePointers[Constants.MaxSnakes];
    private readonly WarField _field;

    public WarArena(in Request request)
    {
        ref readonly var board = ref request.Board;
        
        var width = board.Width;
        var area = board.Area;
        var capacity = board.Capacity;
        
        _activeSnakes = (uint)board.Snakes.Length;
        
        // --- Calcolo Memoria per i Bitboard ---
        var bitboardSize = (area + 63) / 64 * sizeof(ulong);
        var bitboardsMemorySize = bitboardSize * WarField.TotalBitboards;
        
        // --- Calcolo Memoria per i Serpenti ---
        var snakeSize = WarSnake.HeaderSize + capacity * sizeof(ushort);
        var snakesMemorySize = snakeSize * _activeSnakes;

        // --- Allochiamo la memoria allineata ---
        _fieldMemory =  (ulong*)NativeMemory.AlignedAlloc(bitboardsMemorySize, Constants.CacheLineSize);
        _snakesMemory =  (byte*)NativeMemory.AlignedAlloc(snakesMemorySize, Constants.CacheLineSize);  
        
        // --- Inizializziamo la plancia di gioco (WarField) con cibo e hazards ---
        _field = new WarField(_fieldMemory, width, bitboardsMemorySize);
        _field.AddFood(board.Food);
        _field.AddHazard(board.Hazards);
        
        // --- Inizializziamo i serpenti sia sulla plancia di gioco (WarField) che nella struttura dedicata alla loro gestione (WarSnake) ---
        InitializeSnakes(in request.You, board.Snakes, (int)capacity, snakeSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeSnakes(in Snake me, ReadOnlySpan<Snake> snakes, int capacity, uint snakeSize)
    {
        // Inizializza il tuo serpente a indice 0 (Me)
        InitializeSnake(me, 0, capacity);

        // Inizializza gli avversari a partire da indice 1 (Enemies)
        var myId = me.Id;
        byte opponentPointerIndex = 1;
        
        foreach (ref readonly var snake in snakes)
        {
            if (snake.Id == myId) continue;
        
            InitializeSnake(snake, opponentPointerIndex, capacity);
            opponentPointerIndex++;
        }
    }
    
    private void InitializeSnake(in Snake snakeDto, byte snakeIndex, int capacity)
    {
        var length = Math.Min(snakeDto.Length, capacity);
        var sourceBody = snakeDto.Body.AsSpan();

        // 1. Ottieni il puntatore alla destinazione finale del WarSnake e inizializza i campi.
        var snakePtr = (WarSnake*)(_snakesMemory + snakeIndex * _snakeSize);
        _snakePointers[snakeIndex] = (long)snakePtr;

        snakePtr->Health = snakeDto.Health;
        snakePtr->Length = length;
        snakePtr->Capacity = capacity;
        snakePtr->TailIndex = 0;
        snakePtr->NextHeadIndex = length & (capacity - 1);

        // 2. Esegui un UNICO CICLO per convertire le coordinate e popolarle in TUTTE le destinazioni.
        for (var i = 0; i < length; i++)
        {
            // Converte la coordinata 2D in 1D UNA SOLA VOLTA.
            // Leggiamo dall'array originale in ordine inverso.
            var coord1D = _field.To1D(sourceBody[length - 1 - i]);

            // a) Scrive direttamente nella memoria del corpo del WarSnake.
            // Scriviamo in ordine Coda -> Testa.
            snakePtr->Body[i] = coord1D;

            // b) Usa lo stesso valore per accendere il bit nel WarField.
            _field.SetSnakeBit(coord1D);
        }

        // 3. Imposta la testa del serpente, che è l'ultimo elemento che abbiamo scritto.
        if (length > 0)
        {
            snakePtr->Head = snakePtr->Body[length - 1];
        }
        else
        {
            // Gestisce il caso raro di un serpente di lunghezza 0
            snakePtr->Head = ushort.MaxValue; // O un altro valore sentinella
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetMe() => ref *(WarSnake*)_snakePointers[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref WarSnake GetSnake(int index) => ref *(WarSnake*)_snakePointers[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_snakesMemory != null) NativeMemory.AlignedFree(_snakesMemory);
        if (_fieldMemory != null) NativeMemory.AlignedFree(_fieldMemory);
    }
}