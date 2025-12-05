namespace Thanos.Memory;

public readonly struct MemoryBlock(int offset, int length)
{
    /// <summary>
    /// Number of contained elements (T).
    /// </summary>
    public readonly int Length = length;
    
    /// <summary>
    /// Offset in bytes from the beginning of the memory block / base pointer.
    /// </summary>
    public readonly int Offset = offset;
}