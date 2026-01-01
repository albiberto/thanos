using System.Numerics;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.Bitboard;

public partial class BitboardTests
{
    [TestCaseSource(nameof(TestDimensions))]
    public void PopCount_WhenComparedToNaiveMethod_ShouldMatchExactly(ushort _, int bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        var rng = new Random(42); // Deterministic seed for reproducibility
        rng.NextBytes(buffer);

        var bitboard = new War.Structures.Bitboard(buffer);

        // Act
        var actual = bitboard.PopCount();

        // Assert
        var expected = buffer.Sum(b => BitOperations.PopCount(b));
        That(actual, Is.EqualTo(expected), "SIMD PopCount mismatch against naive calculation.");
    }
}