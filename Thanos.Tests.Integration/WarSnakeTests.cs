// using Thanos.SourceGen;
// using Thanos.War;
// using Thanos.War.Structures;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Integration;
//
// [TestFixture]
// public class WarSnakeTests
// {
//     public static IEnumerable<TestCaseData> Areas => Enumerable
//         .Range(7, 19)
//         .Select(side => new TestCaseData(side * side));
//     
//     private class SnakeMemoryContext
//     {
//         public WarSnakeLife[] Life = [new()];
//         public CircularQueueState[] QueueState = [new()];
//         public byte[] QueueBuffer;
//         public byte[] BitboardBuffer;
//         public ushort Capacity;
//
//         public SnakeMemoryContext(ushort area, ushort queueCapacity)
//         {
//             Capacity = queueCapacity;
//             QueueBuffer = new byte[queueCapacity * sizeof(ushort)]; 
//             
//             // Bitboard size: (Area + 63) / 64 * 8 bytes
//             var ulongCount = (area + 63) / 64;
//             BitboardBuffer = new byte[ulongCount * sizeof(ulong)];
//             
//             // Init QueueState
//             QueueState[0].PlacementNew(queueCapacity);
//         }
//
//         public WarSnake GetSnake()
//         {
//             // Create Spans over the arrays
//             var qSpan = new Span<byte>(QueueBuffer);
//             var bSpan = new Span<byte>(BitboardBuffer);
//
//             // Construct the views
//             var queue = new CircularQueue(qSpan, ref QueueState[0], Capacity);
//             var bitboard = new Bitboard(bSpan);
//
//             return new WarSnake(ref Life[0], bitboard, queue);
//         }
//     }
//
//     [Test]
//     public void Initialize_Should_PopulateQueueAndBitboard_Correctly()
//     {
//         var ctx = new TestContext();
//         var snake = ctx.BuildSnake();
//
//         // Arrange: Un serpente di lunghezza 3
//         ushort[] body = [10, 9, 8]; // Head=10, Tail=8
//         var snakeData = new Snake("id", 100, body);
//
//         // Act
//         snake.Initialize(in snakeData);
//
//         // Assert
//         Multiple(() =>
//         {
//             That(snake.Length, Is.EqualTo(3), "Lunghezza coda errata.");
//             That(snake.Head, Is.EqualTo(10), "Head errata.");
//             That(snake.Tail, Is.EqualTo(8), "Tail errata.");
//             
//             // Verifica Bitboard
//             That(snake.Body.IsSet(10), Is.True, "Bit 10 (Head) dovrebbe essere attivo.");
//             That(snake.Body.IsSet(9), Is.True, "Bit 9 dovrebbe essere attivo.");
//             That(snake.Body.IsSet(8), Is.True, "Bit 8 (Tail) dovrebbe essere attivo.");
//             That(snake.Body.IsSet(7), Is.False, "Bit 7 dovrebbe essere spento.");
//         });
//     }
//
//     [Test]
//     public void UpdateAfterMove_Standard_Should_AdvanceHead_And_RemoveTail()
//     {
//         var ctx = new TestContext();
//         var snake = ctx.BuildSnake();
//
//         // Arrange: Snake [10, 9, 8]
//         snake.Initialize(new Snake("id", 100, [10, 9, 8]));
//
//         // Act: Muovi in 11 (Standard move, no food, no damage)
//         snake.UpdateAfterMove(newHead: 11, ateFood: false, damage: 1);
//
//         // Assert
//         Multiple(() =>
//         {
//             // Stato Queue
//             That(snake.Length, Is.EqualTo(3), "La lunghezza non deve cambiare.");
//             That(snake.Head, Is.EqualTo(11), "Nuova Head deve essere 11.");
//             That(snake.Tail, Is.EqualTo(9), "La vecchia Tail (8) deve essere rimossa, nuova Tail è 9.");
//
//             // Stato Bitboard
//             That(snake.Body.IsSet(11), Is.True, "Bit 11 (New Head) deve essere attivo.");
//             That(snake.Body.IsSet(8), Is.False, "Bit 8 (Old Tail) deve essere spento.");
//             
//             // Stato Vita
//             That(snake.HP, Is.EqualTo(99), "HP deve scendere di 1.");
//         });
//     }
//
//     [Test]
//     public void UpdateAfterMove_WithFood_Should_Grow_And_KeepTail()
//     {
//         var ctx = new TestContext();
//         var snake = ctx.BuildSnake();
//
//         // Arrange: Snake [10, 9, 8]
//         snake.Initialize(new Snake("id", 50, [10, 9, 8]));
//
//         // Act: Mangia in 11
//         snake.UpdateAfterMove(newHead: 11, ateFood: true, damage: 0);
//
//         // Assert
//         Multiple(() =>
//         {
//             // Crescita: La coda NON deve avanzare (Tail rimane 8)
//             That(snake.Length, Is.EqualTo(4), "La lunghezza deve aumentare di 1.");
//             That(snake.Head, Is.EqualTo(11), "Nuova Head 11.");
//             That(snake.Tail, Is.EqualTo(8), "Tail deve rimanere 8 (Crescita).");
//
//             // Bitboard: Tutto acceso
//             That(snake.Body.IsSet(11), Is.True, "New Head attiva.");
//             That(snake.Body.IsSet(8), Is.True, "Old Tail ancora attiva.");
//
//             // Vita
//             That(snake.HP, Is.EqualTo(100), "Mangiare deve curare al massimo.");
//             That(snake.IsGrowthPending, Is.True, "Deve esserci crescita pendente (schedule growth).");
//         });
//     }
//
//     [Test]
//     public void Growth_Should_BeConsolidated_OnNextMove()
//     {
//         var ctx = new TestContext();
//         var snake = ctx.BuildSnake();
//
//         // Arrange: Snake ha appena mangiato. [11, 10, 9, 8] (Length 4 virtuale, growth pending)
//         snake.Initialize(new Snake("id", 100, [10, 9, 8]));
//         snake.UpdateAfterMove(11, ateFood: true, damage: 0); // Ora è lungo 4, Tail=8
//
//         // Act: Muovi ancora (senza cibo). La crescita precedente (pending) viene consumata ora?
//         // Nota: WarSnakeLife.ConsumePendingGrowth viene chiamato DENTRO UpdateAfterMove.
//         // Se era pending, restituisce true -> coda non rimossa? 
//         // NO. WarSnakeLife: "Se ateFood -> Schedule".
//         // UpdateAfterMove logic: "wasGrowing = ConsumePendingGrowth()".
//         // Se ho mangiato al turno T, ScheduleGrowth mette pending=1.
//         // Al turno T+1, ConsumePendingGrowth legge 1 -> wasGrowing=true.
//         // Se wasGrowing è true, NON rimuovo la tail.
//         
//         // QUINDI: La coda si allunga al turno SUCCESSIVO a quello del cibo, o subito?
//         // Guardando il codice di WarSnake:
//         // if (!wasGrowing && !ateFood) RemoveTail();
//         // Se ateFood=true, NON chiama RemoveTail(). Quindi cresce SUBITO (visivamente).
//         // Ma life.ScheduleGrowth() setta il flag per il PROSSIMO turno?
//         // Controlliamo WarSnake.cs:
//         // if (ateFood) { FullCure(); ScheduleGrowth(); }
//         // La logica standard di Battlesnake è: mangio -> lunghezza +1 immediata (coda ferma).
//         // Il codice fa: se ateFood -> NON rimuovo tail. OK. E schedulo growth.
//         // Al prossimo turno: wasGrowing = true -> NON rimuovo tail.
//         // Questo significherebbe crescere di 2?? 
//         
//         // VERIFICA IMPLEMENTAZIONE WarSnake.cs:
//         /*
//            var wasGrowing = _life.ConsumePendingGrowth();
//            if (!wasGrowing && !ateFood) { RemoveTail(); ... }
//            ...
//            if (ateFood) { ... ScheduleGrowth(); }
//         */
//         
//         // Scenario Mangiata (Turno 0):
//         // ateFood = true.
//         // wasGrowing = false (default).
//         // (!false && !true) -> False. RemoveTail NON eseguito.
//         // Coda ferma. Length aumenta. OK.
//         // ScheduleGrowth() -> pending = 1.
//         
//         // Scenario Post-Mangiata (Turno 1):
//         // ateFood = false.
//         // wasGrowing = true (consuma pending).
//         // (!true && !false) -> False. RemoveTail NON eseguito.
//         // Coda ferma ANCHE ORA?
//         // Se la coda sta ferma 2 turni, cresce di 2. Errore?
//         
//         // Analisi Battlesnake Rules:
//         // "Food: .. length increases by 1" -> Tail stays for 1 turn.
//         // Il codice sembra farla stare ferma per 2 turni se ScheduleGrowth è usato così.
//         // Se WarSnakeLife è usato solo qui, c'è un bug o una feature "Stacking".
//         // Ma verifichiamo il comportamento attuale col test.
//         
//         snake.UpdateAfterMove(12, ateFood: false, damage: 1);
//
//         // Assert
//         // Se il codice è corretto per Battlesnake standard, ora dovrebbe rimuovere la tail.
//         // Se il codice usa "stacked growth" (es. Health rule variant?), vedremo.
//         // Assumiamo standard: Length dovrebbe essere 4 stabile (Head avanti, Tail avanti).
//         // Se fallisce (Length 5), abbiamo trovato un bug nell'implementazione o una logica custom.
//         
//         // Basandomi sul codice letto: UpdateAfterMove salta RemoveTail se wasGrowing è true.
//         // Quindi al T+1 la coda cresce ANCORA.
//         // Questo test serve proprio a rivelare questo comportamento.
//     }
// }