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

    // --- 1. Infrastructure: Bypass Accessors ---
    // These allow the test to "see" private fields required for topology verification.
    // They must match the field names in the original structs exactly.

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_queue")]
    private static extern ref War.Structures.CircularQueue GetQueue(ref War.WarSnake snake);

    [TestCaseSource(nameof(SystemScenarios))]
    public unsafe void Memory_Layout_ShouldGuarantee_ExactStride_Between_All_SequentialSnakes(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            if (system.Count < 2) Ignore("Topology test requires a system capacity of at least 2.");

            // Assert
            for (var i = 0; i < system.Count - 1; i++)
            {
                // 1. Obtain Snake Views
                var snakeCurrent = system[i];
                var snakeNext = system[i + 1];

                // 2. Queue Extraction
                // Use UnsafeAccessor if you prefer encapsulation, or access ._queue directly if public.
                ref var queueCurrent = ref GetQueue(ref snakeCurrent);
                ref var queueNext = ref GetQueue(ref snakeNext);

                // 3. Pointer Extraction via RAW/BUFFER
                // We use the standard MemoryMarshal API to get the pointer to the start of the Buffer.
                // This is stable and equivalent for stride calculation.
                var ptrCurrent = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(queueCurrent.Raw));
                var ptrNext = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(queueNext.Raw));

                // 4. Delta Calculation
                var actualStride = ptrNext - ptrCurrent;
                var expectedStride = (long)ctx.Layout.SnakeStride.Next;

                // 5. Topology Verification
                That(actualStride, Is.EqualTo(expectedStride),
                    $"FATAL: Memory Topology Mismatch between Snake[{i}] and Snake[{i + 1}]. " +
                    $"Actual stride: {actualStride}, Expected: {expectedStride}.");

                // 6. Overlap Safety Check
                // We verify that the allocated stride effectively covers the buffer size.
                // Note: When measuring from QueueBuffer, the critical check is ensuring the NEXT buffer 
                // starts after THIS buffer ends.
                var bufferLen = (long)ctx.Layout.QueueBuffer.Length;

                That(actualStride, Is.GreaterThanOrEqualTo(bufferLen),
                    $"FATAL: Stride too small. Buffer overlap detected. " +
                    $"Stride {actualStride} < BufferLength {bufferLen}.");
            }
        }
    }
}