using Thanos.War.Structures;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BitboardTests
{
    public static IEnumerable<TestCaseData> TestDimensions
    {
        get
        {
            // Define the bit-counts we want to test, ranging from less than one ulong (32) to multiple ulongs (512).
            int[] logicalBitSizes = [32, 64, 96, 128, 192, 256, 512];

            foreach (var size in logicalBitSizes)
            {
                // CALCULATION:
                // Since Bitboard uses ulong* (64-bit pointers), memory must be aligned to 8 bytes.
                // Formula: Round up to the nearest multiple of 64 bits, then convert to bytes.
                // Example (Size 32): (32 + 63) / 64 = 1 ulong  -> 8 Bytes.
                // Example (Size 96): (96 + 63) / 64 = 2 ulongs -> 16 Bytes.
                var bufferSizeBytes = (size + 63) / 64 * 8;
                var physicalBits = (ushort)(bufferSizeBytes * 8);

                // --- CASE 1: Physical Memory Boundary (Safety Stress Test) ---
                // This tests the absolute limit of the allocated byte array.
                // Because we allocate in 64-bit chunks (ulongs), the Physical Memory is often larger than the Logical Size.
                //
                // Example for Size = 32:
                // - Logical Request: 32 bits.
                // - Physical Allocation: 64 bits (1 ulong / 8 bytes).
                // - Logical Max Index: 31.
                // - Physical Max Index: 63.
                var physicalBound = (ushort)(physicalBits - 1);
                yield return new TestCaseData(physicalBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_PhysicalMax");

                // --- CASE 3: Logical Upper Bound (User Perspective) ---
                // This tests the exact number of bits requested by the "game" logic.
                // We verify that we can write up to the last bit defined by 'size'.
                //
                // Example for Size = 32:
                // - We expect valid indices from 0 to 31.
                // - Input here is 31.
                var logicalBound = (ushort)(size - 1);
                yield return new TestCaseData(logicalBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_LogicalMax");

                // --- CASE 2: Half Capacity ---
                // Standard usage test, filling only half the board.
                var halfBound = (ushort)(size / 2 - 1);
                yield return new TestCaseData(halfBound, physicalBound, physicalBits, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_Half");
            }
        }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Set_WhenBufferIsEmpty(ushort upperBound, ushort physicalBound, ushort _, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Clear(buffer, 0, buffer.Length);
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != 0) Fail("Setup failure: Bitboard should be empty upon initialization.");

        // Act
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Set(i);

            // Immediate check
            if (!bb.IsSet(i)) Fail($"Immediate failure: Failed to SET bit at index {i}.");
        }

        // Assert: Population Count
        var expected = upperBound + 1;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Final count mismatch. Expected {expected} but was {actual}.");

        // Assert: Raw Memory Integrity (Full Scan)
        for (ushort i = 0; i <= physicalBound; i++)
            if (i <= upperBound)
            {
                // Logic Zone: Must be SET
                if (!bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Logical Zone) should be SET but was UNSET.");
            }
            else
            {
                // Extra Zone (Padding): Must be UNSET (remained clean)
                if (bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Extra Zone) was incorrectly SET.");
            }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Unset_WhenBufferIsFull(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF);
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act: Unset only the logical range
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Unset(i);

            // Immediate check
            if (bb.IsSet(i)) Fail($"Immediate failure: Failed to UNSET bit at index {i}.");
        }

        // Assert: Population Count
        // Calculation: Total Physical - (Logical Bits Removed)
        var expected = physicalBits - (upperBound + 1);
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Unset cleared incorrect amount. Expected {expected} dirty bits remaining but was {actual}.");

        // Assert: Raw Memory Integrity (Full Scan)
        for (ushort i = 0; i <= physicalBound; i++)
            if (i <= upperBound)
            {
                // Logic Zone: Must be UNSET
                if (bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Logical Zone) should be UNSET but was SET.");
            }
            else
            {
                // Extra Zone (Padding): Must be SET (remained dirty)
                if (!bb.IsSet(i)) Fail($"Integrity failure: Bit {i} (Extra Zone) was incorrectly UNSET/CLEARED.");
            }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Clear_WhenBufferIsFull(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF);
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act
        bb.Clear();

        // Assert: Population Count
        const int expected = 0;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Non-zero count after Clear(). Expected {expected} but was {actual}.");

        // Assert: Raw Memory Integrity (Double Check)
        for (var i = 0; i < buffer.Length; i++)
            if (buffer[i] != 0)
                Fail($"Integrity failure: Raw memory byte at index {i} is not zero.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Set_WhenBufferIsEmpty_Ascending(ushort upperBound, ushort physicalBound, ushort _, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Clear(buffer, 0, buffer.Length);
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != 0) Fail("Setup failure: Bitboard should be empty upon initialization.");

        // Act: Iterate 0 -> upperBound
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Set(i);

            // Immediate check
            if (!bb.IsSet(i)) Fail($"Immediate failure: Failed to SET bit at index {i}.");
        }

        // Assert: Population Count
        var expected = upperBound + 1;
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Final count mismatch. Expected {expected} but was {actual}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Unset_WhenBufferIsFull_Descending(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        Array.Fill<byte>(buffer, 0xFF); // Setup: Pieno Fisico
        var bb = new Bitboard(buffer);

        // Pre-Check
        if (bb.PopCount() != physicalBits) Fail($"Setup failure: Expected {physicalBits} physical bits set, but got {bb.PopCount()}.");

        // Act: Unset only the logical range, going BACKWARDS (upperBound -> 0)
        for (var i = (int)upperBound; i >= 0; i--)
        {
            var idx = (ushort)i;
            bb.Unset(idx);

            // Immediate check
            if (bb.IsSet(idx)) Fail($"Immediate failure: Failed to UNSET bit at index {idx}.");
        }

        // Assert: Population Count
        // Calculation: Total Physical - (Logical Bits Removed)
        var expected = physicalBits - (upperBound + 1);
        var actual = bb.PopCount();

        That(actual, Is.EqualTo(expected), $"Population failure: Unset cleared incorrect amount. Expected {expected} dirty bits remaining but was {actual}.");
    }
    
    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_QueryMethods_WhenBufferIsFull(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        // Setup Raw: Accendiamo fisicamente tutti i bit (11111111)
        // Questo garantisce che stiamo testando la lettura, non la scrittura.
        Array.Fill(buffer, (byte)0xFF); 
        var bb = new Bitboard(buffer);

        // Act & Assert Loop
        for (ushort i = 0; i <= upperBound; i++)
        {
            // Caso: Bit ACCESO (1)
            var isSet = bb.IsSet(i);
            var isUnset = bb.IsUnset(i);

            // Verifica IsSet
            if (!isSet) Fail($"Logic failure: IsSet({i}) returned False, but memory is physically 0xFF (1).");

            // Verifica IsUnset (Deve essere l'opposto)
            if (isUnset) Fail($"Logic failure: IsUnset({i}) returned True, but memory is physically 0xFF (1).");
            
            // Verifica Coerenza (Non possono mai essere uguali)
            if (isSet == isUnset) Fail($"Logic failure: IsSet and IsUnset returned the same value for index {i}.");
        }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_QueryMethods_WhenBufferIsEmpty(ushort upperBound, ushort physicalBound, ushort _, byte bufferSize)
    {
        // Arrange
        var buffer = new byte[bufferSize];
        // Setup Raw: Spegniamo fisicamente tutti i bit (00000000)
        Array.Clear(buffer, 0, buffer.Length); 
        var bb = new Bitboard(buffer);

        // Act & Assert Loop
        for (ushort i = 0; i <= upperBound; i++)
        {
            // Caso: Bit SPENTO (0)
            var isSet = bb.IsSet(i);
            var isUnset = bb.IsUnset(i);

            // Verifica IsSet
            if (isSet) Fail($"Logic failure: IsSet({i}) returned True, but memory is physically 0x00 (0).");

            // Verifica IsUnset (Deve essere l'opposto)
            if (!isUnset) Fail($"Logic failure: IsUnset({i}) returned False, but memory is physically 0x00 (0).");
            
            // Verifica Coerenza
            if (isSet == isUnset) Fail($"Logic failure: IsSet and IsUnset returned the same value for index {i}.");
        }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_CopyTo_VerifyExactClone(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var srcBuffer = new byte[bufferSize];
        var dstBuffer = new byte[bufferSize];

        // 1. Setup Sorgente: Pattern 01010101 (0x55)
        // Riempie velocemente tutto il buffer con un pattern noto.
        Array.Fill(srcBuffer, (byte)0x55);
        var src = new Bitboard(srcBuffer);

        // 2. Setup Destinazione: Pattern 10101010 (0xAA)
        // Sporchiamo la destinazione con il pattern opposto. 
        // Se CopyTo non sovrascrive correttamente, rimarranno tracce di 0xAA.
        Array.Fill(dstBuffer, (byte)0xAA);
        var dst = new Bitboard(dstBuffer);

        // Pre-Check: Assicuriamoci che siano diverse prima della copia
        // Nota: Potrebbero essere uguali solo se il pattern sorgente fosse casualmente identico a 0xAA (improbabile qui)
        if (src.PopCount() == dst.PopCount())
        {
            // Controllo paranoia: verifichiamo che almeno un ulong sia diverso
            var srcSpan = src.Buffer;
            var dstSpan = dst.Buffer;
            var areDifferent = false;
            for (var i = 0; i < srcSpan.Length; i++)
                if (srcSpan[i] != dstSpan[i])
                {
                    areDifferent = true;
                    break;
                }

            if (!areDifferent) Fail("Setup failure: Source and Destination match before CopyTo execution.");
        }

        // Act
        src.CopyTo(dst);

        // Assert 1: Functional Equality (PopCount)
        var srcCount = src.PopCount();
        var dstCount = dst.PopCount();

        That(dstCount, Is.EqualTo(srcCount), $"Population failure: Destination PopCount ({dstCount}) does not match Source ({srcCount}).");

        // Assert 2: Raw Memory Integrity (Ulong by Ulong comparison)
        // Qui estraiamo direttamente gli span di ulong (la memoria cruda)
        var srcRaw = src.Buffer;
        var dstRaw = dst.Buffer;

        // Verifichiamo che la lunghezza dei buffer (in ulong) sia identica (sanity check)
        That(dstRaw.Length, Is.EqualTo(srcRaw.Length), "Integrity failure: Buffer lengths mismatch.");

        for (var i = 0; i < srcRaw.Length; i++)
        {
            var srcVal = srcRaw[i];
            var dstVal = dstRaw[i];

            if (srcVal != dstVal)
                Fail($"Integrity failure: Raw memory mismatch at ulong index {i}.\n"
                     + $"Expected (Src): {srcVal:X16}\n"
                     + $"Actual   (Dst): {dstVal:X16}");
        }
    }
    
    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Or_WithComplementaryPatterns(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        // Setup: Due pattern perfettamente complementari
        // BB1: 01010101 (0x55)
        // BB2: 10101010 (0xAA)
        Array.Fill(buffer1, (byte)0x55);
        Array.Fill(buffer2, (byte)0xAA);

        var bb1 = new Bitboard(buffer1);
        var bb2 = new Bitboard(buffer2); // 'Other' passed as 'in'

        // Act
        bb1.Or(bb2);

        // Assert 1: BB1 deve essere diventata tutta 1 (0xFF)
        // 0x55 | 0xAA = 0xFF
        var expectedCount = physicalBits; // Tutti i bit fisici accesi
        var actualCount = bb1.PopCount();

        That(actualCount, Is.EqualTo(expectedCount), $"Logic failure: OR operation did not combine bits correctly. Expected full board ({expectedCount}) but got {actualCount}.");

        // Assert 2: Raw Memory di BB1 deve essere 0xFF ovunque
        foreach (var b in buffer1) if (b != 0xFF) Fail($"Integrity failure: BB1 byte is {b:X2}, expected 0xFF.");

        // Assert 3: BB2 (Other) NON deve essere cambiata
        // Deve rimanere 0xAA
        foreach (var b in buffer2) if (b != 0xAA) Fail($"Immutability failure: The 'other' bitboard was modified during OR operation! Byte: {b:X2}");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Xor_WithOverlappingPatterns(ushort upperBound, ushort physicalBound, ushort physicalBits, byte bufferSize)
    {
        // Arrange
        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        // Setup:
        // BB1: 11111111 (0xFF) - Tutto pieno
        // BB2: 10101010 (0xAA) - Pattern alternato
        // XOR: 01010101 (0x55) - Dove c'erano 1 in entrambi, ora c'è 0.
        Array.Fill(buffer1, (byte)0xFF);
        Array.Fill(buffer2, (byte)0xAA);

        var bb1 = new Bitboard(buffer1);
        var bb2 = new Bitboard(buffer2);

        // Act
        bb1.Xor(bb2);

        // Assert 1: BB1 deve essere diventata 0x55
        // Verifica matematica: metà dei bit devono essere accesi
        var expectedCount = physicalBits / 2;
        var actualCount = bb1.PopCount();

        That(actualCount, Is.EqualTo(expectedCount), $"Logic failure: XOR count mismatch. Expected {expectedCount} (0x55 pattern) but got {actualCount}.");

        // Assert 2: Raw Memory di BB1 deve essere 0x55 ovunque
        foreach (var b in buffer1) if (b != 0x55) Fail($"Integrity failure: BB1 byte is {b:X2}, expected 0x55 (Result of FF ^ AA).");

        // Assert 3: BB2 (Other) NON deve essere cambiata
        foreach (var b in buffer2) if (b != 0xAA) Fail($"Immutability failure: The 'other' bitboard was modified during XOR operation! Byte: {b:X2}");
    }
}