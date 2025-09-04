using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public struct Health
{
    private const byte FullHealth = 100; 
    
    public byte Points { get; private set; }
    private byte _flags; // Un campo privato per altri flag, se necessario in futuro, mantiene l'allineamento a 2 byte.

    public void PlacementNew(byte startHealth)
    {
        Points = startHealth;
        _flags = 0;
    }

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