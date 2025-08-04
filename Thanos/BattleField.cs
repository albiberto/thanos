using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
/// Manages the global context, memory, and collective operations on all game objects.
/// The struct itself is manually padded to be exactly 128 bytes (2 cache lines).
/// </summary>
/// <remarks>
/// --- CACHE-OPTIMIZED MANUAL LAYOUT (x64) ---
///
/// This struct uses Pack = 1 to disable automatic padding and implements manual
/// padding to ensure perfect alignment and size control. Total size: 128 bytes.
///
/// --- Cache Line 1 (0-63 bytes) ---
/// - Offset  0-1:   _boardWidth, _boardHeight   (2 bytes)
/// - Offset  2-5:   _maxBodyLength, _snakeStride (4 bytes)
/// - Offset  6:     _isInitialized             (1 byte)
/// - Offset  7:     _manualPadding1            (1 byte)  -> Aligns the following 8-byte pointer.
/// - Offset  8-15:  _memory                    (8 bytes) -> Pointer to snake data.
/// - Offset 16-25:  _collisionMatrix           (10 bytes) -> The embedded CollisionMatrix struct.
/// - Offset 26-63:  _manualPadding2            (38 bytes) -> Fills the rest of the first cache line.
///
/// --- Cache Line 2 (64-127 bytes) ---
/// - Offset 64-127: _snakePointers[8]          (64 bytes) -> Full cache line for direct snake pointers.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Battlefield : IDisposable
{
    private const int MaxSnakes = 8;
    private const int CacheLine = 64;
    private const int SnakeElementsPerCacheLine = CacheLine / sizeof(ushort);

    // === CACHE LINE 1 (0-63 bytes) ===
    private ushort _maxBodyLength;
    private ushort _snakeStride;
    private bool _isInitialized;
    private readonly byte _manualPadding1; // 1 byte padding for alignment
    private byte* _memory;
    private CollisionMatrix _collisionMatrix;
    private fixed byte _manualPadding2[38]; // 38 bytes padding to fill the cache line

    // === CACHE LINE 2 (64-127 bytes) ===
    private fixed long _snakePointers[MaxSnakes];

    /// <summary>
    /// Initializes the battlefield once with specific dimensions.
    /// </summary>
    public void Initialize(byte boardWidth, byte boardHeight)
    {
        if (_isInitialized) return;
        
        var boardArea = (ushort)(boardWidth * boardHeight);
        var desiredBodyLength = boardArea * 3 / 4;

        var maxBodyLength = (ushort)((desiredBodyLength + SnakeElementsPerCacheLine - 1) / SnakeElementsPerCacheLine * SnakeElementsPerCacheLine);
        if (maxBodyLength < SnakeElementsPerCacheLine) maxBodyLength = SnakeElementsPerCacheLine;
        if (maxBodyLength > 256) maxBodyLength = 256;
        _maxBodyLength = maxBodyLength;

        _snakeStride = (ushort)(BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort));

        var totalSnakeMemory = (nuint)(_snakeStride * MaxSnakes);
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, CacheLine);
        if (_memory == null) throw new OutOfMemoryException($"Failed to allocate {totalSnakeMemory} bytes for snakes");

        _collisionMatrix.Initialize(boardArea);

        _isInitialized = true;

        PrecalculatePointers();
        ResetAllSnakes();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrecalculatePointers()
    {
        for (var i = 0; i < MaxSnakes; i++)
        {
            _snakePointers[i] = (long)(_memory + i * _snakeStride);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(int index) => (BattleSnake*)_snakePointers[index];
    
    private void ResetAllSnakes()
    {
        if (!_isInitialized) return;
        var totalSnakeMemory = (nuint)(_snakeStride * MaxSnakes);
        Unsafe.InitBlock(_memory, 0, (uint)totalSnakeMemory);

        for (var i = 0; i < MaxSnakes; i++)
        {
            var snake = (BattleSnake*)_snakePointers[i];
            snake->Reset(_maxBodyLength);
        }
        _collisionMatrix.Clear();
    }

    /// <summary>
    /// Updates the CollisionMatrix by projecting the state of all active snakes.
    /// </summary>
    public void UpdateSnakePositions()
    {
        _collisionMatrix.Clear();
        fixed (Battlefield* thisPtr = &this)
        {
            _collisionMatrix.ProjectBattlefield(thisPtr, MaxSnakes);
        }
    }
    
    /// <summary>
    /// Applies hazard locations to the collision matrix. Call this after UpdateSnakePositions.
    /// </summary>
    public void ApplyHazards(ReadOnlySpan<ushort> hazardPositions)
    {
        // Usa un valore riservato per i pericoli
        const byte hazardId = byte.MaxValue; // 255
        _collisionMatrix.ApplyHazards(hazardPositions);        
    }

    /// <summary>
    /// Processes movements for all active snakes.
    /// </summary>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, ReadOnlySpan<CellContent> destinationContents, int hazardDamage)
    {
        for (var i = 0; i < MaxSnakes; i++)
        {
            var snakePtr = GetSnake(i);
            if (snakePtr->Health <= 0) continue;

            var bodyPtr = (ushort*)((byte*)snakePtr + BattleSnake.HeaderSize);
            snakePtr->Move(bodyPtr, newHeadPositions[i], destinationContents[i], hazardDamage);
        }
    }

    public void Dispose()
    {
        if (!_isInitialized) return;
        _collisionMatrix.Dispose();
        NativeMemory.AlignedFree(_memory);
        _memory = null;
        _isInitialized = false;
    }
}