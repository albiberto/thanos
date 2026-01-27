using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    [TestCaseSource(nameof(SystemScenarios))]
    public void Me_WhenAccessed_ShouldAlwaysReturnFirstSnake(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Setup: Define explicit expectations for a distinct 4-segment body
            const byte expectedHp = 99;
            const ushort expectedHead = 9;
            const ushort expectedNeck = 11;
            const ushort expectedTail = 12;

            system[0].Initialize(new Snake("me", expectedHp, [expectedHead, 10, expectedNeck, expectedTail]));

            // Act
            var me = system.Me;

            // Assert
            // 1. Vital Signs
            That(me.Hp, Is.EqualTo(expectedHp), "Me.HP mismatch.");

            // 2. Structural Pointers
            That(me.Head, Is.EqualTo(expectedHead), "Me.Head mismatch.");
            That(me.PreTail, Is.EqualTo(expectedNeck), "Me.ElementBeforeTail mismatch.");
            That(me.Tail, Is.EqualTo(expectedTail), "Me.Tail mismatch.");

            // 3. Structural Identity (Verify Reference)
            // If we modify 'Me', system[0] should reflect it immediately (Zero-Copy)
            me.Kill();
            That(system[0].IsDead, Is.True, "Me property is not pointing to Snake[0] reference.");
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public unsafe void Me_WhenComparingPointers_ShouldPointToSameMemoryAsIndexZero(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Act
            // Obtain ref structs (views)
            var me = system.Me;
            var indexZero = system[0];

            // Assert
            // 1. Extract references to underlying memory (Queue Raw Buffer)
            // Note: GetQueue is visible because defined 'private static' in the partial class
            ref var meQueue = ref GetQueue(ref me);
            ref var zeroQueue = ref GetQueue(ref indexZero);

            // 2. Obtain pointers to physical addresses
            var mePtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(meQueue.Raw));
            var zeroPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(zeroQueue.Raw));

            // 3. Verify Identity
            That((nint)mePtr, Is.EqualTo((nint)zeroPtr),
                "FATAL: 'Me' property creates a copy or points to different memory than 'this[0]'. Zero-copy violation.");
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void Indexer_WhenAccessed_ShouldReturnCorrectInstance(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Initialize distinct states for every active snake
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                var hp = (byte)(10 + i);
                var head = (ushort)(i * 10);
                var elementBeforeTail = (ushort)(head + 2);
                var tail = (ushort)(head + 3);

                system[i].Initialize(new Snake($"s{i}", hp, [head, (ushort)(head + 1), elementBeforeTail, tail]));

                // Act & Assert
                var snake = system[i];

                // 1. Vital Signs
                That(snake.Hp, Is.EqualTo(hp), $"Snake {i} HP mismatch.");

                // 2. Structural Pointers
                That(snake.Head, Is.EqualTo(head), $"Snake {i} Head mismatch.");
                That(snake.PreTail, Is.EqualTo(elementBeforeTail), $"Snake {i} ElementBeforeTail mismatch.");
                That(snake.Tail, Is.EqualTo(tail), $"Snake {i} Tail mismatch.");
            }
        }
    }
}