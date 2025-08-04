using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Tesla : IDisposable
{
    private const int HeaderSize = 64;

    // === CACHE LINE 1 ===
    private bool _isInitialized;
    private ushort _snakeStride;
    private ushort _maxBodyLength;
    private byte _activeSnakes;
    private byte* _memory;
    private BattleField _battleField;
    private fixed byte _padding[HeaderSize - (sizeof(bool) + sizeof(byte) + sizeof(ushort) * 2 + 8 + 10)];

    // === CACHE LINE 2 ===
    private fixed long _snakePointers[Constants.MaxSnakes];

    /// <summary>
    ///     Inizializza l'engine e il suo stato direttamente da un set di posizioni iniziali.
    ///     Questo metodo alloca la quantità di memoria ESATTA per i serpenti forniti.
    ///     Ideale per scenari di deserializzazione a singolo turno.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeFromState(byte boardWidth, byte boardHeight, ReadOnlySpan<ushort> startingPositions)
    {
        if (_isInitialized)
            // Se l'engine era già inizializzato, dobbiamo prima liberare la vecchia memoria
            Dispose();

        _activeSnakes = (byte)startingPositions.Length;
        var boardArea = (ushort)(boardWidth * boardHeight);

        // 1. CALCOLO MEMORIA
        // I calcoli sono gli stessi, ma useremo _activeSnakes invece di Constants.MaxSnakes
        var desiredBodyLength = (ushort)(boardArea * Constants.MaxBodyLengthRatio);
        var desiredBodyBytes = (uint)(desiredBodyLength * sizeof(ushort));
        var allocatedBodyBytes = (desiredBodyBytes + Constants.CacheLineSize - 1) / Constants.CacheLineSize * Constants.CacheLineSize;
        _maxBodyLength = (ushort)(allocatedBodyBytes / sizeof(ushort));
        _snakeStride = (ushort)(BattleSnake.HeaderSize + allocatedBodyBytes);

        // Alloca memoria ESATTA per i serpenti attivi
        var totalSnakeMemory = (nuint)(_snakeStride * _activeSnakes);

        // 2. ALLOCAZIONE E PRE-CALCOLO
        _battleField.Initialize(boardArea);

        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, Constants.CacheLineSize);
        InitializeSnakes(startingPositions);

        _isInitialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeSnakes(ReadOnlySpan<ushort> startingPositions)
    {
        for (byte i = 0; i < _activeSnakes; i++)
        {
            _snakePointers[i] = (long)(_memory + i * _snakeStride);
            GetSnake(i)->Reset(startingPositions[i], _maxBodyLength);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(byte index) => (BattleSnake*)_snakePointers[index];

    public void Dispose()
    {
        if (!_isInitialized) return;

        NativeMemory.AlignedFree(_memory);
        _memory = null;

        _battleField.Dispose();
        _isInitialized = false;
    }
}