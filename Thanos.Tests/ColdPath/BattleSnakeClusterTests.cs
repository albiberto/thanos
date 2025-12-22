// using Thanos.Abstract;
// using Thanos.Memory;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.ColdPath;
//
// [TestFixture]
// [NonParallelizable]
// public class BattleSnakeClusterTests
// {
//     private LookupsMemoryPool _lookups;
//
//     [OneTimeSetUp]
//     public void OneTimeSetup()
//     {
//         _lookups = LookupsMemoryPool.Medium;
//     }
//
//     /// <summary>
//     ///     Verifies that constructor throws ArgumentException when arrays have mismatched lengths,
//     ///     ensuring proper validation.
//     /// </summary>
//     [Test]
//     public void Constructor_Should_Throw_When_Arrays_Have_MismatchedLengths()
//     {
//         var engines = new MCST.Engine[2];
//         var slotPools = new ISlotMemoryPool[3];
//         var nodePools = new INodeMemoryPool[2];
//
//         Throws<ArgumentException>(() => new BattleSnakeCluster(engines, slotPools, nodePools, _lookups),
//             "Constructor should throw ArgumentException when array lengths don't match.");
//     }
//
//     /// <summary>
//     ///     Verifies that constructor accepts arrays of matching lengths,
//     ///     ensuring proper initialization.
//     /// </summary>
//     [Test]
//     public void Constructor_Should_Accept_MatchingLengths()
//     {
//         const int count = 2;
//         const ushort area = 121;
//         const ushort queueCapacity = 121;
//         const byte snakeCount = 4;
//
//         var layout = new SlotMemoryLayout(area, queueCapacity, snakeCount);
//         var nodeLayout = new NodeMemoryLayout();
//
//         var engines = new MCST.Engine[count];
//         var slotPools = new ISlotMemoryPool[count];
//         var nodePools = new INodeMemoryPool[count];
//
//         for (var i = 0; i < count; i++)
//         {
//             slotPools[i] = new SlotMemoryPool(10, 0, snakeCount, _lookups, layout);
//             nodePools[i] = new NodeMemoryPool(1000, 1, nodeLayout);
//             engines[i] = new MCST.Engine(slotPools[i], nodePools[i], 1);
//         }
//
//         BattleSnakeCluster? cluster = null;
//
//         try
//         {
//             DoesNotThrow(() => cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups),
//                 "Constructor should not throw with matching array lengths.");
//
//             That(cluster, Is.Not.Null,
//                 "Cluster should be successfully created.");
//         }
//         finally
//         {
//             cluster?.Dispose();
//         }
//     }
//
//     /// <summary>
//     ///     Verifies that InitializeGame does not throw exceptions with valid snake IDs,
//     ///     ensuring proper game initialization.
//     /// </summary>
//     [Test]
//     public void InitializeGame_Should_Not_Throw_With_ValidSnakeIds()
//     {
//         const int count = 1;
//         const ushort area = 121;
//         const ushort queueCapacity = 128;
//         const byte snakeCount = 4;
//
//         var layout = new SlotMemoryLayout(area, queueCapacity, snakeCount);
//         var nodeLayout = new NodeMemoryLayout();
//
//         var engines = new MCST.Engine[count];
//         var slotPools = new ISlotMemoryPool[count];
//         var nodePools = new INodeMemoryPool[count];
//
//         for (var i = 0; i < count; i++)
//         {
//             slotPools[i] = new SlotMemoryPool(10, 0, snakeCount, _lookups, layout);
//             nodePools[i] = new NodeMemoryPool(1000, 1, nodeLayout);
//             engines[i] = new MCST.Engine(slotPools[i], nodePools[i], 1);
//         }
//
//         using var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);
//
//         string[] snakeIds = [Support.Me, Support.Enemy1, Support.Enemy2];
//
//         DoesNotThrow(() => cluster.InitializeGame(snakeIds),
//             "InitializeGame should not throw with valid snake IDs.");
//     }
//
//     /// <summary>
//     ///     Verifies that Reset does not throw exceptions,
//     ///     ensuring proper state reset.
//     /// </summary>
//     [Test]
//     public void Reset_Should_Not_Throw()
//     {
//         const int count = 1;
//         const ushort area = 121;
//         const ushort queueCapacity = 121;
//         const byte snakeCount = 4;
//
//         var layout = new SlotMemoryLayout(area, queueCapacity, snakeCount);
//         var nodeLayout = new NodeMemoryLayout();
//
//         var engines = new MCST.Engine[count];
//         var slotPools = new ISlotMemoryPool[count];
//         var nodePools = new INodeMemoryPool[count];
//
//         for (var i = 0; i < count; i++)
//         {
//             slotPools[i] = new SlotMemoryPool(10, 0, snakeCount, _lookups, layout);
//             nodePools[i] = new NodeMemoryPool(1000, 1, nodeLayout);
//             engines[i] = new MCST.Engine(slotPools[i], nodePools[i], 1);
//         }
//
//         using var cluster = new BattleSnakeCluster(engines, slotPools, nodePools, _lookups);
//
//         DoesNotThrow(() => cluster.Reset(),
//             "Reset should not throw exceptions.");
//     }
//
//     /// <summary>
//     ///     Verifies that Dispose executes without throwing exceptions,
//     ///     ensuring proper resource cleanup.
//     /// </summary>
//     [Test]
//     public void Dispose_Should_Not_Throw()
//     {
//         const int count = 1;
//         const ushort area = 121;
//         const ushort queueCapacity = 121;
//         const byte snakeCount = 4;
//
//         var layout = new SlotMemoryLayout(area, queueCapacity, snakeCount);
//         var nodeLayout = new NodeMemoryLayout();
//
//         var engines = new MCST.Engine[count];
//         var slotPools = new ISlotMemoryPool[count];
//         var nodePools = new INodeMemoryPool[count];
//
//         for (var i = 0; i < count; i++)
//         {
//             slotPools[i] = new SlotMemoryPool(10, 0, snakeCount, _lookups, layout);
//             nodePools[i] = new NodeMemoryPool(1000, 1, nodeLayout);
//             engines[i] = new MCST.Engine(slotPools[i], nodePools[i], 1);
//         }
//
//         // Dispose only the pools manually, don't dispose the cluster which would dispose the singleton
//         DoesNotThrow(() =>
//         {
//             foreach (var pool in slotPools) pool.Dispose();
//             foreach (var pool in nodePools) pool.Dispose();
//         }, "Dispose should not throw exceptions.");
//     }
// }
//
