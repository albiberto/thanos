namespace Snakes.Core;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class Allocator : IDisposable
{
    readonly byte* bytes;
    readonly int capacity;

    public Allocator(int capacity)
    {
        this.capacity = capacity;
        bytes = (byte*)NativeMemory.AlignedAlloc((nuint)capacity, 8);
        NativeMemory.Clear(bytes, (nuint)capacity);
        
        GC.AddMemoryPressure(capacity);
    }

    public int Allocated { get; private set; }

    public ref T AllocateRef<T>(int count)
        where T : unmanaged
    {
        Debug.Assert(count > 0);
        
        var alignment = AlignmentOf<T>.Value - 1;
        var size = Unsafe.SizeOf<T>() * count;
        
        var start = (Allocated + alignment) & ~alignment;
        var end = start + size;
        
        if (end > capacity)
        {
            throw new OutOfMemoryException();
        }

        Allocated = end;

        return ref Unsafe.As<byte, T>(ref bytes[start]);
    }
    
    public ref T Allocate<T>()
        where T : unmanaged
        => ref AllocateRef<T>(1);
    
    public Span<T> Allocate<T>(int count)
        where T : unmanaged
        => MemoryMarshal.CreateSpan(ref AllocateRef<T>(count), count);

    public void Reset()
    {
        Unsafe.InitBlock(ref bytes[0], 0, (uint)Allocated);
        Allocated = 0;
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(bytes);
        GC.RemoveMemoryPressure(capacity);
        GC.SuppressFinalize(this);
    }

    ~Allocator() 
        => Dispose();
}

public readonly struct RelativePtr<T>(nuint offset)
    where T : unmanaged
{
    public ref T Ref 
        => ref Unsafe.As<RelativePtr<T>, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in this), offset));
}

public static class RelativePtr
{
    extension <T> (ref RelativePtr<T> self)
        where T : unmanaged
    {
        public unsafe void New(Allocator allocator)
        {
            ref var target = ref allocator.Allocate<T>();
            var offset = (nuint)Unsafe.ByteOffset(in Unsafe.As<RelativePtr<T>, T>(ref self), in target);
            self = new RelativePtr<T>(offset);
        }
    }
}

public readonly struct RelativeSpan<T>(nuint offset, int length)
    where T : unmanaged
{
    public Span<T> Span 
        => MemoryMarshal.CreateSpan(ref Unsafe.As<RelativeSpan<T>, T>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in this), offset)), length);
}

public static class RelativeSpan
{
    extension <T> (ref RelativeSpan<T> self)
        where T : unmanaged
    {
        public void New(Allocator allocator, int count)
        {
            ref var target = ref allocator.AllocateRef<T>(count);
            var offset = (nuint)Unsafe.ByteOffset(in Unsafe.As<RelativeSpan<T>, T>(ref self), in target);
            self = new RelativeSpan<T>(offset, count);
        }
    }
}

public interface IClonable<in TParams>
    where TParams : allows ref struct 
{
    void New(Allocator allocator, TParams parameters);
}

public unsafe readonly struct Clonable<T>(T* pointer, int size)
    where T : unmanaged
{
    readonly T* pointer = pointer;
    
    public ref T Ref 
        => ref Unsafe.AsRef<T>(pointer);

    public Clonable<T> Clone(Allocator allocator)
    {
        var result = new Clonable<T>((T*)Unsafe.AsPointer(ref allocator.AllocateRef<byte>(size)), size);
        result.CopyFrom(this);
        
        return result;
    }
    
    public void CopyFrom(Clonable<T> source)
    {
        Debug.Assert(!new Span<byte>(pointer, size).Overlaps(new Span<byte>(source.pointer, size)));
        
        ref var target = ref Unsafe.As<T, byte>(ref Ref);
        ref var src = ref Unsafe.As<T, byte>(ref source.Ref);
        
        Unsafe.CopyBlockUnaligned(ref target, ref src, (uint)size);
    }
}

public static class Clonable
{
    public static unsafe void New<T, TParams> (this ref Clonable<T> self, Allocator allocator, TParams parameters)
        where T : unmanaged, IClonable<TParams>
        where TParams : allows ref struct
    {
        var start = allocator.Allocated;
        
        ref var instance = ref Unsafe.As<byte, T>(ref allocator.AllocateRef<byte>(sizeof(T)));
        instance.New(allocator, parameters);
        
        self = new Clonable<T>((T*)Unsafe.AsPointer(ref instance), allocator.Allocated - start);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct AlignmentOf<T>
    where T : unmanaged
{
    byte padding;
    T value;
        
    public static int Value
        => Unsafe.SizeOf<AlignmentOf<T>>() - Unsafe.SizeOf<T>();
}