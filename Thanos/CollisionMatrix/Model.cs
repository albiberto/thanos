using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Numerics;
using System.IO.Hashing;

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
    public static (byte x, byte y) ToCoords(ushort index, int width) => 
        ((byte)(index % width), (byte)(index / width));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPosition(int x, int y, int width, int height) =>
        x >= 0 && x < width && y >= 0 && y < height;
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
    public Board* BoardPtr;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(byte width, byte height)
    {
        Width = width;
        Height = height;
        TotalCells = (ushort)(width * height);
        Turn = 0;
        SnakeCount = FoodCount = HazardCount = YouIndex = 0;
        BoardPtr = MemoryPool.GetBoard();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Snake You() => ref Snakes[YouIndex];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in GameState source)
    {
        // Copia struct header
        this = source;
        
        // Copia arrays usando Span (JIT ottimizzerà in SIMD)
        if (SnakeCount > 0)
        {
            new ReadOnlySpan<Snake>(source.Snakes, SnakeCount)
                .CopyTo(new Span<Snake>(Snakes, SnakeCount));
        }
        
        if (FoodCount > 0)
        {
            new ReadOnlySpan<ushort>(source.FoodPositions, FoodCount)
                .CopyTo(new Span<ushort>(FoodPositions, FoodCount));
        }
        
        if (HazardCount > 0)
        {
            new ReadOnlySpan<ushort>(source.HazardPositions, HazardCount)
                .CopyTo(new Span<ushort>(HazardPositions, HazardCount));
        }
        
        // Copia snake bodies
        CopySnakeBodies(in source);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CopySnakeBodies(in GameState source)
    {
        ushort* destBody = SnakeBodies;
        ushort* srcBody = source.SnakeBodies;
        
        for (int i = 0; i < SnakeCount; i++)
        {
            ref Snake snake = ref Snakes[i];
            if (snake.Length > 0)
            {
                // Usa Span per copia ottimizzata dal JIT
                new ReadOnlySpan<ushort>(srcBody, snake.Length)
                    .CopyTo(new Span<ushort>(destBody, snake.Length));
                
                snake.Body = destBody;
                destBody += snake.Length;
                srcBody += snake.Length;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetHash()
    {
        uint hash = 2166136261u;
        
        // Hash metadata
        hash ^= (uint)(Width | (Height << 8) | (Turn << 16) | (SnakeCount << 24));
        hash *= 16777619u;
        
        // Hash snakes con CRC32 hardware
        for (int i = 0; i < SnakeCount; i++)
        {
            hash ^= Snakes[i].GetHash();
            hash *= 16777619u;
        }
        
        // Hash food positions con CRC32 se ci sono abbastanza elementi
        if (FoodCount > 4)
        {
            var foodBytes = new ReadOnlySpan<byte>(FoodPositions, FoodCount * sizeof(ushort));
            hash ^= Crc32.HashToUInt32(foodBytes);
            hash *= 16777619u;
        }
        else if (FoodCount > 0)
        {
            // Per pochi elementi, hash diretto
            for (int i = 0; i < FoodCount; i++)
            {
                hash ^= FoodPositions[i];
                hash *= 16777619u;
            }
        }
        
        return hash;
    }
    
    // Rimosso HashArray - ora usiamo CRC32 direttamente
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
    public uint GetHash()
    {
        if (Length == 0) return BodyHash = 0;
        
        // CRC32 hardware accelerato - 10-20x più veloce di FNV-1a
        var bodyBytes = new ReadOnlySpan<byte>(Body, Length * sizeof(ushort));
        return BodyHash = Crc32.HashToUInt32(bodyBytes);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ushort position)
    {
        // Per snake piccoli, loop lineare resta imbattibile
        if (Length < Vector<ushort>.Count)
        {
            for (int j = 0; j < Length; j++)
            {
                if (Body[j] == position) return true;
            }
            return false;
        }
        
        // SIMD path per snake grandi
        var bodySpan = new ReadOnlySpan<ushort>(Body, Length);
        var searchVector = new Vector<ushort>(position);
        
        // Confronta blocchi vettoriali
        int i = 0;
        for (; i <= Length - Vector<ushort>.Count; i += Vector<ushort>.Count)
        {
            var currentVector = new Vector<ushort>(bodySpan.Slice(i));
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
/// Board di gioco allineato in memoria
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 10240)]
public unsafe struct Board
{
    private fixed byte _cells[10240]; // Extra padding per allineamento
    
    public byte* Cells => (byte*)Unsafe.AsPointer(ref _cells[0]);
    
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Cells[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Cells[index] = value;
    }
    
    public byte this[int x, int y, int width]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Cells[y * width + x];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Cells[y * width + x] = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int width, int height)
    {
        int bytes = width * height;
        new Span<byte>(Cells, bytes).Clear();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFull()
    {
        new Span<byte>(Cells, 10000).Clear();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Board* other, int width, int height)
    {
        int bytes = width * height;
        return new ReadOnlySpan<byte>(Cells, bytes)
            .SequenceEqual(new ReadOnlySpan<byte>(other->Cells, bytes));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(Board* source, int width, int height)
    {
        int bytes = width * height;
        new ReadOnlySpan<byte>(source->Cells, bytes)
            .CopyTo(new Span<byte>(Cells, bytes));
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
    private static readonly Board* _board;
    
    // Buffer backup per rollback
    private static readonly Snake* _snakesBackup;
    private static readonly ushort* _foodBackup;
    private static readonly ushort* _hazardsBackup;
    private static readonly ushort* _bodiesBackup;
    private static readonly Board* _boardBackup;
    
    // Costanti
    private const int MAX_SNAKES = 16;
    private const int MAX_FOOD = 200;
    private const int MAX_HAZARDS = 500;
    private const int MAX_BODY_SEGMENTS = 2000;
    private const int ALIGNMENT = 64;
    
    // Stats
    private static int _currentWidth = 11;
    private static int _currentHeight = 11;
    
    static MemoryPool()
    {
        // Alloca buffer allineati
        _snakes = (Snake*)NativeMemory.AlignedAlloc((nuint)(MAX_SNAKES * sizeof(Snake)), ALIGNMENT);
        _food = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_FOOD * sizeof(ushort)), ALIGNMENT);
        _hazards = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_HAZARDS * sizeof(ushort)), ALIGNMENT);
        _bodies = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_BODY_SEGMENTS * sizeof(ushort)), ALIGNMENT);
        _board = (Board*)NativeMemory.AlignedAlloc((nuint)sizeof(Board), ALIGNMENT);
        
        _snakesBackup = (Snake*)NativeMemory.AlignedAlloc((nuint)(MAX_SNAKES * sizeof(Snake)), ALIGNMENT);
        _foodBackup = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_FOOD * sizeof(ushort)), ALIGNMENT);
        _hazardsBackup = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_HAZARDS * sizeof(ushort)), ALIGNMENT);
        _bodiesBackup = (ushort*)NativeMemory.AlignedAlloc((nuint)(MAX_BODY_SEGMENTS * sizeof(ushort)), ALIGNMENT);
        _boardBackup = (Board*)NativeMemory.AlignedAlloc((nuint)sizeof(Board), ALIGNMENT);
        
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
    public static Board* GetBoard() => _board;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDimensions(int width, int height)
    {
        _currentWidth = width;
        _currentHeight = height;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reset()
    {
        // Usa Span.Clear che il JIT ottimizza in memset SIMD
        new Span<byte>(_snakes, MAX_SNAKES * sizeof(Snake)).Clear();
        new Span<byte>(_food, MAX_FOOD * sizeof(ushort)).Clear();
        new Span<byte>(_hazards, MAX_HAZARDS * sizeof(ushort)).Clear();
        new Span<byte>(_bodies, MAX_BODY_SEGMENTS * sizeof(ushort)).Clear();
        _board->Clear(_currentWidth, _currentHeight);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateBackup(int snakeCount, int foodCount, int hazardCount)
    {
        // Backup solo i dati necessari
        if (snakeCount > 0)
        {
            new ReadOnlySpan<Snake>(_snakes, snakeCount)
                .CopyTo(new Span<Snake>(_snakesBackup, snakeCount));
        }
        
        if (foodCount > 0)
        {
            new ReadOnlySpan<ushort>(_food, foodCount)
                .CopyTo(new Span<ushort>(_foodBackup, foodCount));
        }
        
        if (hazardCount > 0)
        {
            new ReadOnlySpan<ushort>(_hazards, hazardCount)
                .CopyTo(new Span<ushort>(_hazardsBackup, hazardCount));
        }
        
        // Stima conservativa per bodies
        int bodyBytes = snakeCount * 20 * sizeof(ushort);
        new ReadOnlySpan<byte>(_bodies, bodyBytes)
            .CopyTo(new Span<byte>(_bodiesBackup, bodyBytes));
        
        _boardBackup->CopyFrom(_board, _currentWidth, _currentHeight);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestoreBackup(int snakeCount, int foodCount, int hazardCount)
    {
        if (snakeCount > 0)
        {
            new ReadOnlySpan<Snake>(_snakesBackup, snakeCount)
                .CopyTo(new Span<Snake>(_snakes, snakeCount));
        }
        
        if (foodCount > 0)
        {
            new ReadOnlySpan<ushort>(_foodBackup, foodCount)
                .CopyTo(new Span<ushort>(_food, foodCount));
        }
        
        if (hazardCount > 0)
        {
            new ReadOnlySpan<ushort>(_hazardsBackup, hazardCount)
                .CopyTo(new Span<ushort>(_hazards, hazardCount));
        }
        
        int bodyBytes = snakeCount * 20 * sizeof(ushort);
        new ReadOnlySpan<byte>(_bodiesBackup, bodyBytes)
            .CopyTo(new Span<byte>(_bodies, bodyBytes));
        
        _board->CopyFrom(_boardBackup, _currentWidth, _currentHeight);
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
        _state->BoardPtr = MemoryPool.GetBoard();
        
        MemoryPool.SetDimensions(width, height);
        MemoryPool.Reset();
    }
    
    public static GameState* State 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state;
    }
    
    public static Board* Board 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _state->BoardPtr;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Snake You() => ref _state->You();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Backup()
    {
        MemoryPool.CreateBackup(_state->SnakeCount, _state->FoodCount, _state->HazardCount);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Restore()
    {
        MemoryPool.RestoreBackup(_state->SnakeCount, _state->FoodCount, _state->HazardCount);
    }
    
    /// <summary>
    /// Esempio di utilizzo ottimizzato
    /// </summary>
    public static void ProcessMove(Direction dir)
    {
        var state = State;
        var board = Board;
        ref var you = ref You();
        
        // Calcola nuova posizione (no LUT!)
        ushort newHead = Movement.Move(you.Head, dir, state->Width, state->Height);
        
        if (Movement.IsValid(newHead))
        {
            // Verifica collisioni usando accesso diretto
            if (board->Cells[newHead] == 0) // Cella vuota
            {
                you.Head = newHead;
                // ... resto della logica
            }
        }
    }
}