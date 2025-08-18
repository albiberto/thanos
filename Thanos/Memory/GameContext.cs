using Thanos.SourceGen;

namespace Thanos.Memory;

public readonly struct GameContext
{
    public readonly int MyId; 
    public readonly int Width;
    public readonly int Height;
    public readonly int Area;
    public readonly int InitialSnakeCount;
    public readonly MemoryLayout Layout;
    

    public GameContext(in Request request, Dictionary<string, int> snakeIdMap)
    {
        MyId = snakeIdMap[request.You.Id];
        Width = request.Board.Width;
        Height = request.Board.Height;
        Area = request.Board.Area;
        InitialSnakeCount = request.Board.SnakeCount;
        Layout = new MemoryLayout(Area, InitialSnakeCount);
    }
}