using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarArena
{
    private WarContext _context;

    private WarField* _field;
    private WarSnake* _snakes;
    
    public uint LiveSnakesCount;
    
    public static void PlacementNew(Span<byte> arenaSpan, in WarContext context, WarSnake* snakes, WarField* field)
    {
        // Ottiene un riferimento all'istanza di WarArena.
        ref var arena = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArena>(arenaSpan));

        // Inizializza i campi.
        arena._context = context;
        arena._snakes = snakes;
        arena._field = field;
        arena.LiveSnakesCount = context.SnakeCount;
    }
}