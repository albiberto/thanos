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

public readonly ref struct MemorySlot(Span<byte> slotMemory, in GameContext context)
{
    private readonly Span<byte> _slotMemory = slotMemory;
    private readonly GameContext _context = context;
    
    // =================================================================
    // Views
    // =================================================================

    public WarArena GetWarArena()
    {
        return new WarArena(
            ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(_slotMemory.Slice(_context.Layout.Offsets.Arena, _context.Layout.WarArena.Header))),
            new WarGrid(new WarGridMemoryView(_slotMemory.Slice(_context.Layout.Offsets.Grid, _context.Layout.WarGrid.Size), in _context.Layout.WarGrid)),
            new WarSnakes(new WarSnakeMemoryView(_slotMemory.Slice(_context.Layout.Offsets.Snakes, _context.Layout.WarSnake.Stride * _context.SnakesCount), in _context.Layout.WarSnake), _context.SnakesCount)
        );
    }
    
    public Node GetNode() => MemoryMarshal.Read<Node>(_slotMemory.Slice(_context.Layout.Offsets.Node, _context.Layout.Node.Size));

    // =================================================================
    // Initializers
    // =================================================================
    
    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);
    
    public void InitializeFromRequest(in Request request)
    {
        var width = request.Board.Width;
        var layout = _context.Layout;
        
        var snakeIdMap = _context.SnakeIdMap;
        var initialSnakes = _context.SnakesCount;
        var capacity = _context.Capacity;
        
        var neighbors = _context.Neighbors;
        
        var nodeMemory = _slotMemory.Slice(layout.Offsets.Node, layout.Node.Size);
        InitializeNodeMemory(nodeMemory);
        
        var gridMemory = _slotMemory.Slice(layout.Offsets.Grid, layout.WarGrid.Size);
        var snakesBitboard = InitializeWarGrid(gridMemory, in layout.WarGrid, width, request.Board.Food, request.Board.Hazards, neighbors);
        
        var snakesMemory = _slotMemory.Slice(layout.Offsets.Snakes, layout.WarSnake.Stride * initialSnakes);
        InitializeWarSnakes(snakesMemory, in layout.WarSnake, snakesBitboard, request.Board.Snakes, width, capacity, snakeIdMap);
        
        var arenaMemory = _slotMemory.Slice(layout.Offsets.Arena, layout.WarArena.Header);
        InitializeWarArena(arenaMemory, in layout.WarArena, initialSnakes);
    }
    
    // =================================================================
    // Static Initializers (Pure Functions)
    // =================================================================
    
    private static void InitializeNodeMemory(Span<byte> memory)
    {
        
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memory));
        node = new Node();
    }
    
    private static Bitboard InitializeWarGrid(Span<byte> memory, in WarGridMemoryLayout layout, int width, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards, ReadOnlySpan<ushort> neighbors)
    {
        var view = new WarGridMemoryView(memory, in layout);
        
        ref var geography = ref view.Geography;
        geography = new Geography(width, width);
        
        var foodBitboard = view.Food;
        foreach (var coord in food) foodBitboard.Set(To1D(coord, width));

        var hazardsBitboard = view.Hazards;
        foreach (var coord in hazards) hazardsBitboard.Set(To1D(coord, width));
        
        var neighborsBoard = view.NeighborsBoard;
        neighbors.CopyTo(neighborsBoard);

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