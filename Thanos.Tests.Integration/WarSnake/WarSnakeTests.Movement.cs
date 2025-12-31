// using Thanos.SourceGen;
// using Thanos.Tests.Integration.WarSnake.Support;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Integration.WarSnake;
//
// public partial class WarSnakeTests
// {
//     // -------------------------------------------------------------------------
//     // 2. BEHAVIORAL TESTS (Physics & Game Logic)
//     // -------------------------------------------------------------------------
//
//     [TestCaseSource(nameof(MovementUnrolledScenarios))]
//     public void UpdateAfterMove_WhenMovingNormally_ShouldAdvanceHeadAndClearTail(SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
//     {
//         // Arrange
//         var snake = context.Build();
//         snake.Initialize(new Snake("hero", hp, body));
//         
//         var targetPos = GetNextPosition(body[0], facing, env.Width);
//         
//         // Act
//         snake.UpdateAfterMove(targetPos, ateFood: false, damage: 1);
//
//         // Assert
//             var expectedHp = hp > 1 ? hp - 1 : 0;
//             That(snake.HP, Is.EqualTo(expectedHp));
//             That(snake.IsDead, Is.EqualTo(expectedHp == 0));
//
//             if (!snake.IsDead)
//             {
//                 var expectedTail = body[^2]; // Il "Collo" diventa Coda
//                 var oldTail = body[^1];      // La vecchia Coda sparisce
//                 
//                 That(snake.Head, Is.EqualTo(targetPos));
//                 That(snake.Tail, Is.EqualTo(expectedTail), "Tail did not advance.");
//                 That(snake.Body.IsSet(oldTail), Is.False, "Old tail not cleared.");
//             }
//     }
//
//     [TestCaseSource(nameof(MovementUnrolledScenarios))]
//     public void UpdateAfterMove_WhenEatingFood_ShouldGrowAndHeal(
//         SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
//     {
//         // Arrange
//         var snake = context.Build();
//         snake.Initialize(new Snake("hero", hp, body));
//         
//         var targetPos = GetNextPosition(body[0], facing, env.Width);
//
//         // Act
//         snake.UpdateAfterMove(targetPos, ateFood: true, damage: 0);
//
//         // Assert
//             That(snake.Length, Is.EqualTo(body.Length + 1));
//             That(snake.IsGrowthPending, Is.True);
//             That(snake.HP, Is.EqualTo(100)); // Full cure
//             
//             var oldTail = body[^1];
//             That(snake.Tail, Is.EqualTo(oldTail), "Tail should stay put.");
//     }
//
//     [TestCaseSource(nameof(MovementUnrolledScenarios))]
//     public void UpdateAfterMove_WhenDigestingFood_ShouldMoveTail(
//         SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
//     {
//         // Arrange
//         var snake = context.Build();
//         snake.Initialize(new Snake("hero", 100, body)); // Start Healthy
//         
//         var eatPos = GetNextPosition(body[0], facing, env.Width);
//
//         // Mossa 1: MANGIA (Turno T)
//         snake.UpdateAfterMove(eatPos, ateFood: true, damage: 0);
//         
//         // Mossa 2: DIGERISCE (Turno T+1)
//         var digestPos = GetNextPosition(eatPos, facing, env.Width);
//         snake.UpdateAfterMove(digestPos, ateFood: false, damage: 1);
//
//         // Assert
//             // Verifica Bug Doppia Crescita
//             That(snake.Length, Is.EqualTo(body.Length + 1), "Snake grew twice!"); 
//             That(snake.IsGrowthPending, Is.False, "Flag consumed.");
//
//             // La coda DEVE muoversi ora
//             var newTail = body[^2];
//             That(snake.Tail, Is.EqualTo(newTail), "Tail MUST advance during digestion.");
//     }
//
//     [TestCaseSource(nameof(MovementUnrolledScenarios))]
//     public void UpdateAfterMove_WhenEatingConsecutively_ShouldKeepTailStationaryAgain(
//         SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
//     {
//         // Arrange
//         var snake = context.Build();
//         snake.Initialize(new Snake("hero", 100, body));
//         
//         var pos1 = GetNextPosition(body[0], facing, env.Width);
//         var pos2 = GetNextPosition(pos1, facing, env.Width);
//
//         // Act 1 & 2
//         snake.UpdateAfterMove(pos1, ateFood: true, damage: 0);
//         snake.UpdateAfterMove(pos2, ateFood: true, damage: 0);
//
//         // Assert
//             That(snake.Length, Is.EqualTo(body.Length + 2));
//             var originalTail = body[^1];
//             That(snake.Tail, Is.EqualTo(originalTail), "Tail stayed put for 2 turns.");
//     }
//     
//     // --- 3. HYBRID (Eating while Stacked) ---
//     
//     [TestCaseSource(nameof(MovementStackedScenarios))]
//     public void UpdateAfterMove_WhenEatingWhileStacked_ShouldGrowAndKeepBitPersistence(
//         SnakeMemoryContext context, Environment env, ushort[] body, byte hp, SnakeFacing facing)
//     {
//         // Arrange
//         var snake = context.Build();
//         snake.Initialize(new Snake("stacked-eater", 100, body)); 
//         
//         var pos1 = GetNextPosition(body[0], facing, env.Width);
//         var pos2 = GetNextPosition(pos1, facing, env.Width);
//
//         // Move 1: Eat (Stacked)
//         snake.UpdateAfterMove(pos1, ateFood: true, damage: 0);
//         
//         // Move 2: Digest (Stacked -> Unrolling)
//         snake.UpdateAfterMove(pos2, ateFood: false, damage: 0);
//
//             That(snake.Length, Is.EqualTo(body.Length + 1)); // 3 -> 4
//             That(snake.IsGrowthPending, Is.False);
//             
//             // Queue Logic: Tail è ancora logicamente alla base (body[0])
//             // Queue fisica dopo digest: [Pos2, Pos1, H, H] (supponendo len 3 init)
//             // La coda logica è avanzata nella queue circolare, ma essendo valori identici...
//             That(snake.Tail, Is.EqualTo(body[0]));
//
//             // Bitboard: Deve proteggere la base
//             That(snake.Body.IsSet(body[0]), Is.True, "Stack base bit must remain set.");
//             That(snake.Body.PopCount(), Is.EqualTo(3), "Bit count 3 (Pos2, Pos1, Base).");
//     }
//
//     // --- HELPERS ---
//
//     private static ushort GetNextPosition(ushort current, SnakeFacing facing, byte width)
//     {
//         // Logica coordinate standard (0,0 Bottom-Left)
//         // Up = Y+1, Down = Y-1, Right = X+1, Left = X-1
//         int dx = 0, dy = 0;
//         switch (facing)
//         {
//             case SnakeFacing.Up:    dy = 1; break;
//             case SnakeFacing.Down:  dy = -1; break;
//             case SnakeFacing.Left:  dx = -1; break;
//             case SnakeFacing.Right: dx = 1; break;
//         }
//         
//         var y = current / width;
//         var x = current % width;
//         
//         // Non serve controllo bounds qui perché il generatore garantisce Width-1 di margine
//         return (ushort)((y + dy) * width + (x + dx));
//     }
// }