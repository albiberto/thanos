using System.Runtime.CompilerServices;

namespace Thanos.Memory;

public readonly unsafe struct MemoryBlock
{
    public readonly nuint Offset;
    public readonly nuint Length;
    public readonly nuint Next;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MemoryBlock(nuint offset, nuint length, nuint next)
    {
        Offset = offset;
        Length = length;
        Next = next;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count<T>() where T : unmanaged => (int)(Length / (nuint)sizeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock CreateUp64<T>(nuint previousNext, int count = 1) where T : unmanaged => Create<T>(previousNext, count, 64);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock CreateUp32<T>(nuint previousNext, int count = 1) where T : unmanaged => Create<T>(previousNext, count, 32);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock CreateUp16<T>(nuint previousNext, int count = 1) where T : unmanaged => Create<T>(previousNext, count, 16);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBlock CreateUp8<T>(nuint previousNext, int count = 1) where T : unmanaged => Create<T>(previousNext, count, 8);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MemoryBlock Create<T>(nuint previousNext, int count, int alignment) where T : unmanaged
    {
        var size = (nuint)(sizeof(T) * count);
        var start = (previousNext + (nuint)alignment - 1) & ~((nuint)alignment - 1);
        var end = start + size;

        var next = (end + (nuint)alignment - 1) & ~((nuint)alignment - 1);

        return new MemoryBlock(start, size, next);
    }
    
    public static MemoryBlock CreateUp8(nuint start, nuint end) => Create(start, end, 8);
    public static MemoryBlock CreateUp16(nuint start, nuint end) => Create(start, end, 16);
    public static MemoryBlock CreateUp32(nuint start, nuint end) => Create(start, end, 32);
    public static MemoryBlock CreateUp64(nuint start, nuint end) => Create(start, end, 64);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MemoryBlock Create(nuint start, nuint end, int alignment)
    {
        var length = end - start;
        var next = (end + (nuint)alignment - 1) & ~((nuint)alignment - 1);

        return new MemoryBlock(start, length, next);
    }
}