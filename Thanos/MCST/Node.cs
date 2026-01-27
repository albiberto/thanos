using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Thanos.Common;

namespace Thanos.MCST;

// Layout "The LightSpeed" - 64 Byte (Cache Line Exact)
[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // --- BLOCCO 1: Dati Atomici & Hot (24 byte) ---
    // Questi sono i dati acceduti costantemente per UCT e Backpropagation.
    // Devono stare all'inizio per massima probabilità di Cache Hit.

    // Rewards in Fixed-Point (es. *10000) per Interlocked.Add
    [FieldOffset(0)] public fixed int AtomicRewards[4]; // 16 byte
    
    // Concurrency Control
    [FieldOffset(16)] public int VirtualLoss; // 4 byte
    [FieldOffset(20)] public int Visits;      // 4 byte

    // --- BLOCCO 2: Identità & Hash (8 byte) ---
    // Hash Zobrist allineato a 8-byte
    [FieldOffset(24)] public long Hash; 

    // --- BLOCCO 3: Struttura Albero (12 byte) ---
    // Indici per la navigazione
    [FieldOffset(32)] public int ParentIndex;
    [FieldOffset(36)] public int FirstChildIndex;
    [FieldOffset(40)] public int NextSiblingIndex;

    // --- BLOCCO 4: Metadata Minimi (2 byte) ---
    
    // Mossa che l'Eroe ha fatto per arrivare qui. 
    // Fondamentale per estrarre le statistiche alla radice.
    [FieldOffset(44)] public byte Move; 
    
    // Stato del nodo (Terminal, Solved)
    [FieldOffset(45)] public NodeFlags Flags;

    // Padding implicito: 46..63 (18 byte liberi per usi futuri, es. euristiche statiche)

    // --- METODI ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PlacementRoot(long hash)
    {
        ClearAtomicData();
        Hash = hash;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        Move = Moves.None;
        Flags = NodeFlags.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PlacementNew(int parentIndex, byte move, long hash)
    {
        ClearAtomicData();
        Hash = hash;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        Move = move; // Salviamo la mossa dell'eroe
        Flags = NodeFlags.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewRoot()
    {
        ParentIndex = -1;
        VirtualLoss = 0; // Reset sicurezza per Tree Reuse
        // Non resettiamo visite/rewards per mantenere la conoscenza acquisita
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateStatsAtomic(ReadOnlySpan<int> fixedPointRewards)
    {
        Interlocked.Increment(ref Visits);
        
        // Unrolling manuale per velocità massima
        Interlocked.Add(ref AtomicRewards[0], fixedPointRewards[0]);
        Interlocked.Add(ref AtomicRewards[1], fixedPointRewards[1]);
        Interlocked.Add(ref AtomicRewards[2], fixedPointRewards[2]);
        Interlocked.Add(ref AtomicRewards[3], fixedPointRewards[3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearAtomicData()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int>.Zero.Store((int*)Unsafe.AsPointer(ref AtomicRewards[0]));
        }
        else
        {
            AtomicRewards[0] = 0; AtomicRewards[1] = 0; 
            AtomicRewards[2] = 0; AtomicRewards[3] = 0;
        }
        VirtualLoss = 0;
        Visits = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkTerminal() => Flags |= NodeFlags.Terminal;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedWin() => Flags |= NodeFlags.SolvedWin;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedLoss() => Flags |= NodeFlags.SolvedLoss;

    public bool IsLeafNode => FirstChildIndex == -1;
    public bool IsTerminal => (Flags & NodeFlags.Terminal) != 0;
    public bool IsSolvedWin => (Flags & NodeFlags.SolvedWin) != 0;
    public bool IsSolvedLoss => (Flags & NodeFlags.SolvedLoss) != 0;
}

[Flags]
public enum NodeFlags : byte
{
    None = 0,
    Terminal = 1 << 0,
    SolvedWin = 1 << 1,
    SolvedLoss = 1 << 2
}