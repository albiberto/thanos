using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
/// Battlefield manages the global context, memory allocation, and collective operations on snakes.
/// It also owns and manages the master CollisionMatrix.
/// </summary>
/// <remarks>
/// CACHE-OPTIMIZED MEMORY LAYOUT (x64):
/// 
/// The layout is designed to minimize cache misses during the most frequent operations.
/// Fields are ordered by access frequency and grouped by operation.
/// 
/// First cache line (0-63 bytes):
/// - Offset  0-3:  _boardWidth      (4 bytes)
/// - Offset  4-7:  _boardHeight     (4 bytes)  
/// - Offset  8-11: _maxBodyLength   (4 bytes)
/// - Offset 12-15: _snakeStride     (4 bytes)
/// - Offset 16-23: _totalMemory     (8 bytes)
/// - Offset 24-31: _memory          (8 bytes)
/// - Offset 32:    _isInitialized   (1 byte)
/// - Offset 33-39: _padding1        (7 bytes - alignment padding)
/// - Offset 40-63: _padding2        (24 bytes - unused space)
/// 
/// Second cache line (64-127 bytes):
/// - Offset 64-127: _snakePointers[8] (64 bytes - array of 8 long pointers)
/// 
/// Subsequent memory:
/// - The CollisionMatrix struct is placed after the primary cache-aligned fields.
/// 
/// TOTAL STRUCT SIZE: >128 bytes
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct Battlefield : IDisposable
{
    private const int MaxSnakes = 8; // Maximum number of managed snakes
    private const int CacheLine = 64; // Standard x64 cache line size
    private const int SnakeElementsPerCacheLine = CacheLine / sizeof(ushort); // 32 elements per cache line

    // === FIRST CACHE LINE (0-63 bytes) ===
    
    // Battlefield dimensions - frequently accessed during move validation
    private int _boardWidth;      // 4 bytes
    private int _boardHeight;     // 4 bytes  
    
    // Snake memory configuration - used to calculate offsets
    private int _maxBodyLength;   // 4 bytes
    private int _snakeStride;     // 4 bytes
    
    // Allocated memory management
    private nuint _totalMemory;   // 8 bytes (on x64)
    private byte* _memory;        // 8 bytes (on x64)
    
    // State flag
    private bool _isInitialized;  // 1 byte
    
    // Explicit padding to align next field to 8-byte boundary
    private fixed byte _padding1[7]; // 7 bytes
    
    // Explicit padding to fill the rest with the first cache line
    private fixed byte _padding2[24]; // 24 bytes
    
    // === SECOND CACHE LINE (64-127 bytes) ===
    
    // Array of direct pointers to snakes - optimizes sequential access
    // Each pointer is precalculated to avoid runtime multiplications
    private fixed long _snakePointers[MaxSnakes]; // 64 bytes (8 × 8)

    // === NUOVA SEZIONE: GESTIONE DELLA MATRICE DI COLLISIONE ===
    
    // La matrice di collisione è di proprietà e gestita dal Battlefield.
    private CollisionMatrix _collisionMatrix;
    
    /// <summary>
    /// Initializes or reinitializes the battlefield with specific dimensions.
    /// Allocates aligned memory for all snakes and the collision matrix.
    /// </summary>
    public void Initialize(int boardWidth, int boardHeight)
    {
        if (_isInitialized && _boardWidth == boardWidth && _boardHeight == boardHeight) return;
        
        // Free any previous memory allocation first
        if (_isInitialized) Dispose();

        // Calculate body length based on 3/4 of total area
        var boardArea = boardWidth * boardHeight;
        var desiredBodyLength = boardArea * 3 / 4;
        
        // Align length to cache line multiples for optimized access
        _maxBodyLength = (desiredBodyLength + SnakeElementsPerCacheLine - 1) / SnakeElementsPerCacheLine * SnakeElementsPerCacheLine;
            
        // Apply reasonable limits to avoid memory waste
        if (_maxBodyLength < SnakeElementsPerCacheLine) 
            _maxBodyLength = SnakeElementsPerCacheLine; // Minimum 1 cache line
        if (_maxBodyLength > 256) 
            _maxBodyLength = 256; // Maximum 8 cache lines
            
        // Calculate total stride per snake (header + aligned body)
        _snakeStride = BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort);
            
        // Allocate aligned memory for all snakes
        _totalMemory = (nuint)_snakeStride * MaxSnakes;
        _memory = (byte*)NativeMemory.AlignedAlloc(_totalMemory, CacheLine);
            
        if (_memory == null) 
            throw new OutOfMemoryException($"Failed to allocate {_totalMemory} bytes for snakes");
            
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        
        // Inizializza la matrice di collisione con le dimensioni corrette
        _collisionMatrix.Initialize(boardArea);

        _isInitialized = true;
            
        PrecalculatePointers();
        ResetAllSnakes();
    }

    /// <summary>
    /// Precalculates direct pointers to each snake.
    /// Avoids multiplications during access, improving performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrecalculatePointers()
    {
        // Store pointers directly as long values
        // This enables O(1) access without runtime calculations
        for (var i = 0; i < MaxSnakes; i++) 
        {
            _snakePointers[i] = (long)(_memory + i * _snakeStride);
        }
    }

    /// <summary>
    /// Gets a direct pointer to the specified snake.
    /// O(1) operation thanks to precalculated pointers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(int index)
    {
        // Single access to second cache line to retrieve pointer
        return (BattleSnake*)_snakePointers[index];
    }
    
    /// <summary>
    /// Initializes all snakes by zeroing memory and setting default values.
    /// Also clears the collision matrix.
    /// </summary>
    private void ResetAllSnakes()
    {
        if (!_isInitialized) return;
        
        // Zero entire memory block with a single efficient operation
        Unsafe.InitBlock(_memory, 0, (uint)_totalMemory);

        // Initialize each snake - this touches memory, loading it into cache
        for (var i = 0; i < MaxSnakes; i++)
        {
            var snake = (BattleSnake*)_snakePointers[i];
            snake->Reset(_maxBodyLength);
        }

        // Assicura che anche la matrice di collisione sia vuota
        _collisionMatrix.Clear();
    }
    
    /// <summary>
    /// Aggiorna la CollisionMatrix proiettando lo stato corrente di tutti i serpenti attivi.
    /// Questa operazione dovrebbe essere eseguita una volta per turno, prima di valutare le mosse.
    /// </summary>
    private void UpdateCollisionMatrix()
    {
        // Azzera lo stato precedente della matrice
        _collisionMatrix.Clear();

        // Per passare un puntatore a questa istanza di struct, dobbiamo usare 'fixed'
        fixed (Battlefield* thisPtr = &this)
        {
            // Proietta lo stato corrente sulla matrice
            _collisionMatrix.ProjectBattlefield(thisPtr, MaxSnakes);
        }
    }

    /// <summary>
    /// Processes movements for all active snakes in a single batch.
    /// Optimized to minimize cache misses by processing snakes sequentially.
    /// </summary>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, 
                               ReadOnlySpan<CellContent> destinationContents, 
                               int hazardDamage)
    {
        for (var i = 0; i < MaxSnakes; i++)
        {
            // Retrieve precalculated pointer from second cache line
            var snakeAddress = _snakePointers[i];

            // Empty slot - no snake allocated
            if (snakeAddress == 0) continue;
        
            // Direct cast to BattleSnake pointer
            var snakePtr = (BattleSnake*)snakeAddress;

            // Skip dead snakes to avoid unnecessary processing
            if (snakePtr->Health <= 0) continue;

            // Calculate body pointer by adding header offset
            // This pointer is cache-line aligned by design
            var bodyPtr = (ushort*)(snakeAddress + BattleSnake.HeaderSize);

            // Execute move with all precalculated data
            snakePtr->Move(bodyPtr, newHeadPositions[i], destinationContents[i], hazardDamage);
        }
    }
    
    /// <summary>
    /// Frees allocated memory for both snakes and the collision matrix.
    /// </summary>
    public void Dispose()
    {
        // Libera la memoria della matrice di collisione
        _collisionMatrix.Dispose();

        // Libera la memoria dei serpenti
        NativeMemory.AlignedFree(_memory);
        _memory = null;
        
        _isInitialized = false;
    }
}