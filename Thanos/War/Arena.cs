using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct Arena(SnakesSystem system, Bitboard food, Bitboard hazards, Bitboard snakes, NeighborsMatrix neighborsMatrix)
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    private readonly NeighborsMatrix _neighborsMatrix = neighborsMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeFromRequest(in Request request, ReadOnlySpan<string> orderedIds)
    {
        System.Initialize();
        Food.Clear();
        Hazards.Clear();
        Snakes.Clear();

        var board = request.Board;

        foreach (var snakeData in board.Snakes)
        {
            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (orderedIds[i] != snakeData.Id) continue;
                
                var snake = System[i];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
                break;
            }
        }

        foreach (var p in board.Food) Food.Set(p);
        foreach (var p in board.Hazards) Hazards.Set(p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        // Semantica "Pull" (Io copio da te) coerente
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }

    // --- SIMULATION ENGINE ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SimulateTurn(ReadOnlySpan<byte> moves, int hazardDamage)
    {
        // 1. Stack Allocation: Velocità luce, zero GC, zero Pool.
        // Questi buffer vivono solo qui per evitare di riaccedere a System[i] (lento).
        Span<ushort> nextHeads = stackalloc ushort[Constants.MaxSnakesCount];
        Span<ushort> tails = stackalloc ushort[Constants.MaxSnakesCount];
        Span<int> lengths = stackalloc int[Constants.MaxSnakesCount];

        // 2. Registri CPU per lo stato (Locals)
        int aliveMask = 0;
        int deadMask = 0;
        int eatMask = 0;

        // --- FASE 1: Snapshot & Move Calculation ---
        for (var i = 0; i < System.Count; i++)
        {
            var snake = System[i];
            if (snake.IsDead) continue;

            var nextHead = _neighborsMatrix.Get(snake.Head, moves[i]);
            
            // Cache su stack
            nextHeads[i] = nextHead;
            lengths[i] = snake.Length;
            tails[i] = snake.Tail;

            aliveMask |= 1 << i;

            // "The LightSpeed": If-Else a cascata per safety e velocità.
            // Se è muro, NON controlliamo il cibo (evita IndexOutOfRangeException).
            if (NeighborsMatrix.IsOutOfBound(nextHead))
            {
                deadMask |= 1 << i; // Muro -> Morte certa
            } 
            else if (Food.IsSet(nextHead)) 
            {
                eatMask |= 1 << i;
            }
            
            // Nota: Non controlliamo i corpi qui. Deferiamo alla Fase 2 (Tail Chasing).
        }

        // --- FASE 2: Risoluzione Collisioni Corpi (Tail Chasing) ---
        
        // wallsSurvivors: Serpenti vivi che NON si sono schiantati sui muri.
        var wallsSurvivorsMask = aliveMask & ~deadMask;
        
        // movingTailsMask: Sottoinsieme dei sopravvissuti che NON mangiano.
        // Solo le code di questi serpenti libereranno la casella occupata.
        var movingTailsMask = wallsSurvivorsMask & ~eatMask;

        while (wallsSurvivorsMask != 0)
        {
            var i = BitOperations.TrailingZeroCount(wallsSurvivorsMask);
            wallsSurvivorsMask &= wallsSurvivorsMask - 1;

            var nextHead = nextHeads[i];

            // Se la casella è libera staticamente, siamo salvi.
            if (Snakes.IsUnset(nextHead)) continue; 

            // Se occupata, cerchiamo un "Salvatrice" (Snake J) che libera la coda.
            var isSafeTail = false;
            
            for (var j = 0; j < System.Count; j++)
            {
                // Filtro Negativo: Saltiamo J se NON può salvarci.
                // J fallisce se: È statico (Bit 0) OPPURE La sua coda non è dove vogliamo andare.
                if ((movingTailsMask & (1 << j)) == 0 || nextHead != tails[j]) continue;

                // Se arriviamo qui: J si muove E libera esattamente nextHead.
                isSafeTail = true;
                break;
            }

            if (!isSafeTail) deadMask |= 1 << i;
        }

        // --- FASE 3: Risoluzione Head-to-Head ---
        var h2hCandidates = aliveMask & ~deadMask;
        
        // (x & (x-1)) != 0 controlla se c'è più di 1 bit settato (minimo 2 per collisione)
        if (h2hCandidates != 0 && (h2hCandidates & (h2hCandidates - 1)) != 0)
        {
            var outerMask = h2hCandidates;
            while (outerMask != 0)
            {
                var i = BitOperations.TrailingZeroCount(outerMask);
                outerMask &= outerMask - 1;

                var headA = nextHeads[i];
                var lenA = lengths[i];

                // Loop interno ottimizzato: controlla solo j > i
                var innerMask = outerMask; 
                while (innerMask != 0)
                {
                    var j = BitOperations.TrailingZeroCount(innerMask);
                    innerMask &= innerMask - 1;

                    if (headA == nextHeads[j])
                    {
                        var lenB = lengths[j];
                        if (lenA <= lenB) deadMask |= 1 << i;
                        if (lenB <= lenA) deadMask |= 1 << j;
                    }
                }
            }
        }

        // --- FASE 4: Commit ---
        var commitMask = aliveMask;
        while (commitMask != 0)
        {
            var i = BitOperations.TrailingZeroCount(commitMask);
            commitMask &= commitMask - 1;

            var snake = System[i];
            
            // Rimuoviamo il vecchio corpo PRIMA di aggiornare
            Snakes.Xor(snake.Body);

            if ((deadMask & (1 << i)) != 0)
            {
                snake.Kill();
            }
            else
            {
                var eating = (eatMask & (1 << i)) != 0;
                var damage = Hazards.IsSet(nextHeads[i]) ? hazardDamage : 0;

                snake.UpdateAfterMove(nextHeads[i], eating, damage + 1);

                Snakes.Or(snake.Body);
                if (eating) Food.Unset(nextHeads[i]);
            }
        }
    }

    // --- MOVE GENERATION & VALIDATION ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int index)
    {
        var snake = System[index];
        if (snake.IsDead) return 0;

        var myLen = snake.Length;
        var mask = GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail, index, myLen);

        return mask == 0 ? (byte)0 : FilterRiskyMoves(mask, index, myLen, snake.Head);
    }

    // Overload di compatibilità per Engine (che potrebbe non avere la lunghezza)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(ushort head, ushort tail, ushort neck, int index) 
        => GetLegalMoves(head, tail, neck, index, System[index].Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(ushort headPosition, ushort tailPosition, ushort elementBeforeTailPosition, int heroIndex, int heroLength)
    {
        // 1. Single Fetch dei vicini
        var neighbors = _neighborsMatrix.GetAll(headPosition);
        byte legalMoves = 0;

        // 2. Check scalare sulle 4 direzioni
        // Nota: Passiamo i parametri necessari per evitare riletture
        if (IsMoveSafeFromStatic(neighbors[0], tailPosition, elementBeforeTailPosition)) legalMoves |= Moves.Up;
        if (IsMoveSafeFromStatic(neighbors[1], tailPosition, elementBeforeTailPosition)) legalMoves |= Moves.Down;
        if (IsMoveSafeFromStatic(neighbors[2], tailPosition, elementBeforeTailPosition)) legalMoves |= Moves.Left;
        if (IsMoveSafeFromStatic(neighbors[3], tailPosition, elementBeforeTailPosition)) legalMoves |= Moves.Right;

        return legalMoves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNewHeadPosition(ushort head, byte move) => _neighborsMatrix.Get(head, move);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMoveSafeFromStatic(ushort pos, ushort tail, ushort neck)
    {
        if (!NeighborsMatrix.IsValid(pos)) return false;
        if (Snakes.IsSet(pos))
        {
            // Eccezioni: Coda (se non protetta dal collo)
            if (pos != tail) return false;
            if (pos == neck) return false;
            // Cibo sulla coda -> Coda cresce -> Collisione
            if (Food.IsSet(pos)) return false; 
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FilterRiskyMoves(byte mask, int myIndex, int myLen, ushort head)
    {
        var myNeighbors = _neighborsMatrix.GetAll(head);

        // Se la mossa è legale (bit a 1), controlliamo se è rischiosa (suicidio/vicolo cieco)
        // Se rischiosa, spegniamo il bit con XOR (che funge da toggle off sicuro perché sappiamo che è 1)
        if ((mask & Moves.Up) != 0 && !IsMoveSafeDynamic(myNeighbors[0], myIndex, myLen)) mask ^= Moves.Up;
        if ((mask & Moves.Down) != 0 && !IsMoveSafeDynamic(myNeighbors[1], myIndex, myLen)) mask ^= Moves.Down;
        if ((mask & Moves.Left) != 0 && !IsMoveSafeDynamic(myNeighbors[2], myIndex, myLen)) mask ^= Moves.Left;
        if ((mask & Moves.Right) != 0 && !IsMoveSafeDynamic(myNeighbors[3], myIndex, myLen)) mask ^= Moves.Right;

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMoveSafeDynamic(ushort targetPos, int myIndex, int myLen)
    {
        // Nota: IsValid già controllato prima, ma per sicurezza nel metodo privato lo teniamo
        if (!NeighborsMatrix.IsValid(targetPos)) return false;

        var targetNeighbors = _neighborsMatrix.GetAll(targetPos);
        if (IsSuicidal(targetNeighbors, myIndex, myLen)) return false;
        if (IsDeadEnd(targetNeighbors)) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSuicidal(Vector64<ushort> targetNeighbors, int myIndex, int myLen)
    {
        for (var i = 0; i < System.Count; i++)
        {
            if (i == myIndex) continue;
            var enemy = System[i];
            
            // Se nemico morto o più piccolo, non è suicidio
            if (enemy.IsDead || enemy.Length < myLen) continue;

            // SIMD Check: Testa nemica adiacente?
            var vEnemyHead = Vector64.Create(enemy.Head);
            if (Vector64.Equals(targetNeighbors, vEnemyHead) != Vector64<ushort>.Zero) return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDeadEnd(Vector64<ushort> neighbors)
    {
        // Se c'è ALMENO una via libera (IsValid E Unset), NON è un DeadEnd.
        // Accesso scalare rapido ai 4 elementi del vettore
        if (NeighborsMatrix.IsValid(neighbors[0]) && Snakes.IsUnset(neighbors[0])) return false;
        if (NeighborsMatrix.IsValid(neighbors[1]) && Snakes.IsUnset(neighbors[1])) return false;
        if (NeighborsMatrix.IsValid(neighbors[2]) && Snakes.IsUnset(neighbors[2])) return false;
        if (NeighborsMatrix.IsValid(neighbors[3]) && Snakes.IsUnset(neighbors[3])) return false;
        
        return true;
    }

    public void SimulateRandomFoodSpawn(int foodSpawnChance, int minimumFood, int area)
    {
        if (Food.PopCount() >= minimumFood && Random.Shared.Next(0, 100) >= foodSpawnChance) return;
        for (var i = 0; i < 10; i++)
        {
            var spot = (ushort)Random.Shared.Next(0, area);
            if (!Snakes.IsUnset(spot) || !Food.IsUnset(spot)) continue;
            Food.Set(spot);
            break;
        }
    }
}