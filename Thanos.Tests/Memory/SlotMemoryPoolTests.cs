// TODO: These tests need to be updated to match the current SlotMemoryPool API
// The API has changed significantly:
// - Constructor now requires firstIndex and snakesCount parameters
// - Configure() method has been removed (pool is immutable)
// - GetArena() returns Arena (ref struct) not WarArena
// - Arena.Snakes is a Bitboard, not an array - need to use Arena.System to access snakes
// - Need to understand the new Arena and SnakesSystem API to properly test

// using Thanos.Memory;
// using Thanos.Common;
// using static NUnit.Framework.Assert;
//
// namespace Thanos.Tests.Memory;
//
// [TestFixture]
// public class SlotMemoryPoolTests
// {
//     // Tests to be implemented once SlotMemoryPool API is stable and Arena API is documented
// }

