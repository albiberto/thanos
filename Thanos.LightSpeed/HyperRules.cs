using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.LightSpeed;

public static class HyperRules
{
    // Mappatura Standard Battlesnake Y=0 in basso (0=Left, 1=Right, 2=Up, 3=Down)
    // Left: -1 (255), Right: +1, Up: +16, Down: -16 (240)
    // Usiamo una property statica che il JIT converte in una data section inline.
    private static ReadOnlySpan<byte> Offsets => [255, 1, 16, 240];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SimulateTurn(ref HyperState state, ReadOnlySpan<byte> moves)
    {
        byte aliveMask = 0;
        byte deadMask = 0;
        byte eatMask = 0;

        byte h0 = 0, h1 = 0, h2 = 0, h3 = 0;

        ref var s0 = ref state.Snake0;
        ref var s1 = ref state.Snake1;
        ref var s2 = ref state.Snake2;
        ref var s3 = ref state.Snake3;
        
        ref var offsets = ref MemoryMarshal.GetReference(Offsets);

        // --- PHASE 1: Snapshot intent and Temporary Tail Removal ---
        if (s0.Health > 0)
        {
            aliveMask |= 1;
            h0 = unchecked((byte)(s0.GetHead() + Unsafe.Add(ref offsets, moves[0])));
            if (state.Food.IsSet(h0)) eatMask |= 1;
            else if (s0.PendingGrowth == 0) state.Obstacles.Unset(s0.GetTail());
        }

        if (s1.Health > 0)
        {
            aliveMask |= 2;
            h1 = unchecked((byte)(s1.GetHead() + Unsafe.Add(ref offsets, moves[1])));
            if (state.Food.IsSet(h1)) eatMask |= 2;
            else if (s1.PendingGrowth == 0) state.Obstacles.Unset(s1.GetTail());
        }

        if (s2.Health > 0)
        {
            aliveMask |= 4;
            h2 = unchecked((byte)(s2.GetHead() + Unsafe.Add(ref offsets, moves[2])));
            if (state.Food.IsSet(h2)) eatMask |= 4;
            else if (s2.PendingGrowth == 0) state.Obstacles.Unset(s2.GetTail());
        }

        if (s3.Health > 0)
        {
            aliveMask |= 8;
            h3 = unchecked((byte)(s3.GetHead() + Unsafe.Add(ref offsets, moves[3])));
            if (state.Food.IsSet(h3)) eatMask |= 8;
            else if (s3.PendingGrowth == 0) state.Obstacles.Unset(s3.GetTail());
        }

        // --- PHASE 2: Static Collisions ---
        if ((aliveMask & 1) != 0 && state.Obstacles.IsSet(h0)) deadMask |= 1;
        if ((aliveMask & 2) != 0 && state.Obstacles.IsSet(h1)) deadMask |= 2;
        if ((aliveMask & 4) != 0 && state.Obstacles.IsSet(h2)) deadMask |= 4;
        if ((aliveMask & 8) != 0 && state.Obstacles.IsSet(h3)) deadMask |= 8;

        // --- PHASE 3: Head-to-Head Collisions ---
        byte survivors = (byte)(aliveMask & ~deadMask);
        
        if (survivors != 0 && (survivors & (survivors - 1)) != 0)
        {
            if ((survivors & 3) == 3 && h0 == h1) {
                if (s0.Length <= s1.Length) deadMask |= 1;
                if (s1.Length <= s0.Length) deadMask |= 2;
            }
            if ((survivors & 5) == 5 && h0 == h2) {
                if (s0.Length <= s2.Length) deadMask |= 1;
                if (s2.Length <= s0.Length) deadMask |= 4;
            }
            if ((survivors & 9) == 9 && h0 == h3) {
                if (s0.Length <= s3.Length) deadMask |= 1;
                if (s3.Length <= s0.Length) deadMask |= 8;
            }
            if ((survivors & 6) == 6 && h1 == h2) {
                if (s1.Length <= s2.Length) deadMask |= 2;
                if (s2.Length <= s1.Length) deadMask |= 4;
            }
            if ((survivors & 10) == 10 && h1 == h3) {
                if (s1.Length <= s3.Length) deadMask |= 2;
                if (s3.Length <= s1.Length) deadMask |= 8;
            }
            if ((survivors & 12) == 12 && h2 == h3) {
                if (s2.Length <= s3.Length) deadMask |= 4;
                if (s3.Length <= s2.Length) deadMask |= 8;
            }
        }

        // --- PHASE 4: Apply Changes ---
        if ((aliveMask & 1) != 0) {
            if ((deadMask & 1) != 0) KillSnake(ref state, ref s0);
            else CommitSnake(ref state, ref s0, h0, (eatMask & 1) != 0);
        }
        if ((aliveMask & 2) != 0) {
            if ((deadMask & 2) != 0) KillSnake(ref state, ref s1);
            else CommitSnake(ref state, ref s1, h1, (eatMask & 2) != 0);
        }
        if ((aliveMask & 4) != 0) {
            if ((deadMask & 4) != 0) KillSnake(ref state, ref s2);
            else CommitSnake(ref state, ref s2, h2, (eatMask & 4) != 0);
        }
        if ((aliveMask & 8) != 0) {
            if ((deadMask & 8) != 0) KillSnake(ref state, ref s3);
            else CommitSnake(ref state, ref s3, h3, (eatMask & 8) != 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void CommitSnake(ref HyperState state, ref HyperSnake snake, byte nextHead, bool eating)
    {
        if (eating)
        {
            snake.Health = 100;
            snake.PendingGrowth++;
            state.Food.Unset(nextHead); 
        }
        else
        {
            snake.Health--;
            if (snake.Health == 0)
            {
                KillSnake(ref state, ref snake);
                return;
            }
        }

        // Branchless growth arithmetic
        byte grows = (byte)(snake.PendingGrowth > 0 ? 1 : 0);
        snake.PendingGrowth -= grows;
        snake.Length += grows;
        
        // Se grows = 0, avanza la coda. Rimuoviamo il vecchio tail dalla maschera.
        if (grows == 0)
        {
            byte oldTail = snake.GetTail();
            snake.BodyMask.Unset(oldTail);
            // Non serve unset state.Obstacles, è stato fatto nella Fase 1
        }

        snake.TailPointer = unchecked((byte)(snake.TailPointer + (1 - grows)));
        
        // Avanza la testa
        snake.HeadPointer = unchecked((byte)(snake.HeadPointer + 1));
        snake.Body[snake.HeadPointer] = nextHead;
        
        state.Obstacles.Set(nextHead);
        snake.BodyMask.Set(nextHead);
    }

    /// <summary>
    /// O(1) Instant Snake Obliteration via SIMD Bitmask XOR/ANDNOT.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void KillSnake(ref HyperState state, ref HyperSnake snake)
    {
        // One SIMD instruction to clear the whole snake from the global map
        state.Obstacles.AndNot(ref snake.BodyMask);
        snake.BodyMask.Clear();
        snake.Health = 0;
        state.AliveCount--;
    }
}