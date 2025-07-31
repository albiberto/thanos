using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;

// Snake struct allineata a cache line (64 byte)
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
public unsafe struct Snake
{
    // Body come buffer circolare (48 byte)
    public fixed ushort Body[24]; // x|y packed in 16 bit
    
    // Metadati (16 byte)
    public byte HeadIndex;
    public byte TailIndex;
    public byte Length;
    public byte Health;
    public byte GrowthBuffer; // Crescita pendente
    public byte Id;
    private fixed byte _padding[10];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Coord GetHead()
    {
        fixed (ushort* body = Body)
        {
            var packed = body[HeadIndex];
            return new Coord { X = (byte)(packed & 0xFF), Y = (byte)(packed >> 8) };
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Coord GetTail()
    {
        fixed (ushort* body = Body)
        {
            var packed = body[TailIndex];
            return new Coord { X = (byte)(packed & 0xFF), Y = (byte)(packed >> 8) };
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(int newX, int newY, bool removeTail)
    {
        fixed (ushort* body = Body)
        {
            HeadIndex = (byte)((HeadIndex + 1) & 23); // % 24 con AND
            body[HeadIndex] = (ushort)((newY << 8) | newX);
            
            if (removeTail && GrowthBuffer == 0)
            {
                TailIndex = (byte)((TailIndex + 1) & 23);
            }
            else if (GrowthBuffer > 0)
            {
                GrowthBuffer--;
                Length++;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}