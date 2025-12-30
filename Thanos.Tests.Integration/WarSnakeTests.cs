using System.Numerics;
using Thanos.SourceGen;
using Thanos.War;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

public class WarSnakeTests
{
    public enum SnakeStartCorner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    /// <summary>
    ///     Generates exhaustive test cases covering:
    ///     Maps x Corners x Lengths x Health.
    ///     Yields fully constructed MemoryContext and Body arrays.
    /// </summary>
    public static IEnumerable<TestCaseData> ExhaustiveScenarios
    {
        get
        {
            var maps = new[]
            {
                (Name: "Small", Data: Constants.Small),
                (Name: "Medium", Data: Constants.Medium),
                (Name: "Large", Data: Constants.Large)
            };

            foreach (var map in maps)
            {
                // 1. Prepare Context (Allocation Strategy)
                var bitboardBytes = (map.Data.Area + 63) / 64 * 8;
                var neededCapacity = BitOperations.RoundUpToPowerOf2(map.Data.Area);
                var capacity = (ushort)Math.Min(neededCapacity, 256); // Hard cap: CircularQueue uses byte indices, so max capacity/length is 256.

                var context = new SnakeMemoryContext(bitboardBytes, capacity, map.Name);

                // 2. Iterators (Corners x Lengths x Health)
                var corners = Enum.GetValues<SnakeStartCorner>();

                foreach (var corner in corners)
                {
                    // Start from 3 to ensure Head, Neck, and Tail are distinct
                    var physicalLength = Math.Min(map.Data.Area, 255u); // Limit max length to Map Area or 255 (Byte limit)
                    for (var len = 3; len <= physicalLength; len++)
                    {
                        // 3. Generate Body (The "Fusion")
                        // We generate the array here to avoid allocations during NUnit discovery if possible,
                        // but mainly to provide clean data to the test method.
                        var body = GenerateZigZagBody(len, map.Data.Width, map.Data.Height, corner);

                        byte[] healthValues = [0, 1, 100, 255];
                        foreach (var hp in healthValues)
                            // 4. Yield everything packed
                            yield return new TestCaseData(context, body, hp)
                                .SetName($"Stress_{map.Name}_{corner}_Len{len}_HP{hp}");
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Generates a valid, non-colliding snake body of specific length starting from a specific corner.
    ///     Uses a Zig-Zag pattern to fill the grid efficiently.
    /// </summary>
    private static ushort[] GenerateZigZagBody(int length, int width, int height, SnakeStartCorner corner)
    {
        var body = new ushort[length];

        var startFromTop = corner is SnakeStartCorner.TopLeft or SnakeStartCorner.TopRight;
        var startFromRight = corner is SnakeStartCorner.BottomRight or SnakeStartCorner.TopRight;

        for (var i = 0; i < length; i++)
        {
            var row = i / width;
            var col = i % width;

            // Y-Axis Transformation
            var y = startFromTop ? height - 1 - row : row;

            // X-Axis Transformation (Zig-Zag + Corner offset)
            var invertX = row % 2 != 0;
            if (startFromRight) invertX = !invertX;

            var x = invertX ? width - 1 - col : col;

            body[i] = (ushort)(y * width + x);
        }

        return body;
    }

    [TestCaseSource(nameof(ExhaustiveScenarios))]
    public void Initialize_WhenExhaustiveScenarios_ShouldMaintainInvariants(SnakeMemoryContext context, ushort[] body, byte hp)
    {
        // Arrange
        var snake = context.Build();
        var data = new Snake("stress", hp, body);

        // Act
        snake.Initialize(in data);

        // Assert
        // --- 1. Vital Signs (Direct Properties) ---
        That(snake.HP, Is.EqualTo(hp), "HP property mismatch.");
        That(snake.IsDead, Is.EqualTo(hp == 0), "IsDead property logic failed.");
        That(snake.IsGrowthPending, Is.False, "IsGrowthPending should be false after initialization.");

        // --- 2. Queue Geometry (Direct Properties) ---
        That(snake.Length, Is.EqualTo(body.Length), "Queue Length mismatch.");
        That(snake.Head, Is.EqualTo(body[0]), "Head position mismatch (Queue Head).");
        That(snake.Tail, Is.EqualTo(body[^1]), "Tail position mismatch (Queue Tail).");
        if (body.Length >= 2) That(snake.ElementBeforeTail, Is.EqualTo(body[^2]), "ElementBeforeTail mismatch (Queue Neck).");

        // --- 3. Bitboard Consistency (Indirect Properties) ---
        // A. Population Count Integrity
        // Since our generator creates non-overlapping bodies, the number of set bits MUST exactly match the length.
        // This verifies no "phantom bits" were set and no bits were missed.
        That(snake.Body.PopCount(), Is.EqualTo(body.Length), "Bitboard PopCount does not match Snake Length (Phantom or missing bits).");

        // B. Spatial Verification (Lookup)
        // Verify that IsOnBody() and Bitboard.IsSet() return true for EVERY segment.
        for (var i = 0; i < body.Length; i++)
        {
            var segment = body[i];

            That(snake.Body.IsSet(segment), Is.True, $"Bitboard check failed at index {i} (Pos {segment}).");
            That(snake.IsOnBody(segment), Is.True, $"IsOnBody helper failed at index {i} (Pos {segment}).");
        }
    }

    /// <summary>
    ///     Helper class to manage the heap memory required for WarSnake (ref struct).
    /// </summary>
    public class SnakeMemoryContext(int bitboardBytes, ushort capacity, string debugName)
    {
        private readonly byte[] _bitboardMemory = new byte[bitboardBytes];
        private readonly byte[] _queueMemory = new byte[capacity * sizeof(ushort)];

        // Struct states
        private WarSnakeLife _life;
        private CircularQueueState _queueState;

        public WarSnake Build()
        {
            // Reset states implicit in new structs.
            // Memory arrays are reused (and must be cleared by WarSnake.Initialize).
            var bitboard = new Bitboard(_bitboardMemory);
            var queue = new CircularQueue(_queueMemory, ref _queueState, capacity);

            return new WarSnake(ref _life, bitboard, queue);
        }

        // Magic touch for NUnit UI: Shows readable name instead of class name.
        public override string ToString() => $"Ctx({debugName})";
    }
}