using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.Hyper;

public static class HyperRules
{
    // L'utilizzo di uno switch-expression viene compilato dal RyuJIT in una
    // lookup table nei registri o in operazioni CMOV. Zero cost array-bounds checking.
    // Mappatura: 0=Left(-1), 1=Right(+1), 2=Up(+16), 3=Down(-16)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetOffset(byte move) => move switch
    {
        0 => 255, // 255 equivale a -1 usando la matematica unchecked sui byte
        1 => 1,
        2 => 16,
        _ => 240  // 240 equivale a -16
    };

    /// <summary>
    /// Executes a full Battlesnake turn via explicit loop unrolling.
    /// Zero bounds checks, zero loops, zero pointer arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SimulateTurn(ref HyperState state, ReadOnlySpan<byte> moves)
    {
        byte aliveMask = 0;
        byte deadMask = 0;
        byte eatMask = 0;

        byte h0 = 0, h1 = 0, h2 = 0, h3 = 0;

        // Estrazione dei riferimenti diretti per bypassare ogni lookup futuro
        ref var s0 = ref state.Snake0;
        ref var s1 = ref state.Snake1;
        ref var s2 = ref state.Snake2;
        ref var s3 = ref state.Snake3;

        // --- PHASE 1: Snapshot intent and Temporary Tail Removal ---
        if (s0.Health > 0)
        {
            aliveMask |= 1;
            h0 = unchecked((byte)(s0.GetHead() + GetOffset(moves[0])));
            if (state.Food.IsSet(h0)) eatMask |= 1;
            else if (s0.PendingGrowth == 0) state.Obstacles.Unset(s0.GetTail());
        }

        if (s1.Health > 0)
        {
            aliveMask |= 2;
            h1 = unchecked((byte)(s1.GetHead() + GetOffset(moves[1])));
            if (state.Food.IsSet(h1)) eatMask |= 2;
            else if (s1.PendingGrowth == 0) state.Obstacles.Unset(s1.GetTail());
        }

        if (s2.Health > 0)
        {
            aliveMask |= 4;
            h2 = unchecked((byte)(s2.GetHead() + GetOffset(moves[2])));
            if (state.Food.IsSet(h2)) eatMask |= 4;
            else if (s2.PendingGrowth == 0) state.Obstacles.Unset(s2.GetTail());
        }

        if (s3.Health > 0)
        {
            aliveMask |= 8;
            h3 = unchecked((byte)(s3.GetHead() + GetOffset(moves[3])));
            if (state.Food.IsSet(h3)) eatMask |= 8;
            else if (s3.PendingGrowth == 0) state.Obstacles.Unset(s3.GetTail());
        }

        // --- PHASE 2: Static Collisions (Walls, own body, stationary enemies) ---
        // Se un serpente è vivo (aliveMask) e colpisce un ostacolo, muore.
        if ((aliveMask & 1) != 0 && state.Obstacles.IsSet(h0)) deadMask |= 1;
        if ((aliveMask & 2) != 0 && state.Obstacles.IsSet(h1)) deadMask |= 2;
        if ((aliveMask & 4) != 0 && state.Obstacles.IsSet(h2)) deadMask |= 4;
        if ((aliveMask & 8) != 0 && state.Obstacles.IsSet(h3)) deadMask |= 8;

        // --- PHASE 3: Head-to-Head Collisions ---
        byte survivors = (byte)(aliveMask & ~deadMask);
        
        // Entriamo qui SOLO se ci sono almeno 2 sopravvissuti ai muri
        if (survivors != 0 && (survivors & (survivors - 1)) != 0)
        {
            // Combinazioni hardcoded per massimizzare il branch prediction (0v1, 0v2, 0v3, 1v2, 1v3, 2v3)
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
            if ((deadMask & 1) != 0) KillSnake(ref state.Obstacles, ref s0);
            else CommitSnake(ref state, ref s0, h0, (eatMask & 1) != 0);
        }
        if ((aliveMask & 2) != 0) {
            if ((deadMask & 2) != 0) KillSnake(ref state.Obstacles, ref s1);
            else CommitSnake(ref state, ref s1, h1, (eatMask & 2) != 0);
        }
        if ((aliveMask & 4) != 0) {
            if ((deadMask & 4) != 0) KillSnake(ref state.Obstacles, ref s2);
            else CommitSnake(ref state, ref s2, h2, (eatMask & 4) != 0);
        }
        if ((aliveMask & 8) != 0) {
            if ((deadMask & 8) != 0) KillSnake(ref state.Obstacles, ref s3);
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
                KillSnake(ref state.Obstacles, ref snake);
                return;
            }
        }

        if (snake.PendingGrowth > 0)
        {
            snake.PendingGrowth--;
            snake.Length++;
        }
        else
        {
            snake.TailPointer = unchecked((byte)(snake.TailPointer + 1));
        }

        snake.HeadPointer = unchecked((byte)(snake.HeadPointer + 1));
        snake.Body[snake.HeadPointer] = nextHead;
        state.Obstacles.Set(nextHead);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void KillSnake(ref Bitboard256 obstacles, ref HyperSnake snake)
    {
        byte ptr = snake.TailPointer;
        for (int i = 0; i < snake.Length; i++)
        {
            obstacles.Unset(snake.Body[ptr]);
            ptr = unchecked((byte)(ptr + 1));
        }
        snake.Health = 0;
    }
}