using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Manages the game's global context, including snake data and the master collision map.
/// This struct is the primary interface for manipulating the game state.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct Tesla : IDisposable
{
    // --- Campi Principali ---
    private byte _boardWidth;
    private byte _boardHeight;
    private ushort _maxBodyLength;
    private ushort _snakeStride;
    private bool _isInitialized;
    private byte* _memory; // Puntatore al blocco di memoria per tutti i serpenti
    private BattelField _battelField; // La matrice di collisione
    private fixed long _snakePointers[Constants.MaxSnakes]; // Puntatori pre-calcolati ai singoli serpenti

    // --- Proprietà Pubbliche ---
    public byte BoardWidth => _boardWidth;
    public byte BoardHeight => _boardHeight;
    public ref readonly BattelField BattelField => ref _battelField;

    /// <summary>
    /// Initializes the battlefield's memory structures once per game.
    /// </summary>
    public void Initialize(byte boardWidth, byte boardHeight)
    {
        if (_isInitialized) return;

        _boardWidth = boardWidth;
        _boardHeight = boardHeight;

        var boardArea = (ushort)(boardWidth * boardHeight);
        
        // Calcolo ottimizzato della dimensione dei serpenti
        const int snakeElementsPerCacheLine = Constants.CacheLineSize / sizeof(ushort);
        var desiredBodyLength = boardArea * 3 / 4;
        var maxBodyLength = (ushort)((desiredBodyLength + snakeElementsPerCacheLine - 1) / snakeElementsPerCacheLine * snakeElementsPerCacheLine);
        if (maxBodyLength < snakeElementsPerCacheLine) maxBodyLength = snakeElementsPerCacheLine;
        if (maxBodyLength > 256) maxBodyLength = 256;
        _maxBodyLength = maxBodyLength;

        _snakeStride = (ushort)(BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort));

        // Allocazione memoria per i serpenti
        var totalSnakeMemory = (nuint)(_snakeStride * Constants.MaxSnakes);
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, Constants.CacheLineSize);

        // Inizializzazione della matrice di collisione
        _battelField.Initialize(boardArea);
        
        _isInitialized = true;
        PrecalculatePointers();
    }
    
    /// <summary>
    /// Updates the entire board state for the current turn based on API data.
    /// This is the main orchestrator method to be called each turn.
    /// </summary>
    public void UpdateBoardState(ReadOnlySpan<ushort> foodPositions, ReadOnlySpan<ushort> hazardPositions)
    {
        // 1. Clear the board from the previous turn's state.
        _battelField.Clear();

        // 2. Project the current snake positions onto the grid.
        fixed (Tesla* thisPtr = &this)
        {
            _battelField.ProjectBattlefield(thisPtr, Constants.MaxSnakes);
        }

        // 3. Apply food and hazards. Hazards overwrite food if they overlap.
        _battelField.ApplyFood(foodPositions);
        _battelField.ApplyHazards(hazardPositions);
    }

    /// <summary>
    /// Processes movements for all active snakes, applying damage where necessary.
    /// </summary>
    public void ProcessAllMoves(ReadOnlySpan<ushort> newHeadPositions, ReadOnlySpan<CellContent> destinationContents, int hazardDamage)
    {
        for (var i = 0; i < Constants.MaxSnakes; i++)
        {
            var snakePtr = GetSnake(i);
            if (snakePtr->Health <= 0) continue;

            // Determine the damage to apply for this specific move.
            // The AI is responsible for determining the content of the destination cell.
            int damageToApply = 0;
            if (destinationContents[i] == CellContent.Hazard)
            {
                damageToApply = hazardDamage;
            }
            // Future logic could add damage for head-to-head collisions here.

            var bodyPtr = (ushort*)((byte*)snakePtr + BattleSnake.HeaderSize);
            snakePtr->Move(bodyPtr, newHeadPositions[i], destinationContents[i], damageToApply);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BattleSnake* GetSnake(int index) => (BattleSnake*)_snakePointers[index];

    private void PrecalculatePointers()
    {
        for (var i = 0; i < Constants.MaxSnakes; i++) _snakePointers[i] = (long)(_memory + i * _snakeStride);
    }

    public void Dispose()
    {
        if (!_isInitialized) return;
        _battelField.Dispose();
        NativeMemory.AlignedFree(_memory);
        _memory = null;
        _isInitialized = false;
    }
}