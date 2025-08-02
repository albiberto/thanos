using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
/// Battlefield gestisce il contesto globale, la memoria e le operazioni collettive sui serpenti.
/// Non conosce la logica specifica della griglia di gioco (cibo, ostacoli).
/// </summary>
/// <remarks>
/// LAYOUT MEMORIA OTTIMIZZATO PER CACHE (x64):
/// 
/// Campi value type (struct inline):
/// - _boardWidth:        4 byte  (int)
/// - _boardHeight:       4 byte  (int)
/// - _maxBodyLength:     4 byte  (int)
/// - _snakeStride:       4 byte  (int)
/// - _totalMemory:       8 byte  (nuint su x64)
/// - _memory:            8 byte  (puntatore su x64)
/// - _isInitialized:     1 byte  (bool)
/// - [padding]:          7 byte  (allineamento a 8 byte)
/// - _snakePointers:    64 byte  (fixed array di 8 long)
/// 
/// TOTALE: 104 byte (2 cache line da 64 byte ciascuna)
/// 
/// Prima cache line (64 byte):
/// - Tutti i campi fino a _snakePointers
/// 
/// Seconda cache line (64 byte):
/// - Array _snakePointers completo
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct Battlefield : IDisposable
{
    private const int MaxSnakes = 8;
    private const int CacheLine = 64; // Allineamento per la cache line

    // === PRIMA CACHE LINE (0-63 byte) ===
    
    // Configurazione del battlefield - 16 byte totali
    private int _boardWidth;      // 4 byte - offset 0
    private int _boardHeight;     // 4 byte - offset 4  
    private int _maxBodyLength;   // 4 byte - offset 8
    private int _snakeStride;     // 4 byte - offset 12
    
    // Memoria allocata - 8 byte
    private nuint _totalMemory;   // 8 byte - offset 16 (su x64)
    
    // Puntatore alla memoria - 8 byte
    private byte* _memory;        // 8 byte - offset 24 (su x64)
    
    // Flag di inizializzazione - 1 byte + 7 padding
    private bool _isInitialized;  // 1 byte - offset 32
    // [padding automatico di 7 byte per allineare il prossimo campo a 8 byte]
    
    // === SECONDA CACHE LINE (64-127 byte) ===
    
    // Array di puntatori precalcolati ai serpenti
    // Su x64: 8 puntatori × 8 byte = 64 byte (esattamente 1 cache line)
    private fixed long _snakePointers[MaxSnakes]; // 64 byte - offset 40
    
    // TOTALE STRUCT: 104 byte (40 + 64)
    
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
            
            // Precalcola i puntatori ai serpenti
            PrecalculatePointers();
            
            // Inizializza tutti i serpenti al loro stato di default.
            ResetAllSnakes();
        }
    }

    /// <summary>
    /// Precalcola e memorizza i puntatori a tutti i serpenti.
    /// Questa operazione tocca la seconda cache line della struct.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrecalculatePointers()
    {
        for (int i = 0; i < MaxSnakes; i++)
        {
            _snakePointers[i] = (long)(_memory + i * _snakeStride);
        }
    }

    /// <summary>
    /// Ottiene un puntatore a un serpente specifico usando il suo indice.
    /// Accede solo alla seconda cache line (offset 64+).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(int index)
    {
        // Accesso diretto al puntatore precalcolato, senza moltiplicazioni
        return (BattleSnake*)_snakePointers[index];
    }

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
            BattleSnake* snake = (BattleSnake*)_snakePointers[i];
            snake->Reset(_maxBodyLength);
        }
    }

    /// <summary>
    /// Resetta un singolo serpente.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetSnake(int index)
    {
        if (!_isInitialized || index < 0 || index >= MaxSnakes) return;
        
        // Usa il puntatore precalcolato invece di calcolare l'offset
        BattleSnake* snake = (BattleSnake*)_snakePointers[index];
        
        // Azzera la memoria del serpente specifico
        Unsafe.InitBlock((byte*)snake, 0, (uint)_snakeStride);
        
        // Inizializza con i valori di default
        snake->Reset(_maxBodyLength);
    }

    /// <summary>
    /// Processa i movimenti per tutti i serpenti attivi.
    /// Accede principalmente alla seconda cache line per i puntatori.
    /// </summary>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, ReadOnlySpan<CellContent> destinationContents)
    {
        // Assicura che gli span abbiano la stessa lunghezza per evitare errori.
        int count = Math.Min(MaxSnakes, Math.Min(newHeadPositions.Length, destinationContents.Length));

        for (int i = 0; i < count; i++)
        {
            // Accesso diretto al puntatore precalcolato (seconda cache line)
            BattleSnake* snake = (BattleSnake*)_snakePointers[i];

            // Procede solo se il serpente è vivo.
            if (snake->Health > 0)
            {
                // Chiama la nuova versione di Move, passando la posizione e il contenuto della cella.
                snake->Move(newHeadPositions[i], destinationContents[i]);
            }
        }
    }

    public void Dispose()
    {
        if (_isInitialized && _memory != null)
        {
            NativeMemory.AlignedFree(_memory);
            _memory = null;
            _isInitialized = false;
            
            // Opzionale: azzera anche i puntatori per sicurezza
            for (int i = 0; i < MaxSnakes; i++)
            {
                _snakePointers[i] = 0;
            }
        }
    }
}