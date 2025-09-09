public readonly unsafe struct LutPointers(void* nPtr, int nLen, void* pPtr, int pLen, void* cPtr, int cLen)
{
    public readonly void* NeighborsPtr = nPtr;
    public readonly int NeighborsLength = nLen; // Lunghezza in numero di elementi, non in byte

    public readonly void* PositionalScoresPtr = pPtr;
    public readonly int PositionalScoresLength = pLen;

    public readonly void* ConversionsMapPtr = cPtr;
    public readonly int ConversionsMapLength = cLen;
}