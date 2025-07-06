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
    
    // Buffer di task (Mosse X Core) pre-allocato per evitare allocazioni dinamiche
    private readonly Task<double>[] _taskBuffer = new Task<double>[4 * Environment.ProcessorCount]; 

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
    public async Task<Direction> GetBestMoveAsync(MoveRequest request)
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

        var eat = mySnake.health < 100;

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

        var count = 0;

        fixed (bool* ptr = _collisionMatrix)
        {
            // === UP ===
            var checkRow = myHeadY - 1;
            if (checkRow < height && !*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Up;

            // === RIGHT ===
            var checkCol = myHeadX + 1;
            if (checkCol < width && !*(ptr + myHeadY * width + checkCol)) _validMoves[count++] = Direction.Right;

            // === DOWN ===
            checkRow = myHeadY + 1;
            if (checkRow < height && !*(ptr + checkRow * width + myHeadX)) _validMoves[count++] = Direction.Down;

            // === LEFT ===
            checkCol = myHeadX - 1;
            if (checkCol < width && !*(ptr + myHeadY * width + checkCol)) _validMoves[count] = Direction.Left;
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

            // === ME - CORPO (salta testa) ===
            var end = eat ? myBodyLength - 1 : myBodyLength;
            for (var i = 1; i < end; i++)
            {
                var bodyPart = myBody[i];
                *(ptr + bodyPart.y * width + bodyPart.x) = true;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task InitialExplorationAsync(int movesCount)
    {
        // 1. Calcola la distribuzione ottimale
        var availableCores = Environment.ProcessorCount;
        var division = availableCores / movesCount;
        var tasksPerMove = (division & ~(division >> 31)) | ((~division + 1) >> 31);
        var timePerMove = PerformanceConfig.InitialExplorationTimeMs / movesCount;
        var timePerTask = timePerMove / tasksPerMove;

        var taskIndex = 0;
        
        // 2. Distribuisce i task per ogni mossa
        for (var i = 0; i < movesCount; i++)
        {
            var move = _validMoves[i];
            var remainingCores = availableCores - taskIndex;
            var coresForThisMove = tasksPerMove < remainingCores ? tasksPerMove : remainingCores;
        
            for (var j = 0; j < coresForThisMove; j++) _taskBuffer[taskIndex++] = Task.Run(() => SimulateMove(move, timePerTask));
        }
        
        // 3. Attende tutti i task e aggrega i risultati
        var scores = await Task.WhenAll(_taskBuffer[..taskIndex]);
        
        // 4. Aggrega i risultati per mossa
        for (var i = 0; i < movesCount; i++)
        {
            var totalScore = 0.0;
            
            var move = _validMoves[i];
            
            var startIndex = i * tasksPerMove;
            var endIndex = Math.Min(startIndex + tasksPerMove, taskIndex);
        
            // Somma i risultati di tutti i task per questa mossa
            for (var j = startIndex; j < endIndex; j++)
            {
                var score = scores[j];
                totalScore += score;
            }

            switch (move)
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private double  SimulateMove(Direction move, int timeAllowedMs) => .1;
}