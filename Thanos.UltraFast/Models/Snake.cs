using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.UltraFast.Models;

/// <summary>
/// Snake struct compatta e cache-friendly
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Snake
{
    public ushort Head;
    public byte Health;
    public byte Length;
    public uint BodyHash;
    public ushort* Body;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ushort position)
    {
        // Per snake piccoli, loop lineare resta imbattibile
        if (Length < Vector<ushort>.Count)
        {
            for (int j = 0; j < Length; j++)
            {
                if (Body[j] == position) return true;
            }
            return false;
        }
        
        // SIMD path per snake grandi
        var bodySpan = new ReadOnlySpan<ushort>(Body, Length);
        var searchVector = new Vector<ushort>(position);
        
        // Confronta blocchi vettoriali
        int i = 0;
        for (; i <= Length - Vector<ushort>.Count; i += Vector<ushort>.Count)
        {
            var currentVector = new Vector<ushort>(bodySpan[i..]);
            if (Vector.EqualsAny(searchVector, currentVector))
            {
                return true;
            }
        }
        
        // Controlla elementi rimanenti
        for (; i < Length; i++)
        {
            if (Body[i] == position) return true;
        }
        
        return false;
    }
}