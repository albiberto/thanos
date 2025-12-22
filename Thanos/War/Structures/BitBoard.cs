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
        _ptr = (ulong*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(raw));
        _length = raw.Length / 8;
    }
    
    public Span<ulong> Buffer 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr, _length);
    }
    
    public ReadOnlySpan<byte> Raw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr, _length * 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
            Vector128<ulong>.Zero.Store(_ptr);
        else
            new Span<ulong>(_ptr, _length).Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ushort position1D) => _ptr[position1D >> 6] |= 1UL << (position1D & 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unset(ushort position1D) => _ptr[position1D >> 6] &= ~(1UL << (position1D & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(ushort position1D) => (_ptr[position1D >> 6] & (1UL << (position1D & 63))) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUnset(ushort position1D) => (_ptr[position1D >> 6] & (1UL << (position1D & 63))) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Xor(Bitboard other)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
            (Vector128.Load(_ptr) ^ Vector128.Load(other._ptr)).Store(_ptr);
        else
            for (var i = 0; i < _length; i++) _ptr[i] ^= other._ptr[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Or(in Bitboard other)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
            (Vector128.Load(_ptr) | Vector128.Load(other._ptr)).Store(_ptr);
        else
            for (var i = 0; i < _length; i++) _ptr[i] |= other._ptr[i];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AndNot(in Bitboard other)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
            (Vector128.Load(_ptr) & ~Vector128.Load(other._ptr)).Store(_ptr);
        else
            for (var i = 0; i < _length; i++) _ptr[i] &= ~other._ptr[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PopCount()
    {
        if (_length == 2) return BitOperations.PopCount(_ptr[0]) + BitOperations.PopCount(_ptr[1]);
        var count = 0;
        for (var i = 0; i < _length; i++) count += BitOperations.PopCount(_ptr[i]);
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Bitboard destination)
    {
        if (Vector128.IsHardwareAccelerated && _length == 2)
            Vector128.Load(_ptr).Store(destination._ptr);
        else
            Unsafe.CopyBlock(destination._ptr, _ptr, (uint)_length * 8);
    }

    // --- WORLD CHAMPION MOVE: SIMD DILATION (Espansione) ---
    // Espande i bit attivi di 1 passo in tutte le direzioni (UDLR)
    // Ignora i bit presenti in 'barriers'.
    // Scrive il risultato in 'destination'.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dilate(in Bitboard barriers, Bitboard destination)
    {
        // HARDCODED PER 11x11 (121 bits) -> 2 ulongs
        // Questa è la funzione critica per il Voronoi veloce.
        
        ulong b0 = _ptr[0];
        ulong b1 = _ptr[1];
        
        // Spazio libero = ~Muri
        ulong free0 = ~barriers._ptr[0];
        ulong free1 = ~barriers._ptr[1];

        // Maschere per evitare il wrap-around "pac-man" (non saltare da destra a sinistra)
        // NOTA: Calcolate per Width=11
        const ulong notRight0 = 0xFFBFF7FEFFDFFBFF; // Mask colonna 10
        const ulong notRight1 = 0xFEFFDFFBFF7FEFFD;
        const ulong notLeft0  = 0xFF7FEFFDFFBFF7FE; // Mask colonna 0
        const ulong notLeft1  = 0xFFFFBFF7FEFFDFFB;

        // LEFT (-1): Shift Right >> 1
        // Se bit 11 (inizio riga 1) shifta a 10 (fine riga 0), dobbiamo cancellarlo.
        ulong left0 = (b0 >> 1) | (b1 << 63);
        ulong left1 = (b1 >> 1);
        left0 &= notRight0; // Applico maschera per pulire ingressi spuri
        left1 &= notRight1;

        // RIGHT (+1): Shift Left << 1
        ulong right0 = (b0 << 1);
        ulong right1 = (b1 << 1) | (b0 >> 63);
        right0 &= notLeft0; 
        right1 &= notLeft1;

        // UP (+11): Shift Left << 11
        ulong up0 = (b0 << 11);
        ulong up1 = (b1 << 11) | (b0 >> 53); // Carry: 64-11=53

        // DOWN (-11): Shift Right >> 11
        ulong down0 = (b0 >> 11) | (b1 << 53);
        ulong down1 = (b1 >> 11);

        // Combine (OR) & Mask Walls (AND)
        destination._ptr[0] = (up0 | down0 | left0 | right0) & free0;
        destination._ptr[1] = (up1 | down1 | left1 | right1) & free1;
    }
}