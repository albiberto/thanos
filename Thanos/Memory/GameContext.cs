using System.Numerics;
using Thanos.Enums;

namespace Thanos.Memory;

public readonly struct GameContext
{
    public readonly int Width;
    public readonly int Height;
    public readonly int Area;

    public readonly Dictionary<string, int> SnakeIdMap = new(StringComparer.InvariantCultureIgnoreCase);
    public readonly int SnakesCount;
    public readonly int Capacity;
    
    public readonly ushort[] Neighbors = [];
    public readonly int NeighborsCount;
    
    
    public readonly MemoryLayout Layout;

    /// <summary>
    /// Proprietà statica che rappresenta il peggior contesto di gioco possibile.
    /// Utile per calcolare la dimensione massima del MemoryPool all'avvio.
    /// </summary>
    public static GameContext Worst(int neighborsLenght) => new(Constants.MaxWidth, Constants.MaxSnakesCount, neighborsLenght);

    private GameContext(int width, int snakesCount, int neighborsLenght)
    {
        Width = Height = width;
        Area = width * width;
        
        SnakesCount = snakesCount;
        NeighborsCount = neighborsLenght;
        
        Capacity = (int)Math.Min(BitOperations.RoundUpToPowerOf2((uint)Area), Constants.MaxSnakeBodyCapacity);
        Layout = new MemoryLayout(Capacity, Area, snakesCount, neighborsLenght);
    }
    
    /// <summary>
    /// Costruttore privato che esegue l'inizializzazione vera e propria.
    /// </summary>
    public GameContext(int width, Dictionary<string, int> snakeIdMap, ushort[] neighbors) : this(width, snakeIdMap.Count, neighbors.Length)
    {
        SnakeIdMap = snakeIdMap;
        Neighbors = neighbors;
    }
}