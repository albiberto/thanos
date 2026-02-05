using Thanos.Shared;

namespace Thanos.War.State;

public sealed class GameContext(NeighborsMatrix neighbors, int minFood, int hazardDamage, int foodSpawnChance)
{
    public readonly NeighborsMatrix Neighbors = neighbors;
    
    public readonly int MinFood = minFood;
    public readonly int HazardDamage = hazardDamage;
    public readonly int FoodSpawnChance = foodSpawnChance;

    public readonly int Area = neighbors.Length;
}