using System.Runtime.CompilerServices;
using Thanos.Domain;
using Thanos.Enums;
using Thanos.Extensions;
using Thanos.Model;

namespace Thanos;

public class MonteCarlo
{
    // Matrice globale riutilizzabile - zero allocazioni
    private static readonly bool[,] _collisionMatrix = new bool[19, 19];
    private static readonly Direction[] _validMoves = new Direction[4];

    public static double UpScore;
    public static double DownScore;
    public static double LeftScore;
    public static double RightScore;

    private static readonly (int, int)[] _vectors = [(0, 1), (0, -1), (-1, 0), (1, 0)];
    private readonly int[] _moveTaskCounts = new int[4]; // Pre-alloca per 4 direzioni max

    // Valori pre-computati di √(ln(n)) per ottimizzare il calcolo UCT nelle simulazioni Monte Carlo.
    private readonly double[] _precomputedSqrtLog;

    // Buffer di task (Mosse X Core) pre-allocato per evitare allocazioni dinamiche
    // 16 task per core, espandibile se necessario, un overkill
    private readonly Task<double>[] _taskBuffer = new Task<double>[Environment.ProcessorCount * 16];

    public MonteCarlo(double[] precomputedSqrtLog)
    {
        Console.WriteLine($"🚀 TIME-BASED Monte Carlo: {PerformanceConfig.SimulationTimeMs}ms per move");

        _precomputedSqrtLog = precomputedSqrtLog;

        Console.WriteLine($"✅ Ready! Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
    }

    /// <summary>
    ///     Implementa un algoritmo Monte Carlo Tree Search (MCTS) ottimizzato per Battlesnake.
    ///     Il metodo esegue una ricerca strutturata in 5 fasi:
    ///     1. Valutazione sicurezza mosse
    ///     2. Inizializzazione strutture dati parallele
    ///     3. Esplorazione iniziale bilanciata
    ///     4. Raffinamento iterativo con UCT
    ///     5. Selezione finale basata su exploitation
    ///     L'algoritmo bilancia exploration (esplorare mosse poco simulate) ed exploitation (sfruttare mosse con punteggio
    ///     alto) per convergere verso la mossa ottimale entro i limiti di tempo di BattleSnake.
    /// </summary>
    /// <param name="request">Stato corrente del gioco contenente posizioni serpenti, cibo e dimensioni board</param>
    /// <returns>Direzione ottimale calcolata tramite simulazioni Monte Carlo</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Direction>  GetBestMoveAsync(MoveRequest request)
    {
        var board = request.Board;
        var mySnake = request.You;

        var width = board.width;
        var height = board.height;

        var hazards = board.hazards;
        var hazardCount = hazards.Length;

        var snakes = board.snakes;
        var snakeCount = snakes.Length;

        var myId = mySnake.id;
        var myBody = mySnake.body;
        var myBodyLength = myBody.Length;
        var myHead = mySnake.head;
        var myHeadX = myHead.x;
        var myHeadY = myHead.y;
        
        // TODO: Valutare se aggiungere il concetto di "mangiare" anche per i serpenti nemici. 
        //       Da capire se il dato in mio possesso include già l'aumento di lunghezza del serpente nemico.
        var eat = mySnake.health == 100;

        // FASE 1: VALUTAZIONE DIREZIONI SICURE

        // Questo metodo sfrutta le variabili di classe preallocate _collisionMatrix e _validMoves.
        // _collisionMatrix è una variabile privata che rappresenta la matrice delle collisioni sulla board
        // e viene utilizzata nei metodi GetValidMoves e BuildCollisionMatrix.
        // _validMoves contiene sempre 4 elementi preallocati (uno per ogni direzione possibile),
        // ma solo i primi movesCount sono effettivamente validi e rappresentano le mosse sicure trovate.
        // Gli altri valori di _validMoves sono solo segnaposto dovuti alla preallocazione e non vanno considerati.
        // Questo approccio riduce le allocazioni di memoria e migliora le prestazioni nelle simulazioni.
        var movesCount = GetValidMoves(width, height, myId, myBody, myBodyLength, myHeadX, myHeadY, hazards, hazardCount, snakes, snakeCount, eat);

        // Gestione casi limite: se non ci sono scelte, evita simulazioni costose
        if (movesCount < 1) return Direction.Up;
        if (movesCount == 1) return _validMoves[0];

        // Reset punteggi direzionali
        UpScore = DownScore = RightScore = LeftScore = 0.0;

        // FASE 2: FASE DI ESPLORAZIONE INIZIALE
        await InitialExplorationAsync(movesCount);


        return Direction.Down;
    }

    public static unsafe int GetValidMoves(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, hazards, hazardCount, snakes, snakeCount, eat);
        
        // TODO: DEBUG - Stampa matrice di collisione
        _collisionMatrix.Print(width, height);
        
        var count = 0;

        fixed (bool* ptr = _collisionMatrix)
        {
            // === UP ===
            if (myHeadY > 0)
            {
                var checkRow = myHeadY - 1;
                if (checkRow < height && !*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Up;
            }

            // === RIGHT ===
            if (myHeadX + 1 < width)
            {
                var checkCol = myHeadX + 1;
                if (!*(ptr + myHeadY * width + checkCol)) _validMoves[count++] = Direction.Right;
            }

            // === DOWN ===
            if (myHeadY + 1 < height)
            {
                var checkRow = myHeadY + 1;
                if (!*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Down;
            }

            // === LEFT ===
            if (myHeadX > 0)
            {
                var checkCol = myHeadX - 1;
                if (checkCol < width && !*(ptr + myHeadY * width + checkCol)) _validMoves[count++] = Direction.Left;
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void BuildCollisionMatrix(uint width, uint height, string myId, Point[] myBody, int myBodyLength, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        _collisionMatrix.ClearUnsafe();

        fixed (bool* ptr = _collisionMatrix)
        {
            // === HAZARDS ===
            for (var i = 0; i < hazardCount; i++)
            {
                var hazard = hazards[i];
                *(ptr + hazard.y * width + hazard.x) = true;
            }

            // === SERPENTI NEMICI ===
            for (var i = 0; i < snakeCount; i++)
            {
                var snake = snakes[i];
                if (snake.id == myId) continue;

                var enemyHead = snake.head;
                var enemyBody = snake.body;
                var enemyBodyLength = enemyBody.Length;

                // === CORPO NEMICO ===
                for (var j = 0; j < enemyBodyLength; j++)
                {
                    var bodyPart = enemyBody[j];
                    *(ptr + bodyPart.y * width + bodyPart.x) = true;
                }

                // === HEAD-TO-HEAD PREVENTION ===
                if (enemyBodyLength > myBodyLength)
                {
                    var hx = enemyHead.x;
                    var hy = enemyHead.y;
                    var offset = hy * width + hx;

                    // Left
                    if (hx > 0)
                        *(ptr + offset - 1) = true;
                    // Right  
                    if (hx < width - 1)
                        *(ptr + offset + 1) = true;
                    // Up
                    if (hy > 0)
                        *(ptr + offset - width) = true;
                    // Down
                    if (hy < height - 1)
                        *(ptr + offset + width) = true;
                }
            }

            // === ME - CORPO (includi testa per evitare movimenti all'indietro) ===
            var end = eat ? myBodyLength : myBodyLength - 1;
            for (var i = 0; i < end; i++)  // Parti da 0 per includere la testa
            {
                var bodyPart = myBody[i];
                *(ptr + bodyPart.y * width + bodyPart.x) = true;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task InitialExplorationAsync(int movesCount)
    {
        // 1. Calcola la distribuzione ottimale con operazioni bit-wise
        var availableCores = Environment.ProcessorCount;
        var division = availableCores / movesCount;
        var tasksPerMove = Math.Max(1, division);
        var timePerMove = PerformanceConfig.InitialExplorationTimeMs / movesCount;
        var timePerTask = timePerMove / tasksPerMove;
        var totalTasks = movesCount * tasksPerMove;

        // Del buffer di task pre-allocato, usa solo la parte necessaria, grazie a Span<T>
        var taskSpan = _taskBuffer.AsSpan(0, totalTasks);

        var taskIndex = 0;

        // 2. Distribuisce i task per ogni mossa - loop unrolled aggressivo
        for (var i = 0; i < movesCount; i++)
        {
            var remainingCores = availableCores - taskIndex;
            var coresForThisMove = tasksPerMove < remainingCores ? tasksPerMove : remainingCores;

            taskIndex = EnqueueSimulationsUnrolled(_validMoves[i], coresForThisMove, timePerTask, taskSpan, taskIndex);
            _moveTaskCounts[i] = coresForThisMove;
        }

        // 3. Riduco lo span per monitorare solo i task attivi, per esempio con 16 core e 3 mosse avremmo 15 elementi.
        // taskSpan: contiene 16 elementi, 1xCore, ma solo i primi 15 sono attivi
        // taskIndex contiene il numero effettivo di core inserito nel buffer taskSpan (15).
        var activeTasks = taskSpan[..taskIndex];
        var results = await Task.WhenAll(activeTasks.ToArray()).ConfigureAwait(false);
        
        // 4. Aggregazione ultra-veloce con accesso diretto
        var scoreIndex = 0;

        for (var i = 0; i < movesCount; i++)
        {
            var taskCount = _moveTaskCounts[i];
            var scoresSpan = results.AsSpan(scoreIndex, taskCount);

            var totalScore = SumScoresUnrolled(taskCount, scoresSpan);

            // Assegnazione diretta ultra-veloce con jump table implicito
            switch (_validMoves[i])
            {
                case Direction.Up:
                    UpScore = totalScore;
                    break;
                case Direction.Down:
                    DownScore = totalScore;
                    break;
                case Direction.Left:
                    LeftScore = totalScore;
                    break;
                case Direction.Right:
                    RightScore = totalScore;
                    break;
            }

            scoreIndex += taskCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int EnqueueSimulationsUnrolled(Direction move, int coresForThisMove, double timePerTask, Span<Task<double>> taskSpan, int taskIndex)
    {
        var j = 0;
        
        // Unroll aggressivo a gruppi di 8
        var unrollLimit = coresForThisMove & ~7;
        for (; j < unrollLimit; j += 8)
        {
            taskSpan[taskIndex] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 1] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 2] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 3] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 4] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 5] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 6] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 7] = Task.Run(() => SimulateMove(move, timePerTask));
            taskIndex += 8;
        }

        // Unroll a gruppi di 4 per i rimanenti
        var unrollLimit4 = coresForThisMove & ~3;
        for (; j < unrollLimit4; j += 4)
        {
            taskSpan[taskIndex] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 1] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 2] = Task.Run(() => SimulateMove(move, timePerTask));
            taskSpan[taskIndex + 3] = Task.Run(() => SimulateMove(move, timePerTask));
            taskIndex += 4;
        }

        // Rimanenti task (0-3)
        for (; j < coresForThisMove; j++) taskSpan[taskIndex++] = Task.Run(() => SimulateMove(move, timePerTask));
        
        return taskIndex;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SumScoresUnrolled(int taskCount, Span<double> scoresSpan)
    {
        var totalScore = 0.0;
        
        var j = 0;

        // Unroll primario a gruppi di 8 per SIMD
        for (; j <= taskCount - 8; j += 8)
            totalScore += scoresSpan[j]
                          + scoresSpan[j + 1]
                          + scoresSpan[j + 2]
                          + scoresSpan[j + 3]
                          + scoresSpan[j + 4]
                          + scoresSpan[j + 5]
                          + scoresSpan[j + 6]
                          + scoresSpan[j + 7];

        // Unroll secondario a gruppi di 4
        for (; j <= taskCount - 4; j += 4)
            totalScore += scoresSpan[j]
                          + scoresSpan[j + 1]
                          + scoresSpan[j + 2]
                          + scoresSpan[j + 3];

        // Rimanenti elementi
        for (; j < taskCount; j++) totalScore += scoresSpan[j];
        
        return totalScore;
    }

    private double SimulateMove(Direction move, double timeAllowedMs) => move == Direction.Up 
        ? .2 
        : .1;
}