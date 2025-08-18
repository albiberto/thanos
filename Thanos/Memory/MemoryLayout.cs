using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe record struct MemoryLayout
{
    // --- Dimensioni ---
    public readonly int NodeSize;
    public readonly int BitboardsSize;
    public readonly int SnakesSize;
    public readonly int SnakeStride;
    public readonly int BitboardStrideInBytes;
    public readonly int BitboardStrideInUlongs;
    public readonly int WarArenaHeaderSize;
    public readonly int WorkspaceSize;

    // --- Offset ---
    public readonly int NodeOffset;
    public readonly int BitboardsOffset;
    public readonly int SnakesOffset;
    public readonly int WarArenaHeaderOffset;
    public readonly int WorkspaceOffset;
    
    // --- NUOVO: Dettagli Interni del Workspace ---
    // Dimensioni dei singoli buffer nel workspace
    public readonly int NewHeadPositionsSize;
    public readonly int HasEatenSize;
    public readonly int IsDeadSize;
    public readonly int OldTailPositionsSize;

    // Offset dei buffer RELATIVI all'inizio del workspace
    public readonly int NewHeadPositionsWorkspaceOffset;
    public readonly int HasEatenWorkspaceOffset;
    public readonly int IsDeadWorkspaceOffset;
    public readonly int OldTailPositionsWorkspaceOffset;

    // --- Totali ---
    public readonly int SlotSize;

    public static MemoryLayout Worst { get; } = new(Constants.MaxArea, Constants.MaxSnakeCount);
    
    public MemoryLayout(int area, int snakeCount)
    {
        // --- 1. Calcolo Dimensioni ---
        NodeSize = sizeof(Node).AlignUp();

        var bitboardSegments = (area + 63) >> 6;
        BitboardStrideInBytes = (bitboardSegments * sizeof(ulong)).AlignUp();
        BitboardStrideInUlongs = BitboardStrideInBytes / sizeof(ulong);
        BitboardsSize = BitboardStrideInBytes * WarField.TotalBitboards;

        var snakeBodyCapacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)area), Constants.MaxSnakeBodyCapacity);
        var snakeHeaderSize = sizeof(WarSnakeHeader).AlignUp();
        SnakeStride = (snakeHeaderSize + snakeBodyCapacity * sizeof(ushort)).AlignUp();
        SnakesSize = SnakeStride * snakeCount;
        
        WarArenaHeaderSize = sizeof(WarArenaHeader).AlignUp();
        
        // --- Dimensione Workspace per la WarArena ---
        // newHeadPositions + hasEaten + isDead + oldTailPositions
        // --- NUOVO: Calcolo Dettagliato Workspace ---
// Calcola la dimensione di ogni buffer...
        NewHeadPositionsSize = sizeof(ushort) * snakeCount;
        HasEatenSize = sizeof(bool) * snakeCount;
        IsDeadSize = sizeof(bool) * snakeCount;
        OldTailPositionsSize = sizeof(ushort) * snakeCount;

// ...calcola i loro offset relativi all'interno del blocco workspace...  <-- ECCOLO QUI
        NewHeadPositionsWorkspaceOffset = 0;
        HasEatenWorkspaceOffset = NewHeadPositionsWorkspaceOffset + NewHeadPositionsSize;
        IsDeadWorkspaceOffset = HasEatenWorkspaceOffset + HasEatenSize;
        OldTailPositionsWorkspaceOffset = IsDeadWorkspaceOffset + IsDeadSize;

// ...e infine la dimensione totale del blocco workspace, allineata.
        var totalWorkspaceUnaligned = OldTailPositionsWorkspaceOffset + OldTailPositionsSize;
        WorkspaceSize = totalWorkspaceUnaligned.AlignUp();

        // --- 2. Calcolo Totali ---
        SlotSize = NodeSize + BitboardsSize + SnakesSize + WarArenaHeaderSize + WorkspaceSize;

        // --- 3. Calcolo Offset ---
        NodeOffset = 0;
        BitboardsOffset = NodeOffset + NodeSize;
        SnakesOffset = BitboardsOffset + BitboardsSize;
        WarArenaHeaderOffset = SnakesOffset + SnakesSize;
        WorkspaceOffset = WarArenaHeaderOffset + WarArenaHeaderSize; 
    }
}

public static class MemoryExtensions
{
    private const long Alignment = Constants.SizeOfCacheLine;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignUp(this int value) => (int)((value + Alignment - 1) & ~(Alignment - 1));
}
