using Thanos.Common;
using Thanos.SourceGen;
using Thanos.Tests.Integration.WarSnake.Support;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.WarSnake;

public partial class WarSnakeTests
{
    [TestCaseSource(nameof(MovementStackedScenarios))]
    public void UpdateAfterMove_WhenStackedAtStart_ShouldUnrollGradually(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakePlacement _)
    {
        // ---------------------------------------------------------
        // 1. Arrange
        // ---------------------------------------------------------
        var snake = context.Build();
        
        // ORACLE: The source of truth.
        // We use a Queue to model the snake segments physically [Tail -> Body -> Head].
        // WarSnake.Initialize pushes segments starting from Tail up to Head.
        // So we Reverse() the body array (which is Head->Tail) to match the FIFO enqueue order.
        var oracleQueue = new Queue<ushort>(body.Reverse());
        
        var snakeData = new Snake("stacked-hero", hp, body);
        snake.Initialize(in snakeData);
        
        // Define a spiral-like path to unroll the snake: Up -> Right -> Down -> Left
        var moves = new[] { Moves.Up, Moves.Right, Moves.Down, Moves.Left };
        var expectedHp = (int)hp;

        // ---------------------------------------------------------
        // 2. Act & Assert (Simulation Loop)
        // ---------------------------------------------------------
        // In our Queue model [Tail -> ... -> Head], the Head is the LAST element.
        var currentHead = oracleQueue.Last();

        foreach (var move in moves)
        {
            // Move for half width distance to ensure full unroll
            foreach (var __ in Enumerable.Range(1, env.Width / 2))
            {
                // --- A. Oracle Calculation (Calculate Expected State) ---
                var (cx, cy) = GetCoord(currentHead, env.Width);
                var (nx, ny) = move switch
                {
                    Moves.Up    => (cx, cy + 1),
                    Moves.Right => (cx + 1, cy),
                    Moves.Down  => (cx, cy - 1),
                    Moves.Left  => (cx - 1, cy),
                    _ => (cx, cy)
                };
                
                var nextHead = (ushort)(ny * env.Width + nx);
                
                // Oracle Logic: Add New Head (Enqueue), Drop Old Tail (Dequeue)
                oracleQueue.Enqueue(nextHead);
                oracleQueue.Dequeue();
                
                expectedHp -= 1; // 1 damage per turn
                currentHead = nextHead;

                // --- B. Execute Real Move ---
                snake.UpdateAfterMove(nextHead, ateFood: false, damage: 1);

                // --- C. Assertions ---
                    // 1. Stats
                    That(snake.HP, Is.EqualTo(expectedHp), $"HP mismatch at {nextHead}.");
                    That(snake.IsDead, Is.EqualTo(expectedHp <= 0), "IsDead logic failed.");
                    That(snake.IsGrowthPending, Is.False, "Snake should not grow without food.");

                    // 2. Queue Geometry (Head/Tail)
                    // Note: With Queue order [Tail -> Head]:
                    // - Last() is the Head (newest item)
                    // - Peek() is the Tail (oldest item)
                    That(snake.Length, Is.EqualTo(body.Length), "Physical Length must remain constant.");
                    That(snake.Head, Is.EqualTo(oracleQueue.Last()), "Head position mismatch.");
                    That(snake.Tail, Is.EqualTo(oracleQueue.Peek()), "Tail position mismatch.");
                    
                    if (body.Length >= 2) 
                    {
                        // ElementBeforeTail is the second element in the FIFO queue (Neck)
                        That(snake.ElementBeforeTail, Is.EqualTo(oracleQueue.ElementAt(1)), "Neck (BeforeTail) mismatch.");
                    }

                    // 3. Bitboard Consistency (The Core Test)
                    
                    // A. Population Count Integrity
                    // The Bitboard Set bits count must match exactly the number of UNIQUE segments in the Oracle.
                    // This proves that overlapping segments (stacking) are handled correctly (1 bit for N segments).
                    var expectedUniqueBits = oracleQueue.Distinct().Count();
                    That(snake.Body.PopCount(), Is.EqualTo(expectedUniqueBits), 
                        $"Bitboard PopCount mismatch. Stacked segments logic error. Oracle: {string.Join(",", oracleQueue)}");

                    // B. Spatial Verification
                    // Every segment in the logical snake must be SET in the Bitboard.
                    foreach (var segment in oracleQueue)
                    {
                        That(snake.Body.IsSet(segment), Is.True, $"Bitboard missing segment {segment}.");
                        That(snake.IsOnBody(segment), Is.True, $"IsOnBody helper failed for {segment}.");
                    }

                    // C. Negative Verification
                    // Check a random point NOT in body is NOT set (basic smoke test)
                    var phantom = (ushort)((nextHead + 50) % 121);
                    if (!oracleQueue.Contains(phantom))
                    {
                        That(snake.Body.IsSet(phantom), Is.False, "Bitboard has phantom bit set.");
                    }
            }
        }
    }

    // --- Helper for Test Logic (Oracle) ---
    private static (int x, int y) GetCoord(ushort index, int width) => (index % width, index / width);
}