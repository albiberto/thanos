using System.Runtime.InteropServices;
using Thanos.Enums;

namespace Thanos;

/// <summary>
/// Manages the game's global context and all snake entities.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public unsafe struct Tesla : IDisposable
{
    private bool _isInitialized;
    private ushort _snakeStride;
    private byte* _memory;
    private BattleField _battleField;
    private fixed long _snakePointers[Constants.MaxSnakes];
    
    /// <summary>
    /// Initializes memory structures based on board dimensions.
    /// </summary>
    public void Initialize(byte boardWidth, byte boardHeight)
    {
        if (_isInitialized) return;

        var boardArea = (ushort)(boardWidth * boardHeight);

        // --- LOGICA CHIAVE: Calcolo dinamico della dimensione del corpo ---

        // 1. Definisci una dimensione desiderata (es. 75% della plancia)
        var desiredBodyLength = (ushort)(boardArea * 3 / 4);
        var desiredBodyBytes = (uint)(desiredBodyLength * sizeof(ushort));

        // 2. Arrotonda la dimensione in BYTE alla cache line successiva (64 byte)
        // Questa è la formula standard per arrotondare per eccesso a un multiplo.
        var allocatedBodyBytes = (desiredBodyBytes + Constants.CacheLineSize - 1) 
                                / Constants.CacheLineSize * Constants.CacheLineSize;

        // 3. Riconverti i byte allocati in numero di segmenti (ushort)
        _maxBodyLength = (ushort)(allocatedBodyBytes / sizeof(ushort));

        // --- Fine Logica Chiave ---

        _snakeStride = (ushort)(BattleSnake.HeaderSize + _maxBodyLength * sizeof(ushort));
        var totalSnakeMemory = (nuint)(_snakeStride * Constants.MaxSnakes);
        _memory = (byte*)NativeMemory.AlignedAlloc(totalSnakeMemory, Constants.CacheLineSize);
        _battleField.Initialize(boardArea);
        
        _isInitialized = true;
        PrecalculatePointers();
        
        // È importante resettare lo stato dei serpenti dopo aver calcolato MaxLength
        ResetSnakesToStart();
    }
    
    // Metodo per inizializzare lo stato di ogni serpente
    private void ResetSnakesToStart()
    {
        for (var i = 0; i < Constants.MaxSnakes; i++)
        {
            var snake = GetSnake(i);
            // Passa il MaxLength calcolato a ogni serpente.
            // Le posizioni iniziali andrebbero prese dai dati del primo turno.
            snake->Reset(0); 
        }
    }
    
    public void Dispose() { /* ... */ }
}