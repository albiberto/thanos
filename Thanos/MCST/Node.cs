using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Thanos.Common;

namespace Thanos.MCST;

// Layout ottimizzato per SIMD (Rewards a Offset 0)
[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // --- BLOCCO 1: SIMD Hot Data (16 byte - Allineato a 16/64) ---
    
    // Spostiamo i Rewards all'inizio.
    // Il Pool garantisce l'allineamento a 64 byte del nodo.
    // Quindi Offset 0 è allineato a 16 byte -> Load/Store Vector128 perfettamente allineati.
    [FieldOffset(0)] public fixed float Rewards[4];

    // --- BLOCCO 2: Dati 8-byte (16 byte) ---

    // Hash scivola qui. Offset 16 è multiplo di 8 (e anche di 16, ottimo).
    [FieldOffset(16)] public long Hash;

    // --- BLOCCO 3: Interi (4 byte) ---

    [FieldOffset(24)] public int Visits;
    [FieldOffset(28)] public int ParentIndex;
    [FieldOffset(32)] public int FirstChildIndex;
    [FieldOffset(36)] public int NextSiblingIndex;

    // --- BLOCCO 4: Byte e Flags ---

    [FieldOffset(40)] public byte PlayerIndex;
    [FieldOffset(41)] public byte Move;
    [FieldOffset(42)] public NodeFlags Flags;

    // Padding implicito 43..63

    // --- METODI ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PlacementRoot(long hash)
    {
        ClearRewards(); // SIMD Clear
        Hash = hash;
        Visits = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        PlayerIndex = 0;
        Move = Moves.None;
        Flags = NodeFlags.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PlacementNew(int parentIndex, byte move, long hash, byte playerIndex, bool isChanceNode)
    {
        ClearRewards(); // SIMD Clear
        Hash = hash;
        Visits = 0;
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        PlayerIndex = playerIndex;
        Move = move;
        Flags = isChanceNode ? NodeFlags.ChanceNode : NodeFlags.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewRoot()
    {
        ParentIndex = -1;
    }

    // UPDATE STATS VETTORIZZATO
    // Riceve già il vettore dei rewards calcolato dal Worker
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateStats(Vector128<float> rewardsVector)
    {
        Visits++;

        if (Vector128.IsHardwareAccelerated)
        {
            // Carica i rewards correnti (aligned load se offset 0 e pool aligned)
            var current = Vector128.Load((float*)Unsafe.AsPointer(ref Rewards[0]));
            // Somma SIMD
            var result = current + rewardsVector;
            // Salva
            result.Store((float*)Unsafe.AsPointer(ref Rewards[0]));
        }
        else
        {
            // Fallback (non dovrebbe succedere su x64/ARM64 moderni)
            // Estraiamo i valori dal vettore o usiamo uno span se l'API cambiasse
            // Qui assumiamo che rewardsVector sia stato passato correttamente
            // Implementazione scalare di emergenza:
            var r = rewardsVector;
            Rewards[0] += r[0];
            Rewards[1] += r[1];
            Rewards[2] += r[2];
            Rewards[3] += r[3];
        }
    }

    // Overload per compatibilità se serve passare scalari (ma lento)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateStatsScalar(ReadOnlySpan<float> rewards)
    {
        Visits++;
        Rewards[0] += rewards[0];
        Rewards[1] += rewards[1];
        Rewards[2] += rewards[2];
        Rewards[3] += rewards[3];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearRewards()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float>.Zero.Store((float*)Unsafe.AsPointer(ref Rewards[0]));
        }
        else
        {
            Rewards[0] = 0; Rewards[1] = 0; Rewards[2] = 0; Rewards[3] = 0;
        }
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