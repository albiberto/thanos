using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Manages the game's global context and all snake entities.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Tesla : IDisposable
{
    private const int HeaderSize = 64; // 1 cache line
    
    // === CACHE LINE 1 - HEADER + BATTLEFIELD ===
    private bool _isInitialized;    // 1byte
    private ushort _snakeStride;    // 2bytes
    private byte* _memory;          // 8byte
    private BattleField _battleField; // 10 bytes
    private fixed byte _padding[HeaderSize - sizeof(bool) - sizeof(ushort) - 8 - 10];
    
    // === CACHE LINE 2 - SNAKES POINTERS ===
    private fixed long _snakePointers[Constants.MaxSnakes]; // 64 bytes (8 bytes per pointer)

    /// <summary>
    /// Initializes memory structures based on board dimensions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(byte boardWidth, byte boardHeight)
    {
        if (_isInitialized) return;

        var boardArea = (ushort)(boardWidth * boardHeight);
        var desiredBodyLength = (ushort)(boardArea * Constants.MaxBodyLengthRatio);
        var desiredBodyBytes = (uint)(desiredBodyLength * sizeof(ushort));
        var allocatedBodyBytes = (desiredBodyBytes + Constants.CacheLineSize - 1) / Constants.CacheLineSize * Constants.CacheLineSize;
        var snakeStride = (ushort)(BattleSnake.HeaderSize + allocatedBodyBytes);
        var totalSnakeMemory = (nuint)(snakeStride * Constants.MaxSnakes);
        
        _snakeStride = snakeStride;
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, Constants.CacheLineSize);
        _battleField.Initialize(boardArea);
        
        _isInitialized = true;
        
        PrecalculatePointers();
    }
    
    /// <summary>
    /// Pre-calcola e memorizza i puntatori all'inizio di ogni struttura BattleSnake.
    /// Questo evita calcoli ripetuti dell'offset durante il gioco.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrecalculatePointers()
    {
        for (var i = 0; i < Constants.MaxSnakes; i++) _snakePointers[i] = (long)_memory + i * _snakeStride;
    }

    /// <summary>
    /// Restituisce un puntatore al serpente specificato dal suo indice.
    /// Questo metodo è estremamente veloce grazie all'uso dell'array di puntatori pre-calcolati.
    /// </summary>
    /// <param name="index">L'indice del serpente (da 0 a MaxSnakes-1).</param>
    /// <returns>Un puntatore a BattleSnake.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(byte index) => (BattleSnake*)_snakePointers[index];

    /// <summary>
    /// Rilascia tutta la memoria non gestita allocata da questa istanza.
    /// </summary>
    public void Dispose()
    {
        NativeMemory.AlignedFree(_memory);
        _battleField.Dispose();

        _isInitialized = false;
    }
}