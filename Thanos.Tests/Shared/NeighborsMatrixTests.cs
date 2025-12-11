// using Thanos.Common;
// using Thanos.Shared;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Shared;
//
// [TestFixture]
// public class NeighborsMatrixTests
// {
//     private static object[][] Dimensions => Support.Dimensions;
//
//     /// <summary>
//     ///     Verifies that NeighborsMatrix correctly reads neighbor indices from the underlying memory buffer
//     ///     using both GetAt() method (with move index) and Get() method (with move mask) across different grid dimensions.
//     /// </summary>
//     [TestCaseSource(nameof(Dimensions))]
//     public void Matrix_ShouldRead_Correctly_FromUnderlyingMemory(byte width, byte height, ushort area)
//     {
//         var buffer = new ushort[area * 4];
//         for (var i = 0; i < buffer.Length; i++) buffer[i] = (ushort)i;
//
//         byte[] masks = [Moves.Up, Moves.Down, Moves.Left, Moves.Right];
//
//         var matrix = new NeighborsMatrix(buffer);
//
//         for (ushort pos = 0; pos < area; pos++)
//         for (var moveIndex = 0; moveIndex < 4; moveIndex++)
//         {
//             var expected = buffer[pos * 4 + moveIndex];
//             var moveMask = masks[moveIndex];
//
//             using (EnterMultipleScope())
//             {
//                 That(matrix.GetAt(pos, moveIndex), Is.EqualTo(expected), $"GetAt({pos}, {moveIndex}) returned an incorrect value.");
//                 That(matrix.Get(pos, moveMask), Is.EqualTo(expected), $"Get({pos}, {moveMask}) returned an incorrect value.");
//             }
//         }
//     }
//
//     /// <summary>
//     ///     Verifies that NeighborsMatrix reflects changes made to the underlying memory buffer,
//     ///     ensuring the matrix acts as a view over the buffer rather than a copy.
//     /// </summary>
//     [Test]
//     public void Matrix_ShouldReflect_ChangesInUnderlyingMemory()
//     {
//         var buffer = new ushort[4];
//         buffer[0] = 100;
//         var matrix = new NeighborsMatrix(buffer);
//
//         buffer[0] = 999;
//
//         That(matrix.Get(0, Moves.Up), Is.EqualTo(999), "Matrix should reflect changes in underlying buffer.");
//     }
//
//     /// <summary>
//     ///     Verifies that NeighborsMatrix.IsValid correctly identifies ushort.MaxValue as invalid
//     ///     and all other values as valid neighbor indices.
//     /// </summary>
//     [Test]
//     public void IsValid_ShouldReturnCorrectBoolean()
//     {
//         using (EnterMultipleScope())
//         {
//             That(NeighborsMatrix.IsValid(ushort.MaxValue), Is.False, "MaxValue should be Invalid");
//             That(NeighborsMatrix.IsValid(0), Is.True, "0 should be Valid");
//             That(NeighborsMatrix.IsValid(12345), Is.True, "Any other number should be Valid");
//         }
//     }
// }