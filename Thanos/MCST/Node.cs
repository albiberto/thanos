using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // --- STATISTICHE (20 bytes) ---
    [FieldOffset(0)] public int Visits;
    [FieldOffset(4)] public fixed float Rewards[4]; // MaxN per 4 giocatori

    // --- ALBERO (8 bytes) ---
    [FieldOffset(20)] public int FirstChildIndex;
    [FieldOffset(24)] public int NextSiblingIndex;

    // --- IDENTIFICAZIONE (16 bytes) ---
    [FieldOffset(28)] public long Hash;
    [FieldOffset(36)] public int ParentIndex;

    // --- METADATI (4 bytes) ---
    [FieldOffset(40)] public byte PlayerIndex; // 0-3: Snake, 255: Environment
    [FieldOffset(41)] public byte Move;
    [FieldOffset(42)] public NodeFlags Flags;
    [FieldOffset(43)] private byte _padding;

    // --- METODI ---

    public void PlacementRoot(long hash)
    {
        Visits = 0;
        ClearRewards();
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        PlayerIndex = 0;
        Hash = hash;
        Move = Moves.None;
        Flags = NodeFlags.None;
    }

    public void PlacementNew(int parentIndex, byte move, long hash, byte playerIndex, bool isChanceNode)
    {
        Visits = 0;
        ClearRewards();
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        PlayerIndex = playerIndex;
        Hash = hash;
        Move = move;
        Flags = isChanceNode ? NodeFlags.ChanceNode : NodeFlags.None;
    }

    public void NewRoot()
    {
        ParentIndex = -1;
        // Manteniamo le statistiche accumulate per sfruttare il "calore" dell'albero
        // Ma potremmo voler ridurre il peso delle visite precedenti (decay)
        // Per ora reset semplice per sicurezza nel cambio root
        // Visits = 0; 
        // ClearRewards();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateStats(ReadOnlySpan<float> rewards)
    {
        Visits++;
        for (int i = 0; i < 4; i++)
        {
            Rewards[i] += rewards[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearRewards()
    {
        for (int i = 0; i < 4; i++) Rewards[i] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkTerminal() => Flags |= NodeFlags.Terminal;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedWin() => Flags |= NodeFlags.SolvedWin;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkSolvedLoss() => Flags |= NodeFlags.SolvedLoss;

    // Proprietà
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