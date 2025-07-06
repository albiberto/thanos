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
    public Direction GetBestMove(MoveRequest request)
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

        var eat = mySnake.health < 100;

        // FASE 1: VALUTAZIONE DIREZIONI SICURE
        var mask = GetValidMoves(width, height, myId, myBody, myBodyLength, hazards, hazardCount, snakes, snakeCount, eat);

        return Direction.Down;
    }

    public Direction[] GetValidMoves(uint width, uint height, string myId, Point[] myBody, int myBodyLength, Point[] hazards, int hazardCount, Snake[] snakes, int snakeCount, bool eat)
    {
        BuildCollisionMatrix(width, height, myId, myBody, myBodyLength, hazards, hazardCount, snakes, snakeCount, eat);

        // Ritorna tutte le mosse valide
        return new[]
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };
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
}