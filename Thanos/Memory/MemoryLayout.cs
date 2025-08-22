using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.War.Arena;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe record struct MemoryLayout
{
    // --- Dimensioni dei Componenti ---
    public readonly int NodeSize;

    public readonly int BitboardStride;
    public readonly int BitboardsSize;

    public readonly int SnakeHealthSize;
    public readonly int SnakeAnatomySize;
    public readonly int SnakeHeaderSize;
    public readonly int SnakeStride;
    public readonly int SnakesSize;
    
    public readonly int WarArenaHeaderSize;
    public readonly int WorkspaceSize;

    // --- Dettagli Granulari (utili per l'accesso ai dati) ---
    
    
    // --- Offset dei Blocchi Principali ---
    public readonly int NodeOffset;
    public readonly int BitboardsOffset;
    public readonly int SnakesOffset;
    public readonly int WarArenaHeaderOffset;
    public readonly int WorkspaceOffset;
    
    // --- Dettagli Interni del Workspace ---
    public readonly int NewHeadPositionsSize;
    public readonly int HasEatenSize;
    public readonly int IsDeadSize;
    public readonly int OldTailPositionsSize;
    public readonly int NewHeadPositionsWorkspaceOffset;
    public readonly int HasEatenWorkspaceOffset;
    public readonly int IsDeadWorkspaceOffset;
    public readonly int OldTailPositionsWorkspaceOffset;

    // --- Totali ---
    public readonly int SlotSize;

    public static MemoryLayout Worst { get; } = new(Constants.MaxArea, Constants.MaxSnakeCount);

    public MemoryLayout(int area, int snakeCount)
    {
        // =================================================================
        // FASE 1: Calcolo Dimensioni dei Singoli Componenti
        // =================================================================
        
        NodeSize = sizeof(Node).AlignUp();
        WarArenaHeaderSize = sizeof(WarArenaHeader).AlignUp();

        // Calcolo per i Bitboard
        // La riga calcola il numero di segmenti necessari per rappresentare l'area come bitboard, dove ogni segmento contiene 64 bit.
        // In pratica, divide `area` per 64 arrotondando per eccesso, così da coprire tutta l'area anche se non è un multiplo esatto di 64.
        // Questo è utile per strutture dati che usano array di `ulong` per rappresentare insiemi di bit.
        var bitboardSegments = (((area + 63) >> 6) * sizeof(ulong)).AlignUp();
        BitboardStride = bitboardSegments / sizeof(ulong);
        BitboardsSize = bitboardSegments * WarGrid.TotalBitboards;

        // Header di un serpente ora include sia Health che Anatomy.
        SnakeHealthSize = sizeof(Health);
        SnakeAnatomySize = sizeof(Anatomy);
        SnakeHeaderSize = (SnakeHealthSize + SnakeAnatomySize).AlignUp();
        
        var snakeBodyCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)area), Constants.MaxSnakeBodyCapacity);
        SnakeStride = (SnakeHeaderSize + snakeBodyCapacity * sizeof(ushort)).AlignUp(); 
        SnakesSize = SnakeStride * snakeCount;
        // --- FINE MODIFICA ---

        // Calcolo dettagliato per il Workspace
        NewHeadPositionsSize = (sizeof(ushort) * snakeCount);
        HasEatenSize = (sizeof(bool) * snakeCount);
        IsDeadSize = (sizeof(bool) * snakeCount);
        OldTailPositionsSize = (sizeof(ushort) * snakeCount);

        NewHeadPositionsWorkspaceOffset = 0;
        HasEatenWorkspaceOffset = NewHeadPositionsWorkspaceOffset + NewHeadPositionsSize;
        IsDeadWorkspaceOffset = HasEatenWorkspaceOffset + HasEatenSize;
        OldTailPositionsWorkspaceOffset = IsDeadWorkspaceOffset + IsDeadSize;
        
        var totalWorkspaceUnaligned = OldTailPositionsWorkspaceOffset + OldTailPositionsSize;
        WorkspaceSize = totalWorkspaceUnaligned.AlignUp();

        // =================================================================
        // FASE 2: Calcolo Offset Sequenziali dei Blocchi di Memoria
        // =================================================================

        NodeOffset = 0;
        BitboardsOffset = NodeOffset + NodeSize;
        SnakesOffset = BitboardsOffset + BitboardsSize;
        WarArenaHeaderOffset = SnakesOffset + SnakesSize;
        WorkspaceOffset = WarArenaHeaderOffset + WarArenaHeaderSize;
        
        // =================================================================
        // FASE 3: Calcolo Totale
        // =================================================================
        
        SlotSize = NodeSize + BitboardsSize + SnakesSize + WarArenaHeaderSize + WorkspaceSize;
    }
}

public static class MemoryExtensions
{
    private const long Alignment = Constants.SizeOfCacheLine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (int)((value + Alignment - 1) & ~(Alignment - 1));
}