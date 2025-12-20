using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Thanos.War.Structures;

public readonly unsafe ref struct Bitboard
{
    private readonly ulong* _ptr;
    private readonly int _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitboard(Span<byte> raw)
    {
        // Unsafe cast: il layout garantisce che 'raw' sia allineato a 16 byte.
        _ptr = (ulong*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(raw));
        _length = raw.Length / 8; // Bytes to Ulongs
    }
    
    // Espone lo span per operazioni non critiche (test, debug)
    public Span<ulong> Buffer 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr, _length);
    }
    
    // Accesso raw allo span di byte originale (utile per copie bulk)
    public ReadOnlySpan<byte> Raw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr, _length * 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // Ottimizzazione specifica per board standard (11x11 = 2 ulongs)
        if (Vector128.IsHardwareAccelerated && _length == 2)
        {
            Vector128<ulong>.Zero.Store(_ptr);
        }
        else
        {
            new Span<ulong>(_ptr, _length).Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D)
    {
        // Branchless set: Indice array + Bitwise Shift
        _ptr[position1D >> 6] |= 1UL << (position1D & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(ushort position1D)
    {
        _ptr[position1D >> 6] &= ~(1UL << (position1D & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D)
    {
        return (_ptr[position1D >> 6] & (1UL << (position1D & 63))) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(ushort position1D)
    {
        return (_ptr[position1D >> 6] & (1UL << (position1D & 63))) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Xor(Bitboard other)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
        {
            var v1 = Vector128.Load(_ptr);
            var v2 = Vector128.Load(other._ptr);
            (v1 ^ v2).Store(_ptr);
        }
        else
        {
            for (var i = 0; i < _length; i++) _ptr[i] ^= other._ptr[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Or(in Bitboard other)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
        {
            var v1 = Vector128.Load(_ptr);
            var v2 = Vector128.Load(other._ptr);
            (v1 | v2).Store(_ptr);
        }
        else
        {
            for (var i = 0; i < _length; i++) _ptr[i] |= other._ptr[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PopCount()
    {
        // Unrolling manuale per 11x11
        if (_length == 2)
        {
            return BitOperations.PopCount(_ptr[0]) + BitOperations.PopCount(_ptr[1]);
        }
        
        var count = 0;
        for (var i = 0; i < _length; i++) count += BitOperations.PopCount(_ptr[i]);
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Bitboard destination)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
        {
            Vector128.Load(_ptr).Store(destination._ptr);
        }
        else
        {
            // Usiamo CopyBlock (memcpy) per velocità raw
            Unsafe.CopyBlock(destination._ptr, _ptr, (uint)_length * 8);
        }
    }
}