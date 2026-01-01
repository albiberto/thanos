using System.Numerics;
using Thanos.Tests.Integration.WarSnake.Support;
using Thanos.War;
using Thanos.War.Structures;

namespace Thanos.Tests.Integration.WarSnake;

[TestFixture]
public partial class WarSnakeTests
{
    private const int NormalDamage = 1;
    
    // --- SCENARIOS ---
    
    // 1. Stress Test: Uses max possible length (minimal movement or stationary)
    public static IEnumerable<TestCaseData> ExhaustiveScenarios => 
        BuildScenarios("Stress", Enum.GetValues<SnakeStartCorner>(), (area, _) => Math.Min(area, 255), BodyBuilder.ZigZag, [0, 1, 100, 255]);

    // 2. Stacked: Uses width - 1 (Safe spiral unrolling from center)
    public static IEnumerable<TestCaseData> MovementStackedScenarios => 
        BuildScenarios("Mov_Stacked", Enum.GetValues<SnakePlacement>(), (_, width) => width - 1, BodyBuilder.Stacked, [50, 100]);

    // 3. Unrolled: Reduces max length to (Width - 3) to guarantee 
    // at least 2 safe steps for digestion/double-food mechanics tests.
    public static IEnumerable<TestCaseData> MovementUnrolledScenarios => 
        BuildScenarios("Mov_Unrolled", Enum.GetValues<SnakeFacing>(), (_, width) => Math.Max(3, width - 3), BodyBuilder.Linear, [1, 50, 100]);
    
    // ... (Rest of generic engine remains unchanged) ...
    
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

            var maxLength = maxLengthCalculator(map.Data.Area, map.Data.Width);

            foreach (var variation in variations)
            {
                for (var len = 3; len <= maxLength; len++)
                {
                    // BodyBuilder is now safe and handles positioning
                    var body = bodyFactory(len, map.Data.Width, map.Data.Height, variation);

                    foreach (var hp in healthValues)
                    {
                        // MEMORY ISOLATION: Context created PER TEST
                        var isolatedContext = new SnakeMemoryContext(bitboardBytes, capacity, map.Name);

                        yield return new TestCaseData(
                                isolatedContext, 
                                new Environment(map.Data.Width, map.Data.Height, map.Data.Area),
                                body, 
                                hp,
                                variation)
                            .SetName($"{namePrefix}_{map.Name}_{variation}_Len{len}_HP{hp}");
                    }
                }
            }
        }
    }

    // --- Helpers & Types ---

    public record Environment(byte Width, byte Height, ushort Area);
    
    public class SnakeMemoryContext(int bitboardBytes, ushort capacity, string debugName)
    {
        private readonly byte[] _bitboardMemory = new byte[bitboardBytes];
        private readonly byte[] _queueMemory = new byte[capacity * sizeof(ushort)];

        private WarSnakeLife _life;
        private CircularQueueState _queueState;

        public War.WarSnake Build()
        {
            var bitboard = new War.Structures.Bitboard(_bitboardMemory);
            var queue = new War.Structures.CircularQueue(_queueMemory, ref _queueState, capacity);
            return new War.WarSnake(ref _life, bitboard, queue);
        }

        public override string ToString() => $"Ctx({debugName})";
    }
}