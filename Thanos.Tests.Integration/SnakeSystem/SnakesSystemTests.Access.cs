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
            const ushort expectedNeck = 11; // Body[Length - 2]
            const ushort expectedTail = 12; // Body[Length - 1]

            // Initialize Snake 0 uniquely: [Head(9), Body(10), Neck(11), Tail(12)]
            system[0].Initialize(new Snake("me", expectedHp, [expectedHead, 10, expectedNeck, expectedTail]));

            // Act
            var me = system.Me;

            // Assert
            // 1. Vital Signs
            That(me.HP, Is.EqualTo(expectedHp), "Me.HP mismatch.");

            // 2. Structural Pointers (Verify Queue Pointers)
            That(me.Head, Is.EqualTo(expectedHead), "Me.Head mismatch.");
            That(me.ElementBeforeTail, Is.EqualTo(expectedNeck), "Me.ElementBeforeTail mismatch.");
            That(me.Tail, Is.EqualTo(expectedTail), "Me.Tail mismatch.");

            // 3. Structural Identity (Verify Reference)
            // If we modify 'Me', system[0] should reflect it immediately (Zero-Copy)
            me.Kill();
            That(system[0].IsDead, Is.True, "Me property is not pointing to Snake[0] reference.");
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
                // Assign unique HP/Head based on index to prevent accidental collisions
                var hp = (byte)(10 + i);
                var head = (ushort)(i * 10);

                // Construct a body: [Head, Body, Neck, Tail]
                var elementBeforeTail = (ushort)(head + 2);
                var tail = (ushort)(head + 3);

                system[i].Initialize(new Snake($"s{i}", hp, [head, (ushort)(head + 1), elementBeforeTail, tail]));

                // Act & Assert
                var snake = system[i];

                // Expectations
                var expectedHp = hp;
                var expectedHead = head;
                var expectedElementBeforeTail = elementBeforeTail;
                var expectedTail = tail;

                // 1. Vital Signs
                That(snake.HP, Is.EqualTo(expectedHp), $"Snake {i} HP mismatch.");

                // 2. Structural Pointers
                That(snake.Head, Is.EqualTo(expectedHead), $"Snake {i} Head mismatch.");
                That(snake.ElementBeforeTail, Is.EqualTo(expectedElementBeforeTail), $"Snake {i} ElementBeforeTail mismatch.");
                That(snake.Tail, Is.EqualTo(expectedTail), $"Snake {i} Tail mismatch.");
            }
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
            // Otteniamo le ref struct (viste)
            var me = system.Me;
            var indexZero = system[0];

            // Assert
            // 1. Estraiamo i riferimenti alla memoria sottostante (Queue Raw Buffer)
            // Nota: GetQueue è visibile perché definita 'private static' nella partial class in Lifecycle.cs
            ref var meQueue = ref GetQueue(ref me);
            ref var zeroQueue = ref GetQueue(ref indexZero);

            // 2. Otteniamo i puntatori agli indirizzi fisici
            var mePtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(meQueue.Raw));
            var zeroPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(zeroQueue.Raw));

            // 3. Verifica Identità
            That((nint)mePtr, Is.EqualTo((nint)zeroPtr), 
                "FATAL: 'Me' property creates a copy or points to different memory than 'this[0]'. Zero-copy violation.");
        }
    }
}