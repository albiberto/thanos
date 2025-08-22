using System.Numerics;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;
using Thanos.MCST;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(in MemoryLayout layout, Span<byte> slotMemory , int capacity, Dictionary<string, int> snakeIdMap)
{
    private readonly Span<byte> _slotMemory = slotMemory;
    private readonly MemoryLayout _layout = layout;
    
    public void InitializeFromRequest(in Request request)
    {
        InitializeNodeMemory();
        var grid = InitializeWarGrid(request.Board.Width, request.Board.Height, request.Board.Food, request.Board.Hazards);
        InitializeWarSnakes(grid, request.Board.Snakes);
    }
    
    // =================================================================
    // Initialization
    // =================================================================
    
    private void InitializeNodeMemory()
    {
        var nodeMemory = _slotMemory.Slice(Offsets.Node, _layout.Node.Size);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeMemory));
        node = new Node();
    }
    
    private WarGrid InitializeWarGrid(int width, int height, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards)
    {
        var gridMemory = _slotMemory.Slice(_layout.Offsets.Grid, _layout.Grid.Size);
    
        var geographyMemory = gridMemory[.._layout.Grid.GeographySize];
        ref var geography = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Geography>(geographyMemory));
        geography = new Geography(width, height);
    
        // Ottiene il blocco di memoria per TUTTI i bitboard
        var bitboardsMemory = gridMemory.Slice(_layout.Grid.GeographySize, _layout.Grid.BitboardsSize);
    
        // Dividiamo il blocco di byte in 3 segmenti e facciamo il cast di ognuno a ulong
        var stride = _layout.Grid.BitboardStrideInBytes;
    
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

    private void InitializeWarSnakes(WarGrid grid, ReadOnlySpan<Snake> snakes)
    {
        var snakesMemory = _slotMemory.Slice(_layout.Offsets.Snakes, _layout.Snake.Stride * snakeIdMap.Capacity);
    
        for (var i = 0; i < snakes.Length; i++)
        {
            var snake = snakes[i];
            
            var profileMemory = snakesMemory.Slice(i * _layout.Snake.Stride, _layout.Snake.ProfileSize);
            ref var profile = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Profile>(profileMemory));
            profile = new Profile(snakeIdMap[snake.Id]);
            
            var healthMemory = snakesMemory.Slice(i * _layout.Snake.Stride + _layout.Snake.ProfileSize, _layout.Snake.HealthSize);
            ref var health = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(healthMemory));
            health = new Health(snake.Health);
            
            var anatomyMemory = snakesMemory.Slice(i * _layout.Snake.Stride + _layout.Snake.ProfileSize + _layout.Snake.HeaderSize, _layout.Snake.AnatomySize);
            ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(anatomyMemory));
            anatomy = new Anatomy(capacity, snake.Body.Length);
            
            var bodyMemoryByte = snakesMemory.Slice(i * _layout.Snake.Stride + _layout.Snake.HeaderSize, _layout.Snake.BodySize);
            var bodyMemoryUshort = MemoryMarshal.Cast<byte, ushort>(bodyMemoryByte);

            var body = snake.Body.AsSpan();
            for (var j = 0; j < snake.Body.Length; j++)
            {
                var coord1D = To1D(body[j], grid.Geography.Width);
                
                bodyMemoryUshort[j] = coord1D;
                grid.Snakes.Set(coord1D);
            }
        }
    }
    
    // =================================================================
    // Views
    // =================================================================
    
    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);
    
    public static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}