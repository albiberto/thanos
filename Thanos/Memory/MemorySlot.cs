using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.MCST;
using Thanos.PreWarm.Memory;
using Thanos.War;
using Thanos.War.Grid;
using Thanos.War.Grid.Memory;
using Thanos.War.Snake;
using Thanos.War.Snake.Memory;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(Span<byte> slotMemory, ref GameContext context, ref Luts luts)
{
    private readonly Span<byte> _slotMemory = slotMemory;

    private readonly ref GameContext _context = ref context;
    private readonly ref Luts _luts = ref luts;
    
    // =================================================================
    // Memory Helpers
    // =================================================================
    
    private Span<byte> WarGridMemory => _slotMemory.Slice(_context.Layout.Offsets.Grid, _context.Layout.WarGrid.Size);
    
    private Span<byte> WarSnakesMemory => _slotMemory.Slice(_context.Layout.Offsets.Snakes, _context.Layout.WarSnake.Stride * _context.SnakesCount);


    // =================================================================
    // Views
    // =================================================================
    
    public General General
    {
        get
        {
            BuildEnvironment(out var grid, out var snakes);
            return new General(grid, snakes);   
        }
    }
    
    public Scout Scout
    {
        get
        {
            BuildEnvironment(out var grid, out var snakes);
            return new Scout(grid, snakes, _luts.ConversionsMap, _luts.PositionalScores);
        }
    }

    private void BuildEnvironment(out WarGrid grid, out WarSnakesMemoryView snakes)
    {
        // 1. Ottiene la vista sulla griglia di gioco.
        var gridView = new WarGridMemoryView(WarGridMemory, in _context.Layout.WarGrid);
        grid = new WarGrid(gridView);
    
        // 2. Ottiene la vista sulla collezione di serpenti.
        snakes = new WarSnakesMemoryView(WarSnakesMemory, ref _context.Layout.WarSnake);
    }

    // =================================================================
    // Initializers
    // =================================================================
    
    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);
    
    public void InitializeFromRequest(in Request request)
    {
        var width = request.Board.Width;
        var layout = _context.Layout;
        
        var snakesBitboard = InitializeWarGrid(WarGridMemory, in layout.WarGrid, width, request.Board.Food, request.Board.Hazards, _context.Neighbors);
        
        InitializeWarSnakes(WarSnakesMemory, in layout.WarSnake, snakesBitboard, request.Board.Snakes, width, _context.Capacity, _context.SnakeIdMap);
    }
    
    // =================================================================
    // Static Initializers (Pure Functions)
    // =================================================================
    
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

                var destinationIndex = body.Length - 1 - i;
    
                bodyMemory[destinationIndex] = coord1D;
                snakesBitboard.Set(coord1D);
            }
        }
    }

    /// <summary>
    /// A dedicated utility class for coordinate transformations.
    /// </summary>
    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}