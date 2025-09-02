using System.Runtime.InteropServices;

namespace Thanos.War.Snake;

[StructLayout(LayoutKind.Sequential)]
public struct Health(int health)
{
    public int HealthPoints { get; private set; } = health;

    public bool IsDead => HealthPoints <= 0;

    public void FullCure() => HealthPoints = 100;
    public void Damage(int amount) => HealthPoints -= amount;
    public void Kill() => HealthPoints = 0;
}