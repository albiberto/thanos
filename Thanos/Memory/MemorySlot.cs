using System.Runtime.InteropServices;
using Thanos.Memory.Pools;
using Thanos.SourceGen;
using Thanos.War;
using Thanos.War.Grid;
using Thanos.War.Memory.Views;
using Thanos.War.Snake;

namespace Thanos.Memory;

/// <summary>
///     Fornisce una vista unificata su uno slot di memoria che è fisicamente
///     diviso in una regione "Hot" (accesso frequente) e "Cold" (accesso sporadico).
/// </summary>
public readonly ref struct MemorySlot
{
    private readonly ref readonly GameContext _context;

    private readonly ref readonly MemoryLayout _memoryLayout;
    private readonly ref readonly ColdMemoryLayout _coldMemoryLayout;

    // =================================================================
    // COSTRUTTORE 
    // =================================================================
    public MemorySlot(Span<byte> hotMemory, Span<byte> coldMemory, in MemoryLayout memoryLayout, in ColdMemoryLayout coldMemoryLayout, in GameContext context)
    {
        _memoryLayout = ref memoryLayout;
        _coldMemoryLayout = ref coldMemoryLayout;

        _context = ref context;

        HeadersMemory = hotMemory.Slice(_memoryLayout.HeadersOffset, _memoryLayout.HeadersTotalSize);
        WarGridMemory = hotMemory.Slice(_memoryLayout.GridOffset, _memoryLayout.GridTotalSize);

        BodiesMemory = MemoryMarshal.Cast<byte, ushort>(coldMemory[.._coldMemoryLayout.SlotSize]);
    }

    // =================================================================
    // MEMORY HELPERS
    // =================================================================

    private Span<byte> HeadersMemory { get; }
    private Span<byte> WarGridMemory { get; }

    private Span<ushort> BodiesMemory { get; }

    // =================================================================
    // MEMORY VIEWS
    // =================================================================

    public WarArena Arena
    {
        get
        {
            var grid = new WarGrid(new WarGridMemoryView(WarGridMemory, in _memoryLayout.WarGridMemoryLayout));
            var snakes = new WarSnakes(HeadersMemory, BodiesMemory, in _memoryLayout, in _coldMemoryLayout, in _context);

            return new WarArena(grid, snakes);
        }
    }

    // =================================================================
    // Initializers
    // =================================================================

    public void CloneFrom(in MemorySlot source) => source._slotMemory.CopyTo(_slotMemory);

    public void InitializeFromRequest(in Request request)
    {
        var snakesBitboard = InitializeWarGrid(WarGridMemory, in _memoryLayout.WarGridMemoryLayout, request.Board.Food, request.Board.Hazards, _context.Width);

        InitializeWarSnakes(WarSnakesMemory, in layout.WarSnake, snakesBitboard, request.Board.Snakes, _context.Capacity, _context.SnakeIdMap);
    }

    // =================================================================
    // Static Initializers (Pure Functions)
    // =================================================================

    private static Bitboard InitializeWarGrid(Span<byte> memory, in WarGridMemoryLayout layout, ReadOnlySpan<Coordinate> food, ReadOnlySpan<Coordinate> hazards, int width)
    {
        memory.Clear();

        var view = new WarGridMemoryView(memory, in layout);

        var foodBitboard = view.Food;
        foreach (var coord in food) foodBitboard.Set(To1D(coord, width));

        var hazardsBitboard = view.Hazards;
        foreach (var coord in hazards) hazardsBitboard.Set(To1D(coord, width));

        return view.Snakes;
    }

    private static void InitializeWarSnakes(Span<byte> hotMemory, Span<ushort> coldMemory, in MemoryLayout memoryLayout, in ColdMemoryLayout coldMemoryLayout, ref Bitboard snakesBitboard, in Request request, int width, Dictionary<string, int> snakeIdMap)
    {
        var warSnakes = new WarSnakes(hotMemory, coldMemory, in memoryLayout, in coldMemoryLayout, null);
        var me = warSnakes.Me;

        me.Health.PlacementNew(request.You.Health);
        me.Anatomy.PlacementNew(request.You.Length);

        var body = request.You.Body.AsSpan();
        foreach (var coord in request.You.Body.AsSpan())
        {
            var coord1D = To1D(coord, width);
            var destinationIndex = body.Length - 1 - i;

            bodyMemory[destinationIndex] = coord1D;
            snakesBitboard.Set(coord1D);
        }

        foreach (var snakes in new)
        {
        }

        // foreach (var snake in request.Board.Snakes.AsSpan())
        // {
        //     var index = snakeIdMap[snake.Id];
        //     
        //     
        //     
        //     var body = snake.Body.AsSpan();
        //
        //     var bodyMemory = view.GetBody();
        //     for (var i = 0; i < body.Length; i++)
        //     {
        //         var coord1D = To1D(body[i], width);
        //
        //         var destinationIndex = body.Length - 1 - i;
        //
        //         bodyMemory[destinationIndex] = coord1D;
        //         snakesBitboard.Set(coord1D);
        //     }
        // }
    }

    /// <summary>
    ///     A dedicated utility class for coordinate transformations.
    /// </summary>
    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);
}