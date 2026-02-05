using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Shared;

/// <summary>
/// A high-performance lookup table for grid adjacency.
/// STRUCTURE: 
/// - Wraps a standard array to allow Heap storage (inside GameContext).
/// - Uses Unsafe/MemoryMarshal for pointer-like access speed (No Bounds Checks).
/// </summary>
public readonly struct NeighborsMatrix
{
    private readonly ushort[] _data;
    public readonly int Length;

    public NeighborsMatrix(ushort[] data)
    {
        _data = data;
        // La lunghezza logica è data / 4 direzioni
        Length = data.Length / 4;
    }

    /// <summary>
    /// Returns the neighbor coordinate given a head position and a move index.
    /// Uses Unsafe logic to skip array bounds checking.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Get(int headIndex, int moveIndex)
    {
        // Formula: index = (head * 4) + move
        // MemoryMarshal.GetArrayDataReference ottiene il puntatore al primo elemento (0 overhead)
        // Unsafe.Add esegue l'aritmetica dei puntatori
        return Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_data), 
            (nint)((headIndex << 2) + moveIndex)
        );
    }

    /// <summary>
    /// Returns a Vector64 (4 ushorts) containing all neighbors for a specific head.
    /// Optimized for SIMD processing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Runtime.Intrinsics.Vector64<ushort> GetAll(int headIndex)
    {
        // Carichiamo 4 ushort (64 bit) in un colpo solo dalla memoria
        ref var address = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_data), 
            (nint)(headIndex << 2)
        );
        
        return Unsafe.ReadUnaligned<System.Runtime.Intrinsics.Vector64<ushort>>(ref Unsafe.As<ushort, byte>(ref address));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutOfBound(ushort coordinate)
    {
        return coordinate == 0xFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort coordinate)
    {
        return coordinate != 0xFFFF;
    }
}