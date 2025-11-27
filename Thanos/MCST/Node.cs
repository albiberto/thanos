using System.Runtime.InteropServices;
using Thanos.Common;

namespace Thanos.MCST;

// Layout esplicito per garantire 64 byte esatti
[StructLayout(LayoutKind.Explicit, Size = 64)]
public unsafe struct Node
{
    // --- STATISTICHE (20 bytes) ---
    
    [FieldOffset(0)] 
    public int Visits; 

    // MaxN: Supportiamo fino a 4 serpenti (standard Battlesnake).
    // Usiamo un fixed buffer per evitare allocazioni heap.
    [FieldOffset(4)] 
    public fixed float Rewards[4]; 

    // --- ALBERO (8 bytes) ---
    
    [FieldOffset(20)] 
    public int FirstChildIndex;
    
    [FieldOffset(24)] 
    public int NextSiblingIndex;

    // --- IDENTIFICAZIONE (16 bytes) ---
    
    [FieldOffset(28)] 
    public long Hash; // Zobrist Hash completo
    
    [FieldOffset(36)] 
    public int ParentIndex;

    // --- METADATI & FLAGS (4 bytes) ---

    [FieldOffset(40)] 
    public byte PlayerIndex; // 0-3: Serpenti, 255: Environment (Chance Node)
    
    [FieldOffset(41)] 
    public byte Move; // La mossa che ha portato qui
    
    [FieldOffset(42)]
    public NodeFlags Flags; // Bitmask per stati speciali

    [FieldOffset(43)]
    private byte _padding; // Padding per allineamento

    // --- SPAZIO LIBERO (20 bytes) ---
    // Riservato per future euristiche o RAVE (Rapid Action Value Estimation)
    // Attualmente padding per arrivare a 64 byte.

    public void PlacementRoot(long hash)
    {
        Visits = 0;
        // Azzera Rewards
        for(int i=0; i<4; i++) Rewards[i] = 0;
        
        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = -1;
        PlayerIndex = 0; // Inizia sempre il player 0 o da definire
        Hash = hash;
        Move = Moves.None;
        Flags = NodeFlags.None;
    }

    public void PlacementNew(int parentIndex, byte move, long hash, byte playerIndex, bool isChanceNode)
    {
        Visits = 0;
        for(int i=0; i<4; i++) Rewards[i] = 0;

        FirstChildIndex = -1;
        NextSiblingIndex = -1;
        ParentIndex = parentIndex;
        PlayerIndex = playerIndex;
        Hash = hash;
        Move = move;
        
        Flags = NodeFlags.None;
        if (isChanceNode) Flags |= NodeFlags.ChanceNode;
    }
    
    public bool IsLeafNode => FirstChildIndex == -1;
    
    public bool IsChanceNode => (Flags & NodeFlags.ChanceNode) != 0;
    public bool IsTerminal => (Flags & NodeFlags.Terminal) != 0;
    public bool IsSolvedWin => (Flags & NodeFlags.SolvedWin) != 0;
    public bool IsSolvedLoss => (Flags & NodeFlags.SolvedLoss) != 0;

    public void SetFlag(NodeFlags flag) => Flags |= flag;
}

[Flags]
public enum NodeFlags : byte
{
    None = 0,
    Terminal = 1 << 0,   // Il gioco è finito naturalmente (morte/vittoria)
    ChanceNode = 1 << 1, // È un nodo stocastico (spawn cibo)
    SolvedWin = 1 << 2,  // Vittoria forzata matematicamente
    SolvedLoss = 1 << 3  // Sconfitta forzata matematicamente
}