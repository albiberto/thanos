using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.MCST;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(in MemoryLayout layout, Span<byte> slotMemory , int capacity, Dictionary<string, int> snakeIdMap)
{
    private readonly Span<byte> _slotMemory = slotMemory;
    private readonly MemoryLayout _layout = layout;
    
    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);
    
    public void InitializeFromRequest(in Request request)
    {
        var nodeMemory = _slotMemory.Slice(_layout.Offsets.Node, _layout.Node.Size);
        InitializeNodeMemory(nodeMemory);
        
        var gridMemory = _slotMemory.Slice(_layout.Offsets.Grid, _layout.Grid.Size);
        var grid = InitializeWarGrid(gridMemory, in _layout.Grid, request.Board.Width, request.Board.Height, request.Board.Food, request.Board.Hazards);
        
        var snakesMemory = _slotMemory.Slice(_layout.Offsets.Snakes, _layout.Snake.Stride * snakeIdMap.Count);
        InitializeWarSnakes(snakesMemory, in _layout.Snake, in grid, request.Board.Snakes, capacity, snakeIdMap);
    }
    
    // =================================================================
    // Static Initializers (Pure Functions)
    // =================================================================
    
    private static void InitializeNodeMemory(Span<byte> memory)
    {
        
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(memory));
        node = new Node();
    }
    
    private static WarGrid InitializeWarGrid(Span<byte> memory, in GridLayout layout, int width, int height, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        var geographyMemory = memory[..layout.GeographySize];
        ref var geography = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Geography>(geographyMemory));
        geography = new Geography(width, height);
    
        // Ottiene il blocco di memoria per TUTTI i bitboard
        var bitboardsMemory = memory.Slice(layout.GeographySize, layout.BitboardsSize);
    
        // Dividiamo il blocco di byte in 3 segmenti e facciamo il cast di ognuno a ulong
        var stride = layout.BitboardStrideInBytes;
    
        var foodMemoryBytes = bitboardsMemory.Slice(0, stride);
        var hazardsMemoryBytes = bitboardsMemory.Slice(stride, stride);
        var snakesMemoryBytes = bitboardsMemory.Slice(stride * 2, stride);

        // Eseguiamo il Cast da Span<byte> a Span<ulong>
        var foodMemoryUlongs = MemoryMarshal.Cast<byte, ulong>(foodMemoryBytes);
        var hazardsMemoryUlongs = MemoryMarshal.Cast<byte, ulong>(hazardsMemoryBytes);
        var snakesMemoryUlongs = MemoryMarshal.Cast<byte, ulong>(snakesMemoryBytes);

        // Creiamo e ritorniamo la vista WarGrid
        var grid = new WarGrid(ref geography, foodMemoryUlongs, hazardsMemoryUlongs, snakesMemoryUlongs);
        
        foreach (var coord in food) grid.Food.Set(To1D(coord, width));
        foreach (var coord in hazards) grid.Hazards.Set(To1D(coord, width));

        return grid;
    }

    private static void InitializeWarSnakes(Span<byte> memory, in SnakeLayout layout, in WarGrid grid, ReadOnlySpan<Snake> snakes, int capacity, Dictionary<string, int> snakeIdMap)
    {
        foreach (var snake in snakes)
        {
            var index = snakeIdMap[snake.Id];
            var view = new WarSnakeMemoryView(memory, in layout, index);
            
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
                var coord1D = To1D(body[i], grid.Geography.Width);
                
                bodyMemory[i] = coord1D;
                grid.Snakes.Set(coord1D);
            }
        }
    }

    /// <summary>
    /// A dedicated utility class for coordinate transformations.
    /// </summary>
    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}