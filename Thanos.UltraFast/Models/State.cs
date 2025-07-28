using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.UltraFast.Models;

/// <summary>
/// Stato di gioco ottimizzato per cache efficiency
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct State
{
    // Hot data (accesso frequente) - 8 bytes
    public byte Width;
    public byte Height;
    public ushort TotalCells;
    public ushort Turn;
    public byte SnakeCount;
    public byte YouIndex;
    
    // Cold data - 8 bytes
    public byte FoodCount;
    public byte HazardCount;
    private fixed byte _padding[6];
    
    // Puntatori (già allineati) - 40 bytes
    public Snake* Snakes;
    public ushort* FoodPositions;
    public ushort* HazardPositions;
    public ushort* SnakeBodies;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(byte width, byte height)
    {
        Width = width;
        Height = height;
        TotalCells = (ushort)(width * height);
        Turn = 0;
        SnakeCount = FoodCount = HazardCount = YouIndex = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Snake You() => ref Snakes[YouIndex];
}