using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public struct Health(int health)
{
    private int _health = health;
    
    public int HealthPoints => _health;
    
    public bool IsDead => _health <= 0;
    
    public void FullCure() => _health = 100;
    public void Damage(int amount) => _health -= amount;
    public void Kill() => _health = 0;
}