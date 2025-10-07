namespace Thanos.PreWarm.Memory;

public readonly unsafe struct LookupPointers(void* neighborsPtr, int neighborsLenght, void* positionalScoresPtr, int positionalScoresLenght, void* conversionMapPtr, int conversionMapLenght)
{
    public readonly void* NeighborsPtr = neighborsPtr;
    public readonly int NeighborsLength = neighborsLenght;

    public readonly void* PositionalScoresPtr = positionalScoresPtr;
    public readonly int PositionalScoresLength = positionalScoresLenght;

    public readonly void* ConversionsMapPtr = conversionMapPtr;
    public readonly int ConversionsMapLength = conversionMapLenght;
}