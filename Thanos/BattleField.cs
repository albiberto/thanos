using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

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
        // Controlla se le dimensioni sono cambiate
        bool dimensionsChanged = !_isInitialized || _boardWidth != boardWidth || _boardHeight != boardHeight;
        
        if (dimensionsChanged)
        {
            // Calcola la lunghezza massima del corpo basata sui 3/4 dell'area.
            int boardArea = boardWidth * boardHeight;
            int desiredBodyLength = boardArea * 3 / 4;
            
            // Arrotonda la lunghezza del body al multiplo di 64 byte più vicino
            // Ogni elemento del body è un ushort (2 byte), quindi 32 elementi = 64 byte
            const int elementsPerCacheLine = CacheLine / sizeof(ushort); // 32 elementi
            _maxBodyLength = (desiredBodyLength + elementsPerCacheLine - 1) / elementsPerCacheLine * elementsPerCacheLine;
            
            // Applica i limiti minimo e massimo DOPO l'arrotondamento
            if (_maxBodyLength < elementsPerCacheLine) _maxBodyLength = elementsPerCacheLine; // min 32 elementi (1 cache line)
            if (_maxBodyLength > 256) _maxBodyLength = 256; // max 256 elementi (8 cache line)
            
            // Lo stride totale include header (64 byte) + body allineato
            _snakeStride = BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort);
            
            // Se già inizializzato, libera la vecchia memoria
            if (_isInitialized) Dispose();
            
            // Alloca la nuova memoria
            _totalMemory = (nuint)_snakeStride * MaxSnakes;
            _memory = (byte*)NativeMemory.AlignedAlloc(_totalMemory, CacheLine);
            
            if (_memory == null) throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes");
            
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
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
            GetSnake(i)->Reset(); // Passa la lunghezza massima calcolata
        }
    }

    /// <summary>
    /// Resetta un singolo serpente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetSnake(int index)
    {
        if (!_isInitialized || index < 0 || index >= MaxSnakes) return;
        
        // Azzera la memoria del serpente specifico
        byte* snakeMemory = _memory + index * _snakeStride;
        Unsafe.InitBlock(snakeMemory, 0, (uint)_snakeStride);
        
        // Inizializza con i valori di default
        GetSnake(index)->Reset(_maxBodyLength);
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
            if (snake->Health > 0)
            {
                // Chiama la nuova versione di Move, passando la posizione e il contenuto della cella.
                snake->Move(newHeadPositions[i], destinationContents[i]);
            }
        }
    }

    public int BoardWidth => _boardWidth;
    public int BoardHeight => _boardHeight;
    public int MaxBodyLength => _maxBodyLength;
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