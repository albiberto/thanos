using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Thanos.Enums;
    
    namespace Thanos;
    
    /// <summary>
    /// Manages the game's global context, memory, and all snake entities.
    /// This struct is designed for high-performance scenarios, allocating memory
    /// for the game state and entities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BattleArena : IDisposable
    {
        // --- Constants for memory layout ---
        private const int PaddingSize = Constants.CacheLine - sizeof(int) * 3 - BattleField.Offset - sizeof(uint);
    
        public const int HeaderSize = Constants.CacheLine;
    
        // ======================================================================
        // === CACHE LINE 1: HEADER
        // ======================================================================
        
        public int SnakesCount;
        
        private int _snakeStride;
        private int _maxBodyLength;
        private byte* _snakesBodyMemory;
        private BattleField* _battleField;
    
        private fixed byte _padding[PaddingSize];
    
        // ======================================================================
        // === CACHE LINE 2: SNAKE POINTERS
        // ======================================================================
        
        private fixed long _snakePointers[Constants.MaxSnakes];
        
        // ======================================================================
        // === END CACHE LINES
        // ======================================================================
    
        /// <summary>
        /// Initializes the engine and its state from a given set of starting positions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PlacementNew(BattleArena* arena, BattleSnake* snakes, BattleField* battleField, uint boardWidth, uint boardHeight, int snakesCount)
        {
            arena->SnakesCount = snakesCount;

            var area = boardWidth * boardHeight;
            
            // -- Step 1: Calculate memory for the BattleField --
            var battleFieldMemorySize = CalculateBattleFieldMemorySize(area);
            
            // --- Step 2: Calculate Memory for BattleSnake ---
            var idealSnakeBodyLength = (int)BitOperations.RoundUpToPowerOf2(area);
            var realSnakeBodyLength = Math.Min(idealSnakeBodyLength, Constants.MaxBodyLength);
            var snakeStride = BattleSnake.HeaderSize + realSnakeBodyLength * sizeof(ushort);
            var totalSnakesBodyMemorySize = (nuint)(snakeStride * snakesCount);
            
            // --- Step 3: Allocate Memory ---
            
            _snakesBodyMemory = (byte*)NativeMemory.AlignedAlloc(totalSnakesBodyMemorySize, Constants.CacheLine);
    
            // Allocate a single block for BattleField header and its grid
            var battleFieldMemory = (byte*)NativeMemory.AlignedAlloc(battleFieldMemorySize, Constants.CacheLine);
            _battleField = (BattleField*)battleFieldMemory;
            var gridMemory = battleFieldMemory + BattleField.HeaderSize;
    
            // Initialize BattleField using the pre-allocated memory
            BattleField.PlacementNew(_battleField, gridMemory);
    
            // --- Step 3: Initialize All Snakes in a Single Pass ---
            InitializeSnakes(startingPositions);
    
            _isInitialized = true;
        }
        
        /// <summary>
        /// Calculates the total memory size required for a BattleField instance
        /// and its associated grid, ensuring proper alignment for SIMD and cache.
        /// </summary>
        /// <param name="area">The number of elements in the grid (unrounded).</param>
        /// <returns>Total aligned memory size in bytes.</returns>
        /// <remarks>
        /// To maximize performance, this method ensures that the main data grid starts on an
        /// aligned memory address (a cache line boundary) and that its total size is a
        /// multiple of the cache line size.
        /// 
        /// The memory layout is structured as follows:
        /// [BattleField Header | Padding | Aligned Grid]
        /// 
        /// 1.  <c>gridStartOffset</c>: First, it calculates the space needed for the BattleField
        ///     header by rounding its size UP to the next cache line multiple. This value
        ///     becomes the starting offset for the grid, guaranteeing its alignment. Any
        ///     extra space between the header's end and the grid's start is padding.
        /// 
        /// 2.  <c>battleFieldGridSize</c>: Second, it calculates the space for the grid itself,
        ///     also rounding its size UP to a cache line multiple. This prevents vectorized
        ///     (SIMD) operations from reading/writing beyond the allocated memory.
        ///
        /// The total memory required is the grid's starting offset plus the grid's own aligned size.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint CalculateBattleFieldMemorySize(nuint area)
        {
            // -- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! --
            // PAY ATTENTION: In integer division, the computer discards the remainder (a truncation operation). 
            // -- !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! --
            
            // Calculate the offset where the grid will start in memory
            // We round up the structure size to the next multiple of the alignment size
            const uint gridStartOffset = (BattleField.Offset + Constants.CacheLine - 1) / Constants.CacheLine * Constants.CacheLine;

            // Calculate the size of the grid itself, rounded up to a multiple of the alignment size
            var battleFieldGridSize = (area + Constants.CacheLine - 1) / Constants.CacheLine * Constants.CacheLine; 
            
            // Total memory is the aligned offset of the grid plus the aligned size of the grid
            return gridStartOffset + battleFieldGridSize;
        }

    
        /// <summary>
        /// Initializes all active snakes in a single loop.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSnakes(ushort* startingPositions)
        {
            for (byte i = 0; i < SnakesCount; i++)
            {
                var snakePtr = _snakesBodyMemory + i * _snakeStride;
                _snakePointers[i] = (long)snakePtr;
    
                // Initialize the snake with default values.
                // The deserializer will later populate it with real data.
                BattleSnake.PlacementNew((BattleSnake*)snakePtr, 100, 1, _maxBodyLength, startingPositions + i);
            }
        }
    
        /// <summary>
        /// Gets a pointer to the snake at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BattleSnake* GetSnake(byte index) => (BattleSnake*)_snakePointers[index];
    
        /// <summary>
        /// Simulates a full turn for all active snakes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Simulate(ushort* moves)
        {
            var updateCount = 0;
    
            for (byte i = 0; i < SnakesCount; i++)
            {
                var snake = GetSnake(i);
                if (snake->Health <= 0) continue;
    
                var newHeadPosition = moves[i];
                var content = (*_battleField)[newHeadPosition];
    
                if (content >= Constants.Me)
                {
                    snake->Kill();
                    _battleField->RemoveSnake(snake);
                    continue;
                }
    
                var hasEaten = content == Constants.Food;
                var damage = content == Constants.Hazard ? 14 : 0; // TODO: Use constant for hazard damage
    
                var oldTail = snake->TailIndex;
                snake->Move(newHeadPosition, hasEaten, damage);
    
                if (snake->Health <= 0)
                {
                    _battleField->RemoveSnake(snake);
                }
                else
                {
                    _turnUpdates[updateCount++] = new BattleField.TurnUpdate(snake->Head, oldTail, i, hasEaten);
                }
            }
    
            if (updateCount > 0) _battleField->Update(_turnUpdates, updateCount);
        }
    
        /// <summary>
        /// Frees all unmanaged memory allocated by this instance.
        /// </summary>
        public void Dispose()
        {
            if (!_isInitialized) return;
    
            NativeMemory.AlignedFree(_snakesBodyMemory);
            _snakesBodyMemory = null;
    
            NativeMemory.AlignedFree(_turnUpdates);
            _turnUpdates = null;
    
            if (_battleField != null)
            {
                // Free the entire block allocated for BattleField (header + grid)
                NativeMemory.AlignedFree(_battleField);
                _battleField = null;
            }
    
            _isInitialized = false;
        }
    }