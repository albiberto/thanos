using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thanos.Common;
using Thanos.Shared;
using Thanos.SourceGen;
using Thanos.War.Structures;

namespace Thanos.War;

public readonly ref struct Arena(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes,
    NeighborsMatrix neighborsMatrix)
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
            for (var i = 0; i < orderedIds.Length; i++)
            {
                if (orderedIds[i] != snakeData.Id) continue;

                var snake = System[i];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
                break;
            }

        foreach (var coordinate in board.Food) Food.Set(coordinate);
        foreach (var coordinate in board.Hazards) Hazards.Set(coordinate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }

    // --- SIMULATION ENGINE ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SimulateTurn(ReadOnlySpan<byte> moves, int hazardDamage)
    {
        Span<ushort> nextHeads = stackalloc ushort[Constants.MaxSnakesCount];
        Span<ushort> tails = stackalloc ushort[Constants.MaxSnakesCount];
        Span<int> lengths = stackalloc int[Constants.MaxSnakesCount];

        var aliveMask = 0;
        var deadMask = 0;
        var eatMask = 0;

        // --- FASE 1: Snapshot & Move Calculation ---
        for (var snakeIndex = 0; snakeIndex < System.Count; snakeIndex++)
        {
            var snake = System[snakeIndex];
            if (snake.IsDead) continue;

            var nextHead = _neighborsMatrix.Get(snake.Head, moves[snakeIndex]);

            // Cache su stack
            nextHeads[snakeIndex] = nextHead;
            lengths[snakeIndex] = snake.Length;
            tails[snakeIndex] = snake.Tail;

            aliveMask |= 1 << snakeIndex;

            if (NeighborsMatrix.IsOutOfBound(nextHead))
                deadMask |= 1 << snakeIndex;
            else if (Food.IsSet(nextHead)) eatMask |= 1 << snakeIndex;
        }

        // --- FASE 2: Risoluzione Collisioni Corpi (Tail Chasing) ---

        // wallsSurvivors: Serpenti vivi che NON si sono schiantati sui muri.
        var wallsSurvivorsMask = aliveMask & ~deadMask;

        // movingTailsMask: Sottoinsieme dei sopravvissuti che NON mangiano.
        // Solo le code di questi serpenti libereranno la casella occupata.
        var movingTailsMask = wallsSurvivorsMask & ~eatMask;

        while (wallsSurvivorsMask != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(wallsSurvivorsMask);
            wallsSurvivorsMask &= wallsSurvivorsMask - 1;

            var nextHead = nextHeads[snakeIndex];

            // Se la casella è libera, siamo salvi.
            if (Snakes.IsUnset(nextHead)) continue;

            // Se la casella è occupata, potremmo venir salvati da una coda che si sposta, cerchiamola
            var isMovingTail = false;
            for (var j = 0; j < System.Count; j++)
            {
                // Filtro Negativo: Scartiamo J se non libera la casella che ci serve.
                // Saltiamo l'iterazione (continue) se:
                // 1. La coda di J è statica (non si muoverà).
                //    Non presente nella lista dei serpenti la cui coda si muoverà
                // 2. La coda di J non coincide con la nostra destinazione (la posizione della mia futura testa).
                if ((movingTailsMask & (1 << j)) == 0 || nextHead != tails[j]) continue;

                // Se arriviamo qui: J si muove E libera esattamente nextHead.
                isMovingTail = true;
                break;
            }

            if (!isMovingTail) deadMask |= 1 << snakeIndex;
        }

        // --- FASE 3: Risoluzione Head-to-Head ---
        // bodyClashSurvivorsMask: Serpenti vivi che NON si sono schiantati contro altri serpenti o contro se stessi.
        var bodyClashSurvivorsMask = aliveMask & ~deadMask;

        // (x & (x-1)) != 0 controlla se c'è più di 1 bit settato (minimo 2 per collisione, ci devono essere due serpenti vivi almeno sull'arena)
        if (bodyClashSurvivorsMask != 0 && (bodyClashSurvivorsMask & (bodyClashSurvivorsMask - 1)) != 0)
        {
            var outerMask = bodyClashSurvivorsMask;
            while (outerMask != 0)
            {
                var snakeIndex = BitOperations.TrailingZeroCount(outerMask);
                outerMask &= outerMask - 1;

                var headA = nextHeads[snakeIndex];
                var lengthA = lengths[snakeIndex];

                var innerMask = outerMask;
                while (innerMask != 0)
                {
                    var enemyIndex = BitOperations.TrailingZeroCount(innerMask);
                    innerMask &= innerMask - 1;

                    if (headA != nextHeads[enemyIndex]) continue;

                    var lengthB = lengths[enemyIndex];
                    if (lengthA <= lengthB) deadMask |= 1 << snakeIndex;
                    if (lengthB <= lengthA) deadMask |= 1 << enemyIndex;
                }
            }
        }

        // --- FASE 4: Commit ---
        // commitMask: Serpenti soravvisuti nelle faci precedenti, a questo punti alive mask contiene solo i sopravvisuti quindi uso direttamnte lei
        var commitMask = aliveMask;
        while (commitMask != 0)
        {
            var snakeIndex = BitOperations.TrailingZeroCount(commitMask);
            commitMask &= commitMask - 1;

            var snake = System[snakeIndex];

            // Rimuoviamo il vecchio corpo PRIMA di aggiornare
            Snakes.Xor(snake.Body);

            if ((deadMask & (1 << snakeIndex)) != 0)
            {
                snake.Kill();
            }
            else
            {
                var eating = (eatMask & (1 << snakeIndex)) != 0;
                var damage = Hazards.IsSet(nextHeads[snakeIndex]) ? hazardDamage : 0;

                snake.UpdateAfterMove(nextHeads[snakeIndex], eating, damage + 1);

                Snakes.Or(snake.Body);
                if (eating) Food.Unset(nextHeads[snakeIndex]);
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int index)
    {
        var snake = System[index];
        if (snake.IsDead) return 0;

        var mask = GetLegalMoves(snake.Head, snake.Tail, snake.Tail != snake.PreTail);

        return mask == 0 ? (byte)0 : FilterRiskyMoves(mask, index, snake.Length, snake.Head);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(ushort head, ushort tail, bool isUnrolled)
    {
        var neighbors = _neighborsMatrix.GetAll(head);
        byte moves = 0;

        // 3. Unrolled Loop con la TUA logica esatta
        
        // --- UP ---
        var p = neighbors[0];
        if (NeighborsMatrix.IsValid(p))
        {
            // Se vuoto OK.
            // Se è la coda: OK solo se srotolato E senza cibo.
            if (Snakes.IsUnset(p) || (p == tail && isUnrolled && Food.IsUnset(p))) 
                moves |= Moves.Up;
        }

        // --- DOWN ---
        p = neighbors[1];
        if (NeighborsMatrix.IsValid(p))
        {
            if (Snakes.IsUnset(p) || (p == tail && isUnrolled && Food.IsUnset(p))) 
                moves |= Moves.Down;
        }

        // --- LEFT ---
        p = neighbors[2];
        if (NeighborsMatrix.IsValid(p))
        {
            if (Snakes.IsUnset(p) || (p == tail && isUnrolled && Food.IsUnset(p))) 
                moves |= Moves.Left;
        }

        // --- RIGHT ---
        p = neighbors[3];
        if (NeighborsMatrix.IsValid(p))
        {
            if (Snakes.IsUnset(p) || (p == tail && isUnrolled && Food.IsUnset(p))) 
                moves |= Moves.Right;
        }

        return moves;
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