// using Thanos.SourceGen;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Integration;
//
// public partial class WarSnakeTests
// {
//     [Test]
//     public void UpdateAfterMove_WhenMovingNormally_ShouldAdvanceHeadAndClearTail()
//     {
//         // Arrange
//         var context = new SnakeTestContext();
//         var snake = context.GetSnakeView();
//
//         // Initial Body: [10, 9, 8]
//         snake.Initialize(new Snake("id", 100, [10, 9, 8]));
//
//         // Act: Move to 11
//         snake.UpdateAfterMove(11, false, 1);
//
//         // Assert
//         That(snake.Head, Is.EqualTo(11), "Head did not advance.");
//         That(snake.Tail, Is.EqualTo(9), "Tail did not advance (old tail 8 should be removed).");
//         That(snake.Length, Is.EqualTo(3), "Length should remain constant.");
//
//         That(snake.Body.IsSet(11), Is.True, "New head bit not set.");
//         That(snake.Body.IsSet(8), Is.False, "Old tail bit not cleared.");
//
//         That(snake.HP, Is.EqualTo(99), "HP did not decrease by damage.");
//     }
//
//     [Test]
//     public void UpdateAfterMove_WhenEatingFood_ShouldGrowAndHeal()
//     {
//         // Arrange
//         var context = new SnakeTestContext();
//         var snake = context.GetSnakeView();
//
//         // Initial: [10, 9, 8], HP 50
//         snake.Initialize(new Snake("id", 50, [10, 9, 8]));
//
//         // Act: Eat at 11
//         snake.UpdateAfterMove(11, true, 0);
//
//         // Assert
//         // GROWTH RULE: If I eat, my tail does NOT move.
//         That(snake.Head, Is.EqualTo(11), "Head did not advance.");
//         That(snake.Tail, Is.EqualTo(8), "Tail moved but shouldn't have (Growth).");
//         That(snake.Length, Is.EqualTo(4), "Length did not increase.");
//
//         That(snake.Body.IsSet(11), Is.True, "New head bit missing.");
//         That(snake.Body.IsSet(10), Is.True, "Old head bit missing.");
//         That(snake.Body.IsSet(8), Is.True, "Tail bit missing (should persist).");
//
//         That(snake.HP, Is.EqualTo(100), "Eating did not fully cure snake.");
//         That(snake.IsGrowthPending, Is.True, "Growth pending flag was not set.");
//     }
//
//     [Test]
//     public void UpdateAfterMove_WhenDigestingFood_ShouldMoveTail()
//     {
//         // STANDARD BATTLESNAKE RULE CHECK
//         // Turn T (Eat): Length + 1, Tail Stays.
//         // Turn T+1 (Digesting): Length Constant, Tail Moves.
//
//         // Arrange
//         var context = new SnakeTestContext();
//         var snake = context.GetSnakeView();
//
//         // Setup: Snake just ate in the previous theoretical turn. 
//         // Current State: [10, 9, 8], Length 3.
//         snake.Initialize(new Snake("id", 100, [10, 9, 8]));
//
//         // Turn T: EAT at 11
//         snake.UpdateAfterMove(11, true, 0);
//         // Check T invariants
//         That(snake.Length, Is.EqualTo(4), "Turn T: Length should increase.");
//         That(snake.Tail, Is.EqualTo(8), "Turn T: Tail should stay.");
//
//         // Act: Turn T+1: MOVE to 12 (No Food)
//         snake.UpdateAfterMove(12, false, 1);
//
//         // Assert
//         // The bug was here: Implementation was keeping tail at 8 (Length 5).
//         // Correct behavior: Tail moves to 9 (Length 4).
//
//         That(snake.Length, Is.EqualTo(4), "Turn T+1: Length should NOT increase again.");
//         That(snake.Tail, Is.EqualTo(9), "Turn T+1: Tail SHOULD advance.");
//
//         That(snake.Body.IsSet(8), Is.False, "Turn T+1: Old tail (8) bit must be cleared.");
//         That(snake.Body.IsSet(9), Is.True, "Turn T+1: New tail (9) bit must be set.");
//     }
// }