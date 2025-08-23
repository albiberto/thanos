using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.MCST;
using Thanos.War.Arena;
using Thanos.War.Arena.Memory;
using Thanos.War.Grid;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake;
using Thanos.War.Snake.Memory;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(in MemoryLayout layout, Span<byte> slotMemory , int capacity, Dictionary<string, int> snakeIdMap)
{
    private readonly Span<byte> _slotMemory = slotMemory;
    private readonly MemoryLayout _layout = layout;
    
    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);
    
    public void InitializeFromRequest(in Request request)
    {
        var initialSnakes = snakeIdMap.Count;
        
        var nodeMemory = _slotMemory.Slice(_layout.Offsets.Node, _layout.Node.Size);
        InitializeNodeMemory(nodeMemory);
        
        var gridMemory = _slotMemory.Slice(_layout.Offsets.Grid, _layout.Grid.Size);
        var snakesBitboard = InitializeWarGrid(gridMemory, in _layout.Grid, request.Board.Width, request.Board.Height, request.Board.Food, request.Board.Hazards);
        
        var snakesMemory = _slotMemory.Slice(_layout.Offsets.Snakes, _layout.Snake.Stride * initialSnakes);
        InitializeWarSnakes(snakesMemory, in _layout.Snake, snakesBitboard, request.Board.Snakes, request.Board.Width, capacity, snakeIdMap);
        
        var arenaMemory = _slotMemory.Slice(_layout.Offsets.Arena, _layout.Arena.Header);
        InitializeWarArena(arenaMemory, in _layout.Arena, initialSnakes);
    }
    
    // =================================================================
    // Static Initializers (Pure Functions)
    // =================================================================
    
    private static void InitializeNodeMemory(Span<byte> memory)
    {
        
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memory));
        node = new Node();
    }
    
    private static Bitboard InitializeWarGrid(Span<byte> memory, in WarGridMemoryLayout layout, int width, int height, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        var view = new WarGridMemoryView(memory, in layout);
        
        ref var geography = ref view.Geography;
        geography = new Geography(width, height);
        
        var foodBitboard = view.Food;
        foreach (var coord in food) foodBitboard.Set(To1D(coord, width));

        var hazardsBitboard = view.Hazards;
        foreach (var coord in hazards) hazardsBitboard.Set(To1D(coord, width));

        return view.Snakes;
    }

    private static void InitializeWarSnakes(Span<byte> memory, in WarSnakeMemoryLayout memoryLayout, Bitboard snakesBitboard, ReadOnlySpan<Snake> snakes, int width, int capacity, Dictionary<string, int> snakeIdMap)
    {
        foreach (var snake in snakes)
        {
            var index = snakeIdMap[snake.Id];
            var view = new WarSnakeMemoryView(memory, in memoryLayout, index);
            
            ref var profile = ref view.GetProfile();
            profile = new Profile(index);
            
            ref var health = ref view.GetHealth();
            health = new Health(snake.Health);
            
            var body = snake.Body.AsSpan();
            
            ref var anatomy = ref view.GetAnatomy();
            anatomy = new Anatomy(capacity, body.Length);
            
            var bodyMemory = view.GetBody();
            for (var i = 0; i < body.Length; i++)
            {
                var coord1D = To1D(body[i], width);
                
                bodyMemory[i] = coord1D;
                snakesBitboard.Set(coord1D);
            }
        }
    }

    private static void InitializeWarArena(Span<byte> memory, in WarArenaMemoryLayout layout, int liveSnakes)
    {
        var view = new WarArenaMemoryView(memory, in layout);
        
        ref var header = ref view.Header;
        header = new WarArenaHeader(liveSnakes);
    }

    /// <summary>
    /// A dedicated utility class for coordinate transformations.
    /// </summary>
    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}