using Thanos.MCST;
using Thanos.War;

namespace Thanos.Memory;

/// <summary>
/// Contiene tutti i puntatori necessari per "assemblare" un'unità di simulazione.
/// È il "prodotto" restituito dal MemoryPooler.
/// </summary>
public readonly unsafe struct MemoryKit(byte* slotPtr, MemoryLayout* layoutPtr)
{
    public readonly Node* Node = (Node*)(slotPtr + layoutPtr->NodeOffset);
    public readonly WarArena* Arena = (WarArena*)(slotPtr + layoutPtr->ArenaOffset);
    public readonly ulong* FieldData = (ulong*)(slotPtr + layoutPtr->FieldDataOffset);
    public readonly byte* SnakesData = slotPtr + layoutPtr->SnakesDataOffset;
}