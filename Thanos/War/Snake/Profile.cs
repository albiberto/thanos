using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public struct Profile(int id, int health)
{
    public int Id { get; } = id;
    public int Health { get; private set; } = health;

    public bool Dead => Health <= 0;
    
    public void FullCure() => Health = 100;
    
    public void Damage(int amount) => Health -= amount;
    
    public void Kill() => Health = 0;
}