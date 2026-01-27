using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.War;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    // --- Infrastructure: Bypass Accessors ---
    // These allow the test to "see" private fields required for topology verification.

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
                ref var queueCurrent = ref GetQueue(ref snakeCurrent);
                ref var queueNext = ref GetQueue(ref snakeNext);

                // 3. Pointer Extraction via RAW/BUFFER
                // Stable anchor for stride calculation
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
                // Note: When measuring from QueueBuffer, the critical check is ensuring the NEXT buffer 
                // starts after THIS buffer ends.
                var bufferLen = (long)ctx.Layout.QueueBuffer.Length;

                That(actualStride, Is.GreaterThanOrEqualTo(bufferLen),
                    $"FATAL: Stride too small. Buffer overlap detected. " +
                    $"Stride {actualStride} < BufferLength {bufferLen}.");
            }
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void CopyFrom_WhenSourceHasComplexState_ShouldCloneToDestination(SnakesSystemTestContext sourceContext)
    {
        // Arrange: Create a matching destination context
        // We infer parameters from the source context to ensure compatibility
        using var destinationContext = new SnakesSystemTestContext(
            sourceContext.MapName,
            // FIX: Rimosso il fattore '* 8'. 
            // Count<ulong> * 64 ci dà un'area sufficiente a generare lo stesso numero di ulong nel layout.
            (ushort)(sourceContext.Layout.Bitboard.Count<ulong>() * 64),
            sourceContext.Layout.QueueCapacity,
            (byte)sourceContext.ActiveCount,
            (byte)sourceContext.LayoutCapacity
        );

        using (sourceContext)
        {
            var source = sourceContext.Build();
            var destination = destinationContext.Build();

            // Setup Source: Distinct states
            for (var i = 0; i < sourceContext.ActiveCount; i++)
            {
                var hp = (byte)(100 - i * 10);
                var body = new[] { (ushort)(i * 10), (ushort)(i * 10 + 1) };
                source[i].Initialize(new($"s{i}", hp, body));
            }

            // Act
            destination.CopyFrom(in source);

            // Assert
            for (var i = 0; i < sourceContext.ActiveCount; i++)
            {
                var srcSnake = source[i];
                var dstSnake = destination[i];

                // Verify Vital Signs
                That(dstSnake.Hp, Is.EqualTo(srcSnake.Hp), $"Snake {i} HP copy failed.");
                That(dstSnake.Length, Is.EqualTo(srcSnake.Length), $"Snake {i} Length copy failed.");

                // Verify Queue State
                That(dstSnake.Head, Is.EqualTo(srcSnake.Head), $"Snake {i} Head copy failed.");
                That(dstSnake.Tail, Is.EqualTo(srcSnake.Tail), $"Snake {i} Tail copy failed.");

                // Verify Bitboard integrity (Hash/PopCount)
                That(dstSnake.Body.PopCount(), Is.EqualTo(srcSnake.Body.PopCount()), $"Snake {i} Bitboard PopCount mismatch.");
            }
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void CopyFrom_WhenDestinationIsModifiedAfterCopy_ShouldNotAffectSource(SnakesSystemTestContext sourceContext)
    {
        // Scenario: Deep Copy verification (Isolation)

        using var destinationContext = new SnakesSystemTestContext(
            sourceContext.MapName,
            sourceContext.Layout.Bitboard.Count<ulong>() * 64 * 8,
            sourceContext.Layout.QueueCapacity,
            (byte)sourceContext.ActiveCount,
            (byte)sourceContext.LayoutCapacity
        );

        using (sourceContext)
        {
            var source = sourceContext.Build();
            var destination = destinationContext.Build();

            // Setup initial state
            source[0].Initialize(new("hero", 100, [1, 2]));

            // Act
            destination.CopyFrom(in source);

            // Modify Destination
            destination[0].UpdateAfterMove(3, false, 10); // Move head to 3, take damage

            // Assert
            // Destination Changed
            That(destination[0].Head, Is.EqualTo(3));
            That(destination[0].Hp, Is.EqualTo(90));

            // Source Unchanged (Isolation)
            That(source[0].Head, Is.EqualTo(1), "Source was modified! Memory overlap detected.");
            That(source[0].Hp, Is.EqualTo(100), "Source HP changed.");
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public unsafe void CopyFrom_WhenExecuted_ShouldNotOverwriteBoundaryMemory(SnakesSystemTestContext sourceCtx)
    {
        // Scenario: Buffer Overrun Protection (Sentinel/Canary Check)
        // Verify CopyFrom strictly respects calculated size and does not write a single byte 
        // beyond the SnakesSystem block.
        // This simulates protection of adjacent global Bitboards (Food/Hazards) in the real Slot.

        using (sourceCtx)
        {
            // 1. Arrange Source (Valid Data)
            var source = sourceCtx.Build();
            source[0].Initialize(new("filler", 100, [1, 2, 3]));

            // 2. Arrange Destination (Manual "Oversized" Allocation)
            // We don't use Context here because we want total control over extra bytes.
            ref readonly var layout = ref sourceCtx.Layout;
            var snakesCount = sourceCtx.LayoutCapacity;

            // Calculate exact system size
            var systemBytes = layout.SnakeStride.Next * (nuint)snakesCount;

            // Allocate: System Size + Sentinel (8 bytes)
            const UIntPtr sentinelSize = sizeof(ulong);
            var totalAlloc = systemBytes + sentinelSize;

            var destPtr = (byte*)NativeMemory.AlignedAlloc(totalAlloc, Constants.CacheLine);

            try
            {
                // Place Sentinel EXACTLY at the end of SnakesSystem block
                const ulong SentinelPattern = 0xDEADBEEF_DEADBEEF;
                var sentinelPtr = (ulong*)(destPtr + systemBytes);
                *sentinelPtr = SentinelPattern;

                // Create Destination view (limited to standard size)
                var destination = new SnakesSystem(destPtr, in layout, snakesCount);

                // Act
                destination.CopyFrom(in source);

                // Assert
                var actualSentinel = *sentinelPtr;
                That(actualSentinel, Is.EqualTo(SentinelPattern),
                    $"Memory Overrun: CopyFrom corrupted memory beyond the block. " +
                    $"Expected {SentinelPattern:X}, got {actualSentinel:X}.");
            }
            finally
            {
                NativeMemory.AlignedFree(destPtr);
            }
        }
    }
}