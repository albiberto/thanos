using System.Numerics;
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
    
    // Pre-allocazioni statiche - zero allocazioni runtime
    private static readonly double[] _resultsBuffer = new double[256];
    private static readonly unsafe double*[] _scorePointers = new double*[4];
    private static readonly ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
    private static TaskDistribution _cachedDistribution;
    private static int _lastMovesCount = -1;

    // Valori pre-computati di √(ln(n)) per ottimizzare il calcolo UCT nelle simulazioni Monte Carlo.
    private readonly double[] _precomputedSqrtLog;

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
    public Direction  GetBestMove(MoveRequest request)
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
        InitialExploration(movesCount);
        
        return Direction.Down;
    }

    public static unsafe int GetValidMoves(uint width, uint height, string myId, Point[] myBody, int myBodyLength, uint myHeadX, uint myHeadY, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, hazards, hazardCount, snakes, snakeCount, eat);
        
        // TODO: DEBUG - Stampa matrice di collisione
        // _collisionMatrix.Print(width, height);
        
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
    private unsafe void InitialExploration(int movesCount)
    {
        // 1. Setup puntatori scores una volta sola (se non fatto)
        if (_scorePointers[0] == null)
        {
            _scorePointers[0] = (double*)Unsafe.AsPointer(ref UpScore);
            _scorePointers[1] = (double*)Unsafe.AsPointer(ref DownScore);  
            _scorePointers[2] = (double*)Unsafe.AsPointer(ref LeftScore);
            _scorePointers[3] = (double*)Unsafe.AsPointer(ref RightScore);
        }
    
        // 2. Cache distribuzione (branch prediction friendly)
        var distribution = _lastMovesCount == movesCount 
            ? _cachedDistribution 
            : _cachedDistribution = new TaskDistribution(movesCount, Environment.ProcessorCount, PerformanceConfig.InitialExplorationTimeMs);
        
        _lastMovesCount = movesCount;
    
        // 3. Esecuzione parallela raw - zero overhead
        Parallel.For(0, distribution.TotalTasks, _parallelOptions, taskIndex =>
        {
            var moveIndex = taskIndex / distribution.TasksPerMove;
            _resultsBuffer[taskIndex] = SimulateMove(_validMoves[moveIndex], distribution.TimePerTask);
        });
    
        // 4. Aggregazione ultra-veloce con SIMD + unsafe
        fixed (double* resultsPtr = _resultsBuffer)
        {
            var resultIndex = 0;
        
            for (var i = 0; i < movesCount; i++)
            {
                var moveInt = (int)_validMoves[i];
                var scoresPtr = resultsPtr + resultIndex;
                var totalScore = SumScoresHyperOptimized(scoresPtr, distribution.TasksPerMove);
            
                // Assegnazione diretta con puntatori - zero branch
                *_scorePointers[moveInt] = totalScore;
            
                resultIndex += distribution.TasksPerMove;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe double SumScoresHyperOptimized(double* scores, int count)
    {
        var totalScore = 0.0;
        var i = 0;
    
        // SIMD ultra-aggressivo con Vector<double>
        if (Vector.IsHardwareAccelerated && count >= 16)
        {
            var vectorSize = Vector<double>.Count;
            var vectorLimit = count & ~(vectorSize - 1);
            var vectorSum = Vector<double>.Zero;
        
            // Unroll SIMD a blocchi di 4 vector (cache-friendly)
            var blockLimit = vectorLimit & ~(vectorSize * 4 - 1);
            for (; i < blockLimit; i += vectorSize * 4)
            {
                var v1 = Unsafe.Read<Vector<double>>(scores + i);
                var v2 = Unsafe.Read<Vector<double>>(scores + i + vectorSize);
                var v3 = Unsafe.Read<Vector<double>>(scores + i + vectorSize * 2);
                var v4 = Unsafe.Read<Vector<double>>(scores + i + vectorSize * 3);
                
                vectorSum += v1 + v2 + v3 + v4;
            }
        
            // Rimanenti vector
            for (; i < vectorLimit; i += vectorSize) vectorSum += Unsafe.Read<Vector<double>>(scores + i);
        
            // Estrai somma dal vector
            for (var j = 0; j < vectorSize; j++) totalScore += vectorSum[j];
        }
    
        // Unroll manuale aggressivo per i rimanenti
        var remaining = count - i;
        var unrollLimit8 = i + (remaining & ~7);
    
        for (; i < unrollLimit8; i += 8)
        {
            totalScore += scores[i] + scores[i + 1] + scores[i + 2] + scores[i + 3] +
                          scores[i + 4] + scores[i + 5] + scores[i + 6] + scores[i + 7];
        }
    
        var unrollLimit4 = i + (remaining & 7 & ~3);
        for (; i < unrollLimit4; i += 4)
        {
            totalScore += scores[i] + scores[i + 1] + scores[i + 2] + scores[i + 3];
        }
    
        // Ultimi elementi
        for (; i < count; i++)
            totalScore += scores[i];
    
        return totalScore;
    }
    
    private double SimulateMove(Direction move, double timeAllowedMs) => move == Direction.Up 
        ? .2 
        : .1;
}