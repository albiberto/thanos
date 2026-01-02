using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;
using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    [TestCaseSource(nameof(SystemScenarios))]
    public void Initialize_WhenCalled_ShouldResetAllActiveSnakes(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Setup: Dirty the state of all active snakes
            // We use specific positions [1, 2, 3] to ensure Bitboard has bits set
            for (var i = 0; i < ctx.ActiveCount; i++)
                system[i].Initialize(new Snake($"s{i}", 100, [1, 2, 3]));

            // Pre-Assert: Verify setup was effective
            // We must confirm the Bitboard is actually dirty before testing the cleanup
            That(system[0].Length, Is.EqualTo(3), "Setup failed to dirty Queue.");
            That(system[0].Body.PopCount(), Is.EqualTo(3), "Setup failed to dirty Bitboard.");

            // Act
            system.Initialize();

            // Assert
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                var snake = system[i];

                // 1. Verify Queue Reset
                That(snake.Length, Is.Zero, $"Snake {i} length was not reset.");
                That(snake.Head, Is.Zero, $"Snake {i} head was not reset.");

                // 2. Verify Life Reset
                That(snake.IsDead, Is.True, $"Snake {i} should be dead (HP 0).");

                // 3. Verify Bitboard Reset (TARGET OF THE FIX)
                // This assertion ensures the bitboard memory range is physically zeroed
                That(snake.Body.PopCount(), Is.Zero, $"Snake {i} bitboard was not cleared.");
            }
        }
    }
    
    [TestCaseSource(nameof(SystemScenarios))]
    public void Initialize_WhenCalled_ShouldResetIndices_ButLeaveQueueBufferDirty(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();
            var snake = system[0];

            // Setup: Riempiamo il buffer con valori "sporchi" noti
            // Simuliamo un serpente che si è mosso e ha lasciato dati
            ushort[] dirtyPattern = [0xAA, 0xBB, 0xCC];
            snake.Initialize(new Snake("dirty", 100, dirtyPattern));

            // Pre-check
            That(snake.Length, Is.EqualTo(3));
            That(snake.Head, Is.EqualTo(0xAA)); // Assumendo ordine di inserimento

            // Act
            system.Initialize();

            // Assert
            // 1. Lo stato logico DEVE essere resettato
            That(snake.Length, Is.Zero, "Length not reset.");
            
            // 2. La memoria fisica DEVE rimanere sporca (Ottimizzazione Performance)
            // Accediamo alla memoria raw tramite la Queue esposta o unsafe
            ref var queue = ref GetQueue(ref snake);
            var bufferSpan = queue.Buffer;

            // Verifichiamo che i byte non siano stati azzerati
            // Se Initialize() facesse buffer.Clear(), questo test fallirebbe (ed è quello che vogliamo evitare per speed)
            var hasDirtyBytes = false;
            foreach (var val in bufferSpan)
            {
                if (val != 0) 
                {
                    hasDirtyBytes = true;
                    break;
                }
            }

            That(hasDirtyBytes, Is.True, 
                "PERFORMANCE WARNING: Initialize() is clearing the Queue Buffer. " +
                "It should only reset indices (Head/Tail/Length) to be O(1).");
        }
    }
}