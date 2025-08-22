using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly struct GameContext
{
    // Proprietà pubbliche (invariate)
    public readonly int MyId; 
    public readonly int Width;
    public readonly int Height;
    public readonly int Area;
    public readonly int InitialSnakeCount;
    public readonly MemoryLayout Layout;

    /// <summary>
    /// Proprietà statica che rappresenta il peggior contesto di gioco possibile.
    /// Utile per calcolare la dimensione massima del MemoryPool all'avvio.
    /// </summary>
    public static GameContext Worst { get; } = new(-1, Constants.MaxWidth, Constants.MaxHeight, Constants.MaxSnakeCount);

    /// <summary>
    /// Costruttore pubblico per creare un contesto da una Request.
    /// </summary>
    public GameContext(in Request request, Dictionary<string, int> snakeIdMap) : this(snakeIdMap[request.You.Id], request.Board.Width, request.Board.Height, request.Board.SnakeCount)
    {
    }

    /// <summary>
    /// Costruttore privato che esegue l'inizializzazione vera e propria.
    /// </summary>
    private GameContext(int myIntId, int width, int height, int initialSnakeCount)
    {
        MyId = myIntId;
        Width = width;
        Height = height;
        Area = width * height;
        InitialSnakeCount = initialSnakeCount;
        Layout = new MemoryLayout(Area, InitialSnakeCount);
    }
}