using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Manages the game's global context, memory, and all snake entities.
/// This struct is designed for high-performance, single-turn scenarios (e.g., deserializing a game state),
/// allocating the exact amount of memory required for the active snakes.
/// </summary>
/// <remarks>
/// --- CACHE-OPTIMIZED MEMORY LAYOUT (x64) ---
/// The struct's fields are ordered to align with CPU cache lines (64 bytes).
///
/// --- CACHE LINE 1: State Header (0-63 bytes) ---
/// Contains all critical state fields. This ensures that the core state of the
/// engine can be loaded into the CPU's L1 cache in a single memory fetch operation.
/// The data types have been chosen to minimize casting during initialization logic.
///
/// --- CACHE LINE 2: Pointer Table (64+ bytes) ---
/// A fixed-size buffer used as a lookup table for snake pointers. Keeping this
/// separate but contiguous with the header improves data locality.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Tesla : IDisposable
{
    private const int PaddingCount = 2;
    private const int PaddingSize1 = Constants.CacheLineSize - sizeof(bool) - sizeof(int) * 3 - sizeof(long) * 2 - BattleField.Size; // 25 bytes
    private const int PaddingSize2 = 0;
    
    public const int HeaderSize = PaddingCount * Constants.CacheLineSize;

    // === CACHE LINE 1 - State Header ===
    private bool _isInitialized;    // 1 byte
    public int ActiveSnakes;     // 4 byte
    private int _snakeStride;       // 4 bytes (int to avoid casting from HeaderSize)
    private int _maxBodyLength;    // 4 bytes (uint to match RoundUpToPowerOfTwo's return type)
    private byte* _memory;          // 8 bytes
    private BattleField.TurnUpdate* _turnUpdates; // 8 bytes
    private BattleField _battleField; // 10 bytes
    
    // Padding to fill the 64-byte cache line: 64 - (1+4+4+4+8+10) = 33 bytes.
    private fixed byte _padding[PaddingSize1];

    // === CACHE LINE 2 - Pointer Table ===
    // This remains fixed-size because the struct layout must be known at compile time.
    // It defines the maximum capacity of the engine.
    private fixed long _snakePointers[Constants.MaxSnakes];
    
    /// <summary>
    /// Initializes the engine and its state from a given set of starting positions.
    /// This method allocates the EXACT amount of memory required for the provided snakes
    /// and is ideal for single-turn deserialization scenarios.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(uint boardWidth, uint boardHeight, ReadOnlySpan<ushort> startingPositions)
    {
        if (_isInitialized) Dispose();

        ActiveSnakes = startingPositions.Length;
        var boardArea = boardWidth * boardHeight;

        // --- Step 1: Calculate Memory Layout ---
        var idealCapacity = (int)BitOperations.RoundUpToPowerOf2(boardArea);
        _maxBodyLength = Math.Min(idealCapacity, Constants.MaxBodyLength); // Cap the capacity at 256.
        _snakeStride = BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort); // Calculate byte sizes and the final stride for a single snake.
        
        // --- Step 2: Allocate Memory ---
        var totalSnakeMemory = (nuint)(_snakeStride * ActiveSnakes);
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, Constants.CacheLineSize);
        _turnUpdates = (BattleField.TurnUpdate*)NativeMemory.AlignedAlloc(BattleField.TurnUpdate.TurnSize * 8, Constants.CacheLineSize);
        _battleField.Initialize(boardArea);
        
        // --- Step 3: Initialize All Snakes in a Single Pass ---
        InitializeSnakes(startingPositions);
        
        _isInitialized = true;
    }

    /// <summary>
    /// Initializes all active snakes in a single loop, calculating their memory pointers
    /// and resetting their state. This is a helper method for InitializeFromState.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeSnakes(ReadOnlySpan<ushort> startingPositions)
    {
        for (byte i = 0; i < ActiveSnakes; i++)
        {
            var snakePtr = _memory + i * _snakeStride; // Calculate the memory address for the current snake.
            
            _snakePointers[i] = (long)snakePtr; // Store the pointer in the lookup table.
            ((BattleSnake*)snakePtr)->Reset(startingPositions[i], _maxBodyLength); // Reset the snake's state at that memory location.
        }
    }

    /// <summary>
    /// Gets a pointer to the snake at the specified index from the pre-calculated lookup table.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(byte index) => (BattleSnake*)_snakePointers[index];
    
    
    /// <summary>
    /// Simulates a full turn for all active snakes, processing their moves,
    /// updating their internal state, and applying the results to the battlefield.
    /// </summary>
    /// <param name="moves">
    /// A read-only span containing the moves (as cell indices) for each active snake.
    /// The length must be at least equal to the number of active snakes.
    /// </param>
    /// <remarks>
    /// The method performs the simulation in two phases:
    /// 1. Evaluates each snake's move, updates its state, and records any changes.
    /// 2. Apply all collected updates to the battlefield in a single operation.
    /// Dead snakes are immediately removed from the battlefield.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Simulate(ReadOnlySpan<ushort> moves)
    {
        var updateCount = 0;
    
        for (byte i = 0; i < ActiveSnakes; i++)
        {
            var snake = GetSnake(i);
            if (snake->Health <= 0) continue;
        
            var oldTail = snake->Body[snake->TailIndex];
            var content = _battleField[moves[i]];
            var hasEaten = (content == Constants.Food);
        
            if (snake->Move(moves[i], content, 14))
            {
                _turnUpdates[updateCount++] = new BattleField.TurnUpdate(snake->Head, oldTail, i, hasEaten);
            }
            else
            {
                _battleField.RemoveSnake(snake);
            }
        }
    
        if (updateCount > 0) _battleField.Simulate(_turnUpdates, updateCount);
    }

    /// <summary>
    /// Frees all unmanaged memory allocated by this instance.
    /// </summary>
    public void Dispose()
    {
        if (!_isInitialized) return;

        NativeMemory.AlignedFree(_memory);
        _memory = null;
        
        NativeMemory.AlignedFree(_turnUpdates);
        _turnUpdates = null;

        _battleField.Dispose();
        _isInitialized = false;
    }
}