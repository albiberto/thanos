using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public ref struct SnakeHealth
{
    private const byte FullHealth = 100; 
    
    public byte Points { get; private set; }

    public void PlacementNew(byte points) => Points = points;

    public readonly bool IsDead => Points <= 0;

    public void FullCure() => Points = FullHealth;
    
    public void Damage(byte amount)
    {
        if (Points > amount)
            Points -= amount;
        else
            Points = 0;
    }
    
    public void Kill() => Points = 0;
}