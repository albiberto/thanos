namespace Thanos.Model;

public class SnakeOptimized
{
    public string Id { get; set; } = "";
    public int Health { get; set; }
    public int[] BodyIndices { get; set; } = []; // <-- Array di indici!
    public int HeadIndex { get; set; }           // <-- Indice singolo!
    public int Length { get; set; }
}

public class MoveRequestOptimized
{
    public int Turn { get; set; }
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    public int[] FoodIndices { get; set; } = [];
    public int[] HazardIndices { get; set; } = [];
    public SnakeOptimized[] Snakes { get; set; } = [];
    public SnakeOptimized You { get; set; } = new();
}