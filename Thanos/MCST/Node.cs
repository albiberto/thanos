using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;

namespace Thanos.MCST;

// Layout Esplicito: 64 Byte esatti (1 Cache Line)
[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // [0-7] Hash Zobrist (8 byte)
    [FieldOffset(0)] public long Hash;

    // [8-23] Rewards accumulati per 4 giocatori (4 * float = 16 byte)
    // SIMD Friendly alignment (Offset 8 è multiplo di 16? No, multiplo di 8. 
    // Per AVX serve 32, ma per SSE/Neon va bene offset 8 se carichi unaligned o usi Vector128)
    [FieldOffset(8)] public fixed float Rewards[4];

    // [24-27] Numero visite
    [FieldOffset(24)] public int Visits;

    // [28-31] Indice nodo padre
    [FieldOffset(28)] public int ParentIndex;

    // [32-35] Primo figlio (Linked List)
    [FieldOffset(32)] public int FirstChildIndex;

    // [36-39] Fratello successivo
    [FieldOffset(36)] public int NextSiblingIndex;

    // [40] Chi ha mosso per arrivare qui
    [FieldOffset(40)] public byte PlayerIndex;

    // [41] Mossa fatta per arrivare qui
    [FieldOffset(41)] public byte Move;

    // [42] Flags (Terminal, Solved, Chance)
    [FieldOffset(42)] public NodeFlags Flags;

    // [43] Padding (Inutilizzato)
    
    // [44-47] OTTIMIZZAZIONE 3: Log(Visits) pre-calcolato
    // Evita Math.Log() nel loop caldo di Selection.
    [FieldOffset(44)] public float LogVisits; 

    // Padding [48-63] per arrivare a 64 bytes.

    // --- METODI ---

    public void PlacementRoot(long hash)
    {
        Hash = hash;
        Visits = 0;
        LogVisits = 0;
        ClearRewards();
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        PlayerIndex = 0;
        Move = Moves.None;
        Flags = NodeFlags.None;
    }

    public void PlacementNew(int parentIndex, byte move, long hash, byte playerIndex, bool isChanceNode)
    {
        Hash = hash;
        Visits = 0;
        LogVisits = 0;
        ClearRewards();
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        PlayerIndex = playerIndex;
        Move = move;
        Flags = isChanceNode ? NodeFlags.ChanceNode : NodeFlags.None;
    }

    public void NewRoot()
    {
        ParentIndex = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateStats(ReadOnlySpan<float> rewards)
    {
        Visits++;
        // Pre-calcolo immediato del logaritmo
        LogVisits = MathF.Log(Visits);
        
        for (var i = 0; i < 4; i++) Rewards[i] += rewards[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearRewards()
    {
        for (var i = 0; i < 4; i++) Rewards[i] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkTerminal() => Flags |= NodeFlags.Terminal;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedWin() => Flags |= NodeFlags.SolvedWin;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedLoss() => Flags |= NodeFlags.SolvedLoss;

    public bool IsLeafNode => FirstChildIndex == -1;
    public bool IsChanceNode => (Flags & NodeFlags.ChanceNode) != 0;
    public bool IsTerminal => (Flags & NodeFlags.Terminal) != 0;
    public bool IsSolvedWin => (Flags & NodeFlags.SolvedWin) != 0;
    public bool IsSolvedLoss => (Flags & NodeFlags.SolvedLoss) != 0;
}

[Flags]
public enum NodeFlags : byte
{
    None = 0,
    Terminal = 1 << 0,
    ChanceNode = 1 << 1,
    SolvedWin = 1 << 2,
    SolvedLoss = 1 << 3
}