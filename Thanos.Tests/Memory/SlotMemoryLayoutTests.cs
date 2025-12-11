// using System.Runtime.CompilerServices;
// using NUnit.Framework;
// using Thanos.Common;
// using Thanos.Memory;
// using Thanos.War;
// using Thanos.War.Structures;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Memory;
//
// [TestFixture]
// public class SlotMemoryLayoutTests
// {
//     private SlotMemoryLayout _layout;
//
//     [SetUp]
//     public void Setup()
//     {
//         // Usiamo il layout Medium come standard per i test
//         _layout = SlotMemoryLayout.Medium;
//     }
//
//     [Test]
//     public void Layout_Should_Calculate_StructSizes_Correctly()
//     {
//         // Verifica che le dimensioni di base siano corrette
//         using (EnterMultipleScope())
//         {
//             That(_layout.WarSnakeLife.Length, Is.EqualTo(1));
//             // WarSnakeLife è una struct piccola (byte/short), verifichiamo che l'offset avanzi della sua dimensione
//             var expectedLifeSize = Unsafe.SizeOf<WarSnakeLife>();
//             
//             // Bitboard size: per area 121 (11x11) -> (121 + 63)/64 = 2 ulongs * 8 bytes = 16 bytes
//             var expectedBitboardSize = sizeof(ulong) * 2;
//             That(_layout.Bitboard.Length, Is.EqualTo(expectedBitboardSize));
//         }
//     }
//
//     [Test]
//     public void Layout_Should_Align_QueueBuffer_To_CacheLine()
//     {
//         // Il buffer della coda (heavy data) deve iniziare su una cache line fresca
//         long bufferOffset = _layout.QueueBuffer.Offset;
//
//         using (EnterMultipleScope())
//         {
//             That(bufferOffset % Constants.CacheLine, Is.Zero, "QueueBuffer must be aligned to 64 bytes");
//             
//             // Verifica che non si sovrapponga con lo stato precedente
//             var previousBlockEnd = _layout.CircularQueueState.Offset + Unsafe.SizeOf<CircularQueueState>();
//             That(bufferOffset, Is.GreaterThanOrEqualTo(previousBlockEnd));
//         }
//     }
//
//     [Test]
//     public void Layout_Should_Align_SnakeStride_To_CacheLine()
//     {
//         // Ogni serpente deve finire su un boundary di 64 byte per evitare false sharing col successivo
//         long stride = _layout.SnakeStride;
//
//         using (EnterMultipleScope())
//         {
//             That(stride % Constants.CacheLine, Is.Zero, "SnakeStride must be aligned to 64 bytes");
//             
//             // Deve contenere tutti i dati del serpente
//             var lastBlockEnd = _layout.QueueBuffer.Offset + (sizeof(ushort) * _layout.QueueCapacity);
//             That(stride, Is.GreaterThanOrEqualTo(lastBlockEnd));
//         }
//     }
//
//     [Test]
//     public void Layout_Should_Calculate_SharedBitboards_Offsets_Correctly()
//     {
//         // I bitboard condivisi iniziano dopo tutti i serpenti
//         var snakesEnd = _layout.SnakeStride * Constants.MaxSnakesCount;
//         
//         using (EnterMultipleScope())
//         {
//             // Verifica allineamento globale
//             That(_layout.CollisionsBitboard.Offset % Constants.CacheLine, Is.Zero, "SnakesBitboard must be aligned");
//             That(_layout.CollisionsBitboard.Offset, Is.GreaterThanOrEqualTo(snakesEnd));
//             
//             // Verifica sequenzialità
//             var snakesBbEnd = _layout.CollisionsBitboard.Offset + _layout.CollisionsBitboard.Length;
//             That(_layout.FoodBitboard.Offset, Is.GreaterThanOrEqualTo(snakesBbEnd));
//             
//             var foodBbEnd = _layout.FoodBitboard.Offset + _layout.FoodBitboard.Length;
//             That(_layout.HazardsBitboard.Offset, Is.GreaterThanOrEqualTo(foodBbEnd));
//         }
//     }
//
//     [Test]
//     public void Layout_Should_Align_TotalSlotSize_To_CacheLine()
//     {
//         using (EnterMultipleScope())
//         {
//             That(_layout.SlotSize % Constants.CacheLine, Is.Zero, "Total SlotSize must be aligned to 64 bytes");
//         }
//     }
// }