using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;
// ====================================================================
// SISTEMA UNIVERSALE SCALABILE PER GRIGLIE DI GIOCO - VERSIONE 2024
// ====================================================================
// FILOSOFIA: "Do less, go faster"
// - NO lookup tables (calcolo diretto più veloce fino a 32x32)
// - NO SIMD manuale (il JIT fa meglio)
// - SI buffer pre-allocati e zero allocazioni
// - SI memoria allineata per cache efficiency
// - SI unsafe per accesso diretto quando serve
// ====================================================================

/// <summary>
/// Helper per conversioni coordinate ultra-veloci
/// </summary>
public static class GridMath
{
    /// <summary>Dimensione cache line CPU moderna</summary>
    public const int CACHE_LINE_SIZE = 64;
    
    /// <summary>Dimensione massima griglia supportata</summary>
    public const int MAX_SIZE = 100;
    
    /// <summary>Valore sentinella per posizione invalida</summary>
    public const ushort INVALID = 65535;
    
    // Inline aggressivo per tutti i calcoli matematici
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToIndex(int x, int y, int width) => (ushort)(y * width + x);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (byte x, byte y) ToCoords(ushort index, int width) => ((byte)(index % width), (byte)(index / width));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPosition(int x, int y, int width, int height) => ((uint)x < (uint)width) & ((uint)y < (uint)height);
}

/// <summary>
/// Stato di gioco ottimizzato per cache efficiency
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct GameState
{
    // Hot data (accesso frequente) - 8 bytes
    public byte Width;
    public byte Height;
    public ushort TotalCells;
    public ushort Turn;
    public byte SnakeCount;
    public byte YouIndex;
    
    // Cold data - 8 bytes
    public byte FoodCount;
    public byte HazardCount;
    private fixed byte _padding[6];
    
    // Puntatori (già allineati) - 40 bytes
    public Snake* Snakes;
    public ushort* FoodPositions;
    public ushort* HazardPositions;
    public ushort* SnakeBodies;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(byte width, byte height)
    {
        Width = width;
        Height = height;
        TotalCells = (ushort)(width * height);
        Turn = 0;
        SnakeCount = FoodCount = HazardCount = YouIndex = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Snake You() => ref Snakes[YouIndex];
}

/// <summary>
/// Snake struct compatta e cache-friendly
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Snake
{
    public ushort Head;
    public byte Health;
    public byte Length;
    public uint BodyHash;
    public ushort* Body;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ushort position)
    {
        // Per snake piccoli, loop lineare resta imbattibile
        if (Length < Vector<ushort>.Count)
        {
            for (var j = 0; j < Length; j++)
            {
                if (Body[j] == position) return true;
            }
            return false;
        }
        
        // SIMD path per snake grandi
        var bodySpan = new ReadOnlySpan<ushort>(Body, Length);
        var searchVector = new Vector<ushort>(position);
        
        // Confronta blocchi vettoriali
        var i = 0;
        for (; i <= Length - Vector<ushort>.Count; i += Vector<ushort>.Count)
        {
            var currentVector = new Vector<ushort>(bodySpan[i..]);
            if (Vector.EqualsAny(searchVector, currentVector))
            {
                return true;
            }
        }
        
        // Controlla elementi rimanenti
        for (; i < Length; i++)
        {
            if (Body[i] == position) return true;
        }
        
        return false;
    }
}

/// <summary>
/// Pool di memoria con buffer pre-allocati
/// </summary>
public static unsafe class MemoryPool
{
    // Buffer principali
    private static readonly Snake* _snakes;
    private static readonly ushort* _food;
    private static readonly ushort* _hazards;
    private static readonly ushort* _bodies;
    
    // Costanti
    private const int MAX_SNAKES = 4;
    private const int MAX_FOOD = 200;
    private const int MAX_HAZARDS = 500;
    private const int MAX_BODY_SEGMENTS = 2000;
    private const int ALIGNMENT = 64;
    
    static MemoryPool()
    {
        // Alloca buffer allineati
        _snakes = (Snake*)NativeMemory.AlignedAlloc((nuint)(MAX_SNAKES * sizeof(Snake)), ALIGNMENT);
        _food = (ushort*)NativeMemory.AlignedAlloc(MAX_FOOD * sizeof(ushort), ALIGNMENT);
        _hazards = (ushort*)NativeMemory.AlignedAlloc(MAX_HAZARDS * sizeof(ushort), ALIGNMENT);
        _bodies = (ushort*)NativeMemory.AlignedAlloc(MAX_BODY_SEGMENTS * sizeof(ushort), ALIGNMENT);
        
        Reset();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snake* GetSnakes() => _snakes;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort* GetFood() => _food;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort* GetHazards() => _hazards;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort* GetBodies() => _bodies;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reset()
    {
        // Usa Span.Clear che il JIT ottimizza in memset SIMD
        new Span<byte>(_snakes, MAX_SNAKES * sizeof(Snake)).Clear();
        new Span<byte>(_food, MAX_FOOD * sizeof(ushort)).Clear();
        new Span<byte>(_hazards, MAX_HAZARDS * sizeof(ushort)).Clear();
        new Span<byte>(_bodies, MAX_BODY_SEGMENTS * sizeof(ushort)).Clear();
    }
}

/// <summary>
/// Sistema di movimento con calcolo diretto (no LUT)
/// </summary>
public static class Movement
{
    public const ushort INVALID = GridMath.INVALID;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Move(ushort position, Direction dir, int width, int height)
    {
        var (x, y) = GridMath.ToCoords(position, width);
        
        return dir switch
        {
            Direction.Up => y > 0 ? GridMath.ToIndex(x, y - 1, width) : INVALID,
            Direction.Down => y < height - 1 ? GridMath.ToIndex(x, y + 1, width) : INVALID,
            Direction.Left => x > 0 ? GridMath.ToIndex(x - 1, y, width) : INVALID,
            Direction.Right => x < width - 1 ? GridMath.ToIndex(x + 1, y, width) : INVALID,
            _ => INVALID
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetNeighbors(ushort position, int width, int height, Span<ushort> neighbors)
    {
        var (x, y) = GridMath.ToCoords(position, width);
        
        neighbors[0] = y > 0 ? GridMath.ToIndex(x, y - 1, width) : INVALID;
        neighbors[1] = y < height - 1 ? GridMath.ToIndex(x, y + 1, width) : INVALID;
        neighbors[2] = x > 0 ? GridMath.ToIndex(x - 1, y, width) : INVALID;
        neighbors[3] = x < width - 1 ? GridMath.ToIndex(x + 1, y, width) : INVALID;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ushort position) => position != INVALID;
}

/// <summary>
/// Direzioni di movimento
/// </summary>
public enum Direction : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}

/// <summary>
/// Manager principale del gioco
/// </summary>
public static unsafe class GameManager
{
    private static GameState* _state;
    private static bool _initialized;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Initialize(byte width = 11, byte height = 11)
    {
        if (!_initialized)
        {
            _state = (GameState*)NativeMemory.AlignedAlloc((nuint)sizeof(GameState), GridMath.CACHE_LINE_SIZE);
            _initialized = true;
        }
        
        _state->Initialize(width, height);
        
        // Collega buffer
        _state->Snakes = MemoryPool.GetSnakes();
        _state->FoodPositions = MemoryPool.GetFood();
        _state->HazardPositions = MemoryPool.GetHazards();
        _state->SnakeBodies = MemoryPool.GetBodies();
        
        MemoryPool.Reset();
    }
    
    public static GameState* State 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Snake You() => ref _state->You();
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Snake Snakes() => ref _state->You();

    /// <summary>
    /// Calcola la mossa migliore analizzando lo stato di gioco corrente.
    /// Questo è il cuore della tua AI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction FindBestMove()
    {
        var state = State;
        ref var you = ref You();
        
        // Usiamo uno Span sullo stack per evitare allocazioni heap
        Span<Direction> safeMoves = stackalloc Direction[4];
        var safeMoveCount = 0;

        // Itera su tutte e 4 le direzioni possibili
        for (byte i = 0; i < 4; i++)
        {
            var direction = (Direction)i;
            var nextHead = Movement.Move(you.Head, direction, state->Width, state->Height);
            
            // Step 1: Controlla che la mossa non vada fuori dai muri
            if (!Movement.IsValid(nextHead))
            {
                continue; // Mossa non valida, passa alla prossima direzione
            }
            
            var isSafe = true;

            // Step 2: Controlla la collisione con tutti i serpenti (incluso te stesso)
            for (var s = 0; s < state->SnakeCount; s++)
            {
                ref var snake = ref state->Snakes[s];
                
                // Il controllo del corpo usa il tuo metodo ottimizzato `Contains`
                // NOTA: il corpo di un serpente include la sua testa (tranne l'ultimo segmento di coda, che si sposterà)
                if (snake.Contains(nextHead))
                {
                    // Eccezione: non è una collisione se la posizione è la coda di un serpente
                    // che non ha appena mangiato (perché la coda si sposterà).
                    // Per semplicità, qui consideriamo ogni collisione fatale.
                    isSafe = false;
                    break;
                }
            }
            
            if (!isSafe)
            {
                continue; // Mossa non sicura, passa alla prossima
            }

            // Aggiungi la direzione alle mosse sicure
            safeMoves[safeMoveCount++] = direction;
        }

        if (safeMoveCount == 0)
        {
            // Nessuna mossa sicura! Situazione disperata.
            // Scegli una mossa di default per non andare in errore.
            Console.WriteLine($"Turno {state->Turn}: NESSUNA MOSSA SICURA! Scelgo UP come ultima risorsa.");
            return Direction.Up;
        }

        // --- QUI INSERISCI LA TUA LOGICA AVANZATA ---
        // Al momento, scegliamo una mossa sicura a caso.
        // Potresti sostituirlo con Minimax, ricerca di cibo, controllo delle aree, ecc.
        var chosenMove = safeMoves[Random.Shared.Next(safeMoveCount)];
        
        Console.WriteLine($"Turno {state->Turn}: Mosse sicure: {safeMoveCount}. Scelta: {chosenMove}");
        return chosenMove;
    }
}