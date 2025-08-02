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
    private const int MaxSnakes = 8; // Numero massimo di serpenti gestiti (me + 7 avversari)
    private const int CacheLine = 64; // Dimensioni della cache line
    private const int SnakeElementsPErCacheLine = CacheLine / sizeof(ushort); // Ogni elemento del body è un ushort (2 byte), quindi 32 elementi = 64 byte

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
        if (_isInitialized && _boardWidth == boardWidth && _boardHeight == boardHeight) return;
        
        // Calcola la lunghezza massima del corpo basata sui 3/4 dell'area.
        var boardArea = boardWidth * boardHeight;
        var desiredBodyLength = boardArea * 3 / 4;
        
        // Arrotonda la lunghezza del body al multiplo di 64 byte più vicino
        _maxBodyLength = (desiredBodyLength + SnakeElementsPErCacheLine - 1) / SnakeElementsPErCacheLine * SnakeElementsPErCacheLine;
            
        // Applica i limiti minimo e massimo DOPO l'arrotondamento
        // TODO: Forse esiste un modo migliore per calcolare il body length?
        if (_maxBodyLength < SnakeElementsPErCacheLine) _maxBodyLength = SnakeElementsPErCacheLine; // min 32 elementi (1 cache line)
        if (_maxBodyLength > 256) _maxBodyLength = 256; // max 256 elementi (8 cache line)
            
        // Lo stride totale include header (64 byte) + body allineato
        _snakeStride = BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort);
            
        // Se già inizializzato, libera la vecchia memoria
        if (_isInitialized) Dispose();
            
        // Alloca la nuova memoria
        // TODO: Considera l'uso di un allocatore personalizzato per ottimizzare ulteriormente
        // TODO: Valuta se manca header di BattleField + Array di puntatori
        _totalMemory = (nuint)_snakeStride * MaxSnakes;
        _memory = (byte*)NativeMemory.AlignedAlloc(_totalMemory, CacheLine);
            
        if (_memory == null) throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes");
            
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        _isInitialized = true;
            
        PrecalculatePointers(); // Precalcola i puntatori ai serpenti 
            
        ResetAllSnakes(); // Inizializza tutti i serpenti al loro stato di default.
    }

    /// <summary>
    /// Precalcola e memorizza i puntatori a tutti i serpenti.
    /// Questa operazione tocca la seconda cache line della struct.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrecalculatePointers()
    {
        // TODO: nell array credo che vengano memorizzati gli offset e non i puntatori diretti. Si potrebbe salvare direttamente il puntatore?
        for (var i = 0; i < MaxSnakes; i++) _snakePointers[i] = (long)(_memory + i * _snakeStride);
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
    private void ResetAllSnakes()
    {
        if (!_isInitialized) return;
        
        // 1. Azzera l'intero blocco di memoria in modo efficiente.
        Unsafe.InitBlock(_memory, 0, (uint)_totalMemory);

        // 2. Chiama il metodo Reset per ogni serpente per inizializzare i suoi valori di default.
        // Questa operazione tocca anche la memoria, aiutando a caricarla in cache (pre-warming).
        for (var i = 0; i < MaxSnakes; i++)
        {
            var snake = (BattleSnake*)_snakePointers[i];
            snake->Reset(_maxBodyLength);
        }
    }
    
    /// <summary>
    /// Processa i movimenti per tutti i serpenti attivi.
    /// I dati di input (posizioni, contenuti) devono essere allineati per indice con i serpenti.
    /// </summary>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, ReadOnlySpan<CellContent> destinationContents, int hazardDamage)
    {
        for (var i = 0; i < MaxSnakes; i++)
        {
            // Ottiene l'indirizzo del serpente come valore numerico (long).
            var snakeAddress = _snakePointers[i];

            // Se l'indirizzo è 0, lo slot non è usato.
            if (snakeAddress == 0) continue;
        
            // Converte l'indirizzo in un puntatore per leggere la vita.
            var snakePtr = (BattleSnake*)snakeAddress;

            // Salta i serpenti già morti.
            if (snakePtr->Health <= 0) continue;

            // Prende l'indirizzo base (long), aggiunge l'offset (64) e fa il cast del risultato a puntatore.
            var bodyPtr = (ushort*)(snakeAddress + BattleSnake.HeaderSize);

            // Esegue la mossa passando il puntatore al corpo precalcolato.
            snakePtr->Move(bodyPtr, newHeadPositions[i], destinationContents[i], hazardDamage);
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
            // TODO: Valuta se è necessario?
            for (var i = 0; i < MaxSnakes; i++)
            {
                _snakePointers[i] = 0;
            }
        }
    }
}