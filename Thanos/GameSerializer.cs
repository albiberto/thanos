// Thanos/ThanosDeserializer.cs

using System.Runtime.InteropServices;
using System.Text.Json;
using Thanos;

public static unsafe class ThanosDeserializer
{
    private static void* _memoryBlock;

    /// <summary>
    /// Deserializza la stringa JSON in un singolo blocco di memoria contigua.
    /// </summary>
    /// <returns>Un puntatore al BattleArena inizializzato.</returns>
    public static BattleArena* Deserialize(string json)
    {
        // Evita memory leak se chiamato più volte
        if (_memoryBlock != null)
        {
            NativeMemory.Free(_memoryBlock);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // =======================================================
        // === LOGICA 1: CALCOLO DIMENSIONE MEMORIA
        // =======================================================
        uint totalSize = CalculateMemorySize(root);

        // =======================================================
        // === LOGICA 2: ALLOCAZIONE
        // =======================================================
        _memoryBlock = NativeMemory.Alloc(totalSize);
        byte* cursor = (byte*)_memoryBlock;
        
        // =======================================================
        // === LOGICA 3: POPOLAMENTO
        // =======================================================
        var arena = PopulateState(root, ref cursor);
        
        // Alla fine, il blocco di memoria è pronto e l'arena è utilizzabile.
        return arena;
    }

    /// <summary>
    /// Calcola la dimensione totale necessaria per l'intero stato del gioco.
    /// </summary>
    private static uint CalculateMemorySize(JsonElement root)
    {
        var boardJson = root.GetProperty("board");
        int width = boardJson.GetProperty("width").GetInt32();
        int height = boardJson.GetProperty("height").GetInt32();
        int gridSizeBytes = width * height;
        
        var snakesJson = boardJson.GetProperty("snakes");
        uint snakesSize = 0;
        foreach (var snakeJson in snakesJson.EnumerateArray())
        {
            // La capacità del corpo è la dimensione massima possibile, per sicurezza.
            int capacity = width * height; 
            snakesSize += BattleSnake.HeaderSize + (uint)(capacity * sizeof(ushort));
        }
        
        // Spazio per array temporanei di coordinate (food, hazards, corpi dei serpenti)
        uint tempCoordsSize = (uint)(boardJson.GetProperty("food").GetArrayLength() * sizeof(ushort) +
                                     boardJson.GetProperty("hazards").GetArrayLength() * sizeof(ushort));
        foreach (var snakeJson in snakesJson.EnumerateArray())
        {
            tempCoordsSize += (uint)snakeJson.GetProperty("body").GetArrayLength() * sizeof(ushort);
        }
        
        return (uint)sizeof(BattleArena) +
               (uint)sizeof(Game) +
               (uint)sizeof(Board) +
               (uint)gridSizeBytes +
               snakesSize +
               tempCoordsSize;
    }

    /// <summary>
    /// Popola le strutture dati nel blocco di memoria pre-allocato.
    /// </summary>
    private static BattleArena* PopulateState(JsonElement root, ref byte* cursor)
    {
        var boardJson = root.GetProperty("board");
        var gameJson = root.GetProperty("game");
        int width = boardJson.GetProperty("width").GetInt32();
        int height = boardJson.GetProperty("height").GetInt32();
        int snakesCount = boardJson.GetProperty("snakes").GetArrayLength();

        // 1. Posiziona le struct principali
        BattleArena* arena = (BattleArena*)cursor;
        cursor += sizeof(BattleArena);
        
        Game* game = (Game*)cursor;
        cursor += sizeof(Game);
        
        Board* board = (Board*)cursor;
        cursor += sizeof(Board);

        // 2. Posiziona la griglia e i serpenti
        byte* gridMemory = cursor;
        cursor += width * height;
        
        BattleSnake* snakesArrayStart = (BattleSnake*)cursor;
        // Il cursore per i serpenti verrà avanzato nel metodo di popolamento
        
        ushort* tempCoordsMemory = (ushort*) (snakesArrayStart + snakesCount * (width * height)); // Stima
        
        // 3. Inizializza l'Arena principale
        *arena = new BattleArena
        {
            Game = game,
            Board = board,
            Turn = root.GetProperty("turn").GetInt32(),
            SnakesCount = snakesCount,
            Width = width,
            Height = height
        };
        
        // 4. Popola le strutture usando PlacementNew
        *game = new Game(gameJson);
        
        board->Snakes = snakesArrayStart;
        BattleField.PlacementNew(&board->Field, gridMemory);

        // 5. Popola gli array di coordinate temporanei
        var foodPtr = tempCoordsMemory;
        int foodCount = JsonCoordsToUshorts(boardJson.GetProperty("food"), foodPtr, width);
        var hazardsPtr = foodPtr + foodCount;
        int hazardCount = JsonCoordsToUshorts(boardJson.GetProperty("hazards"), hazardsPtr, width);
        var tempSnakeBodyPtr = hazardsPtr + hazardCount;

        // 6. Popola i serpenti (rispettando la regola "you" è il primo)
        PopulateSnakes(root, board->Snakes, tempSnakeBodyPtr, width, height);

        // 7. Applica food e hazards alla griglia
        board->Field.ApplyFoods(foodPtr, foodCount);
        board->Field.ApplyHazards(hazardsPtr, hazardCount);

        return arena;
    }

    /// <summary>
    /// Converte un array JSON di coordinate {x, y} in un array 1D di ushort.
    /// </summary>
    private static int JsonCoordsToUshorts(JsonElement coordsJson, ushort* dest, int width)
    {
        int count = 0;
        foreach (var coordJson in coordsJson.EnumerateArray())
        {
            int x = coordJson.GetProperty("x").GetInt32();
            int y = coordJson.GetProperty("y").GetInt32();
            dest[count++] = (ushort)(y * width + x);
        }
        return count;
    }

    /// <summary>
    /// Popola tutti i serpenti, assicurandosi che "you" sia in prima posizione.
    /// </summary>
    private static void PopulateSnakes(JsonElement root, BattleSnake* snakesArrayStart, ushort* tempBodyMemory, int width, int height)
    {
        var snakesJson = root.GetProperty("board").GetProperty("snakes");
        var youJson = root.GetProperty("you");
        string youId = youJson.GetProperty("id").GetString()!;

        byte* currentSnakeCursor = (byte*)snakesArrayStart;
        
        // Prima "you"
        var snakePtr = (BattleSnake*)currentSnakeCursor;
        int bodyLength = JsonCoordsToUshorts(youJson.GetProperty("body"), tempBodyMemory, width);
        BattleSnake.PlacementNew(
            snakePtr,
            youJson.GetProperty("health").GetInt32(),
            bodyLength,
            width * height, // capacity
            tempBodyMemory
        );
        currentSnakeCursor += BattleSnake.HeaderSize + (uint)((width * height) * sizeof(ushort));
        tempBodyMemory += bodyLength;

        // Poi gli altri
        foreach (var snakeJson in snakesJson.EnumerateArray())
        {
            if (snakeJson.GetProperty("id").GetString() == youId) continue;
            
            snakePtr = (BattleSnake*)currentSnakeCursor;
            bodyLength = JsonCoordsToUshorts(snakeJson.GetProperty("body"), tempBodyMemory, width);
            BattleSnake.PlacementNew(
                snakePtr,
                snakeJson.GetProperty("health").GetInt32(),
                bodyLength,
                width * height, // capacity
                tempBodyMemory
            );
            currentSnakeCursor += BattleSnake.HeaderSize + (uint)((width * height) * sizeof(ushort));
            tempBodyMemory += bodyLength;
        }
    }
}