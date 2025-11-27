using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Common;

namespace Thanos.MCST;

// Layout ottimizzato per allineamento a 8 byte
[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // --- BLOCCO 1: Dati ad accesso frequente e allineati a 8 byte ---
    
    // Hash spostato all'offset 0 per allineamento perfetto (long = 8 byte)
    [FieldOffset(0)] public long Hash; 

    // Rewards (4 float = 16 byte). Offset 8 è multiplo di 4. Perfetto.
    [FieldOffset(8)] public fixed float Rewards[4]; 

    // --- BLOCCO 2: Interi (4 byte) ---
    
    [FieldOffset(24)] public int Visits;
    [FieldOffset(28)] public int ParentIndex;
    [FieldOffset(32)] public int FirstChildIndex;
    [FieldOffset(36)] public int NextSiblingIndex;

    // --- BLOCCO 3: Byte e Flags (1 byte) ---
    
    [FieldOffset(40)] public byte PlayerIndex;
    [FieldOffset(41)] public byte Move;
    [FieldOffset(42)] public NodeFlags Flags;
    
    // Padding implicito fino a 64 byte...

    // --- METODI ---

    public void PlacementRoot(long hash)
    {
        Hash = hash; // Ora è il primo campo
        Visits = 0;
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