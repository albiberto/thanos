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

                // --- CASE 1: Physical Memory Boundary (Safety Stress Test) ---
                // This tests the absolute limit of the allocated byte array.
                // Because we allocate in 64-bit chunks (ulongs), the Physical Memory is often larger than the Logical Size.
                //
                // Example for Size = 32:
                // - Logical Request: 32 bits.
                // - Physical Allocation: 64 bits (1 ulong / 8 bytes).
                // - Logical Max Index: 31.
                // - Physical Max Index: 63.
                var physicalBound = (ushort)(bufferSizeBytes * 8 - 1);
                yield return new TestCaseData(physicalBound, physicalBound, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_PhysicalMax");

                // --- CASE 3: Logical Upper Bound (User Perspective) ---
                // This tests the exact number of bits requested by the "game" logic.
                // We verify that we can write up to the last bit defined by 'size'.
                //
                // Example for Size = 32:
                // - We expect valid indices from 0 to 31.
                // - Input here is 31.
                var logicalBound = (ushort)(size - 1);
                yield return new TestCaseData(logicalBound, physicalBound, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_LogicalMax");

                // --- CASE 2: Half Capacity ---
                // Standard usage test, filling only half the board.
                var halfIBound = (ushort)(size / 2 - 1);
                yield return new TestCaseData(halfIBound, physicalBound, (byte)bufferSizeBytes)
                    .SetName($"Size_{size}b_Half");
            }
        }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Filling_EmptyBuffer(ushort upperBound, ushort physicalBound, byte bufferSize)
    {
        // --- TEST SET ---
        // Arrange
        var buffer = new byte[bufferSize]; // Buffer pulito (tutti 0)
        var bb = new Bitboard(buffer);

        // Pre-check: Deve essere vuota all'inizio
        if (bb.PopCount() != 0)
            Fail("Setup failed: Bitboard not empty.");

        // Act: Accendiamo bit per bit
        for (ushort i = 0; i <= upperBound; i++)
        {
            bb.Set(i);

            // Assert Immediato
            if (!bb.IsSet(i))
                Fail($"Failed to SET bit {i} (Size: {bufferSize}B).");
        }

        // Assert Finale
        var totalCount = bb.PopCount();
        // Qui ci aspettiamo che siano accesi esattamente (upperBound + 1) bit
        That(totalCount, Is.EqualTo(upperBound + 1),
            $"Final PopCount mismatch. Expected {upperBound + 1} but was {totalCount}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Unset_FilledBuffer(ushort upperBound, ushort physicalBound, byte bufferSize)
    {
        // --- TEST UNSET (Draining) ---
        // Arrange
        var buffer = new byte[bufferSize];

        // FIX 1: Setup Preciso
        // Non possiamo usare Array.Fill(0xFF) ciecamente perché accenderebbe 
        // anche i bit OLTRE l'upperBound (sporcizia fisica), facendo fallire l'assert finale.
        // Dobbiamo accendere ESATTAMENTE da 0 a upperBound.

        var bitsToSet = upperBound + 1;
        var fullBytes = bitsToSet / 8;
        var remainingBits = bitsToSet % 8;

        // Riempiamo i byte interi
        if (fullBytes > 0) Array.Fill(buffer, (byte)0xFF, 0, fullBytes);
        // Mascheriamo l'ultimo byte parziale (es. se restano 3 bit -> 00000111)
        if (remainingBits > 0) buffer[fullBytes] = (byte)((1 << remainingBits) - 1);

        var bb = new Bitboard(buffer);

        // FIX 2: Pre-Check Corretto
        // Prima controllavamo se era vuota (!= 0), ma ora ci aspettiamo che sia PIENA.
        if (bb.PopCount() != bitsToSet)
            Fail($"Setup failed: Bitboard not correctly filled. Expected {bitsToSet}, got {bb.PopCount()}");

        // Act: Spegniamo bit per bit (All'indietro o in avanti non importa, qui faccio all'indietro)
        for (var i = (int)upperBound; i >= 0; i--)
        {
            var idx = (ushort)i;
            bb.Unset(idx);

            // Assert Immediato
            if (bb.IsSet(idx))
                Fail($"Failed to UNSET bit {idx} (Size: {bufferSize}B).");
        }

        // Assert Finale
        var totalCount = bb.PopCount();
        // FIX 3: Assert Logico
        // Se abbiamo spento tutto, deve essere 0. (Prima controllavi che fosse piena!)
        That(totalCount, Is.EqualTo(0),
            $"Final PopCount mismatch. Expected 0 (Empty) but was {totalCount}.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void StressTest_Clear_FilledBuffer(ushort upperBound, ushort physicalBound, byte bufferSize)
    {
        // --- TEST CLEAR ---
        // Arrange
        var buffer = new byte[bufferSize];

        // Qui possiamo usare Array.Fill(0xFF) perché Clear() deve pulire TUTTA la memoria fisica,
        // inclusa l'eventuale sporcizia oltre l'upperBound logico.
        Array.Fill<byte>(buffer, 0xFF);

        var bb = new Bitboard(buffer);

        // FIX 1: Calcolo bits fisici totali
        // Un buffer di 16 byte ha 128 bit fisici totali.
        var totalPhysicalBits = bufferSize * 8;

        // FIX 2: Pre-Check Corretto
        // Controlliamo che sia PIENA FISICAMENTE. (Prima usavi == per fallire, invece di !=)
        if (bb.PopCount() != totalPhysicalBits)
            Fail($"Setup failed: Bitboard not physically filled. Expected {totalPhysicalBits}, got {bb.PopCount()}.");

        // Act
        bb.Clear();

        // Assert Finale
        var totalCount = bb.PopCount();

        // FIX 3: Assert Logico
        // Clear deve portare a 0. (Prima controllavi upperBound + 1)
        That(totalCount, Is.EqualTo(0),
            $"Final PopCount mismatch after Clear(). Expected 0 but was {totalCount}.");

        // Verifica Extra: Controlliamo che anche il buffer raw sia zero
        foreach (var b in buffer)
            if (b != 0)
                Fail("Clear failed to zero-out raw memory bytes.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void IntegrationTest_CopyTo_VerifyCloningIntegrity(ushort upperBound, byte bufferSize)
    {
        var sourceBuffer = new byte[bufferSize];
        var destBuffer = new byte[bufferSize];

        var source = new Bitboard(sourceBuffer);
        var dest = new Bitboard(destBuffer);

        source.Clear();
        dest.Clear();

        // Pattern dinamico: Alternato pari
        for (ushort i = 0; i <= upperBound; i += 2) source.Set(i);

        // Aggiungiamo i boundary critici (63, 64, 127, 128) SOLO se rientrano nell'upperBound
        if (upperBound >= 63) source.Set(63);
        if (upperBound >= 64) source.Set(64);
        if (upperBound >= 127) source.Set(127);
        if (upperBound >= 128) source.Set(128);

        var expectedCount = source.PopCount();

        // Act
        source.CopyTo(dest);

        // Assert
        var destCount = dest.PopCount();
        That(destCount, Is.EqualTo(expectedCount), $"Clone PopCount mismatch. Src: {expectedCount}, Dest: {destCount}");

        for (ushort i = 0; i <= upperBound; i++)
        {
            var sVal = source.IsSet(i);
            var dVal = dest.IsSet(i);

            if (sVal != dVal)
                Fail($"Clone mismatch at index {i}. Source: {sVal}, Dest: {dVal}");
        }
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void IntegrationTest_Or_CombinesAllBits(ushort upperBound, byte bufferSize)
    {
        var mem1 = new byte[bufferSize];
        var mem2 = new byte[bufferSize];
        var bb1 = new Bitboard(mem1);
        var bb2 = new Bitboard(mem2);

        // Dividiamo il lavoro a metà in base all'upperBound reale
        var midPoint = upperBound / 2;

        // BB1: Prima metà
        for (ushort i = 0; i <= midPoint; i++) bb1.Set(i);

        // BB2: Seconda metà
        for (var i = (ushort)(midPoint + 1); i <= upperBound; i++) bb2.Set(i);

        // Act: OR
        bb1.Or(bb2);

        // Assert: Tutto pieno da 0 a upperBound
        var count = bb1.PopCount();
        That(count, Is.EqualTo(upperBound + 1), "OR failed to combine halves.");

        for (ushort i = 0; i <= upperBound; i++)
            if (bb1.IsUnset(i))
                Fail($"Index {i} missing after OR.");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void IntegrationTest_Xor_FindsDifferences(ushort upperBound, byte bufferSize)
    {
        // Questo test ha senso solo se abbiamo almeno qualche bit su cui lavorare
        if (upperBound < 2) Ignore("Too small for XOR test");

        var mem1 = new byte[bufferSize];
        var mem2 = new byte[bufferSize];
        var bb1 = new Bitboard(mem1);
        var bb2 = new Bitboard(mem2);

        // Riempiamo entrambe fino a quasi la fine
        var limit = (ushort)(upperBound - 1);
        for (ushort i = 0; i <= limit; i++)
        {
            bb1.Set(i);
            bb2.Set(i);
        }

        // Creiamo 2 differenze dinamiche
        var diff1 = (ushort)(limit / 2); // Un punto nel mezzo
        var diff2 = upperBound; // Un punto alla fine (che prima non c'era)

        bb2.Unset(diff1); // BB1 ha diff1, BB2 no
        bb2.Set(diff2); // BB2 ha diff2, BB1 no

        // Act
        bb1.Xor(bb2);

        // Assert
        var count = bb1.PopCount();
        That(count, Is.EqualTo(2), "XOR result count mismatch.");

        That(bb1.IsSet(diff1), Is.True, $"XOR missed diff inside (index {diff1}).");
        That(bb1.IsSet(diff2), Is.True, $"XOR missed diff at end (index {diff2}).");
    }

    [TestCaseSource(nameof(TestDimensions))]
    public void Integrity_Clear_ResetsEverything(ushort upperBound, byte bufferSize)
    {
        var buffer = new byte[bufferSize];
        var bb = new Bitboard(buffer);

        for (ushort i = 0; i <= upperBound; i++) bb.Set(i);

        // Act
        bb.Clear();

        // Assert Memory Zeroed
        var chunks = bb.Buffer;
        foreach (var chunk in chunks)
            if (chunk != 0UL)
                Fail("Memory chunk not zeroed.");

        That(bb.PopCount(), Is.EqualTo(0));
    }
}