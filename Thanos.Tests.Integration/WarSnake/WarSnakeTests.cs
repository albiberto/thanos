using System.Numerics;
using Thanos.Tests.Integration.WarSnake.Support;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Tests.Integration.WarSnake;

[TestFixture]
public partial class WarSnakeTests
{
    private const int NormalDamage = 1;
    
    // --- PUBLIC SCENARIO SOURCES ---
    public static IEnumerable<TestCaseData> ExhaustiveScenarios => BuildScenarios("Stress", Enum.GetValues<SnakeStartCorner>(), (area, _) => Math.Min(area, 255), BodyBuilder.ZigZag, [0, 1, 100, 255]);
    public static IEnumerable<TestCaseData> MovementStackedScenarios => BuildScenarios("Mov_Stacked", Enum.GetValues<SnakePlacement>(), (_, width) => width - 1, BodyBuilder.Stacked, [50, 100]);
    public static IEnumerable<TestCaseData> MovementUnrolledScenarios => BuildScenarios("Mov_Unrolled", Enum.GetValues<SnakeFacing>(), (_, width) => width - 1, BodyBuilder.Linear, [1, 50, 100]);
    
    // --- GENERIC ENGINE ---

    private static IEnumerable<TestCaseData> BuildScenarios<TVariation>(string namePrefix, TVariation[] variations, Func<int, int, int> maxLengthCalculator, Func<int, int, int, TVariation, ushort[]> bodyFactory, byte[] healthValues)
    {
        var maps = new[]
        {
            (Name: "Small", Data: Constants.Small),
            (Name: "Medium", Data: Constants.Medium),
            (Name: "Large", Data: Constants.Large)
        };

        foreach (var map in maps)
        {
            var bitboardBytes = (map.Data.Area + 63) / 64 * 8;
            var neededCapacity = BitOperations.RoundUpToPowerOf2(map.Data.Area);
            var capacity = (ushort)Math.Min(neededCapacity, 256);

            var context = new SnakeMemoryContext(bitboardBytes, capacity, map.Name);
            var maxLength = maxLengthCalculator(map.Data.Area, map.Data.Width);

            foreach (var variation in variations)
                for (var len = 3; len <= maxLength; len++)
                {
                    var body = bodyFactory(len, map.Data.Width, map.Data.Height, variation);

                    foreach (var hp in healthValues)
                        yield return new TestCaseData(
                                context, 
                                new Environment( map.Data.Width, map.Data.Height, map.Data.Area),
                                body, 
                                hp,
                                variation)
                            .SetName($"{namePrefix}_{map.Name}_{variation}_Len{len}_HP{hp}");
                }
        }
    }

    public record Environment(byte Width, byte Height, ushort Area);
    
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

        public War.WarSnake Build()
        {
            // Reset states implicit in new structs.
            // Memory arrays are reused (and must be cleared by WarSnake.Initialize).
            var bitboard = new War.Structures.Bitboard(_bitboardMemory);
            var queue = new War.Structures.CircularQueue(_queueMemory, ref _queueState, capacity);

            return new War.WarSnake(ref _life, bitboard, queue);
        }

        // Magic touch for NUnit UI: Shows readable name instead of class name.
        public override string ToString() => $"Ctx({debugName})";
    }
}