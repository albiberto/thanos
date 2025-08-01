using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks; // Assicurati che CellContent sia accessibile

namespace Thanos;

/// <summary>
/// Battlefield gestisce il contesto globale, la memoria e le operazioni collettive sui serpenti.
/// Non conosce la logica specifica della griglia di gioco (cibo, ostacoli).
/// </summary>
public unsafe struct Battlefield : IDisposable
{
    private const int MaxSnakes = 8;
    private const int CacheLine = 64; // Allineamento per la cache line

    // Configurazione del battlefield
    private int _boardWidth;
    private int _boardHeight;
    private int _maxBodyLength;
    private int _snakeStride; // Dimensione in byte per ogni slot di serpente, allineata
    private nuint _totalMemory;

    // Memoria
    private byte* _memory;
    private bool _isInitialized;

    /// <summary>
    /// Inizializza o reinizializza il battlefield con dimensioni specifiche.
    /// Alloca la memoria necessaria per tutti i serpenti.
    /// </summary>
    public void Initialize(int boardWidth, int boardHeight)
    {
        // Calcola la lunghezza massima del corpo basata sull'area, con un minimo e un massimo.
        int boardArea = boardWidth * boardHeight;
        _maxBodyLength = boardArea / 2;
        if (_maxBodyLength < 32) _maxBodyLength = 32;
        if (_maxBodyLength > 256) _maxBodyLength = 256;

        // Calcola lo "stride": lo spazio totale occupato da un serpente, allineato alla cache line.
        // Questo garantisce che ogni serpente inizi su un nuovo blocco di cache.
        int snakeSize = BattleSnake.HeaderSize + (_maxBodyLength * sizeof(ushort));
        _snakeStride = (snakeSize + CacheLine - 1) & ~(CacheLine - 1);

        // Se le dimensioni cambiano, libera la vecchia memoria.
        if (_isInitialized && (_boardWidth != boardWidth || _boardHeight != boardHeight))
        {
            Dispose();
        }

        _boardWidth = boardWidth;
        _boardHeight = boardHeight;

        if (!_isInitialized)
        {
            _totalMemory = (nuint)_snakeStride * MaxSnakes;
            _memory = (byte*)NativeMemory.AlignedAlloc(_totalMemory, CacheLine);

            if (_memory == null) throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes");

            _isInitialized = true;
            
            // Inizializza tutti i serpenti al loro stato di default.
            ResetAllSnakes();
        }
    }

    /// <summary>
    /// Ottiene un puntatore a un serpente specifico usando il suo indice.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(int index) => (BattleSnake*)(_memory + index * _snakeStride);

    /// <summary>
    /// Azzera la memoria e imposta tutti i serpenti al loro stato iniziale.
    /// </summary>
    public void ResetAllSnakes()
    {
        if (!_isInitialized) return;
        
        // 1. Azzera l'intero blocco di memoria in modo efficiente.
        Unsafe.InitBlock(_memory, 0, (uint)_totalMemory);

        // 2. Chiama il metodo Reset per ogni serpente per inizializzare i suoi valori di default.
        // Questa operazione tocca anche la memoria, aiutando a caricarla in cache (pre-warming).
        for (int i = 0; i < MaxSnakes; i++)
        {
            GetSnake(i)->Reset(); // Utilizza il nuovo metodo Reset di BattleSnake
        }
    }

    /// <summary>
    /// Processa i movimenti per tutti i serpenti attivi.
    /// Richiede la nuova posizione della testa e il contenuto della cella di destinazione per ogni mossa.
    /// </summary>
    /// <param name="newHeadPositions">Le coordinate delle nuove teste per ogni serpente.</param>
    /// <param name="destinationContents">Il contenuto della cella per ogni mossa corrispondente.</param>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, ReadOnlySpan<CellContent> destinationContents)
    {
        // Assicura che gli span abbiano la stessa lunghezza per evitare errori.
        int count = Math.Min(MaxSnakes, Math.Min(newHeadPositions.Length, destinationContents.Length));

        for (int i = 0; i < count; i++)
        {
            BattleSnake* snake = GetSnake(i);

            // Procede solo se il serpente è vivo.
            // Sostituisce il vecchio metodo IsAlive() con un controllo diretto sulla vita.
            if (snake->Health > 0)
            {
                // Chiama la nuova versione di Move, passando la posizione e il contenuto della cella.
                snake->Move(newHeadPositions[i], destinationContents[i]);
            }
        }
    }

    public int BoardWidth => _boardWidth;
    public int BoardHeight => _boardHeight;
    public int SnakeCount => MaxSnakes;

    public void Dispose()
    {
        if (_isInitialized && _memory != null)
        {
            NativeMemory.AlignedFree(_memory);
            _memory = null;
            _isInitialized = false;
        }
    }
}