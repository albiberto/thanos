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
        var stackedMask = 0;

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
            if (snake.IsTailStacked) stackedMask |= 1 << snakeIndex;
            if (NeighborsMatrix.IsOutOfBound(nextHead))
            {
                deadMask |= 1 << snakeIndex;
            }
            else if (Food.IsSet(nextHead))
            {
                eatMask |= 1 << snakeIndex;
            }
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

            // CASO 1: MORTE (Full Clear)
            if ((deadMask & (1 << snakeIndex)) != 0)
            {
                Snakes.Xor(snake.Body); // Rimuovi tutto
                snake.Kill();
            }
            // CASO 2: SOPRAVVISSUTO (Delta Update)
            else
            {
                var eating = (eatMask & (1 << snakeIndex)) != 0;
                var damage = Hazards.IsSet(nextHeads[snakeIndex]) ? hazardDamage : 0;
                
                var newHead = nextHeads[snakeIndex];
                var oldTail = tails[snakeIndex];

                // 1. Aggiornamento Stato Interno
                snake.UpdateAfterMove(newHead, eating, damage + 1);

                // 2. Aggiornamento Globale (Ottimizzato)
                
                // UNSET: Rimuoviamo la coda SOLO se:
                // a) Non stiamo mangiando (altrimenti la coda cresce/resta ferma)
                // b) La coda NON era impilata (check su registro stackedMask)
                if (!eating && (stackedMask & (1 << snakeIndex)) == 0)
                {
                    Snakes.Unset(oldTail);
                }

                // SET: La testa vince sempre (anche se era la stessa cella della coda)
                Snakes.Set(newHead);

                if (eating) Food.Unset(newHead);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int index)
    {
        var snake = System[index];
        if (snake.IsDead) return 0;

        // 1. Fetch unico dalla memoria (SIMD Load)
        var neighbors = _neighborsMatrix.GetAll(snake.Head);

        // 2. Estrazione scalare sui registri (Cost 0 dopo inlining)
        var up = neighbors.GetElement(0);
        var down = neighbors.GetElement(1);
        var left = neighbors.GetElement(2);
        var right = neighbors.GetElement(3);

        // 3. Calcolo Legalità (Branchless friendly)
        var mask = GetLegalMoves(up, down, left, right, snake.Tail, snake.Tail != snake.PreTail);

        // 4. Filtro Rischi (Solo se necessario)
        return mask == 0
            ? (byte)0
            : FilterRiskyMoves(up, down, left, right, mask, index, snake.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetLegalMoves(ushort up, ushort down, ushort left, ushort right, ushort tail, bool isUnrolled)
    {
        byte moves = 0;

        // Nota: Food.IsUnset è veloce, lo facciamo solo nel ramo 'else' (p == tail)
        // L'ordine (IsUnset || ...) sfrutta lo short-circuit per il caso comune (casella vuota).

        // --- UP ---
        if (NeighborsMatrix.IsValid(up))
            if (Snakes.IsUnset(up) || (up == tail && isUnrolled && Food.IsUnset(up)))
                moves |= Moves.Up;

        // --- DOWN ---
        if (NeighborsMatrix.IsValid(down))
            if (Snakes.IsUnset(down) || (down == tail && isUnrolled && Food.IsUnset(down)))
                moves |= Moves.Down;

        // --- LEFT ---
        if (NeighborsMatrix.IsValid(left))
            if (Snakes.IsUnset(left) || (left == tail && isUnrolled && Food.IsUnset(left)))
                moves |= Moves.Left;

        // --- RIGHT ---
        if (NeighborsMatrix.IsValid(right))
            if (Snakes.IsUnset(right) || (right == tail && isUnrolled && Food.IsUnset(right)))
                moves |= Moves.Right;

        return moves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FilterRiskyMoves(ushort up, ushort down, ushort left, ushort right, byte mask, int myIndex,
        int myLength)
    {
        // Check UP
        if ((mask & Moves.Up) != 0 && !IsMoveSafeDynamic(up, myIndex, myLength))
            mask ^= Moves.Up;

        // Check DOWN
        if ((mask & Moves.Down) != 0 && !IsMoveSafeDynamic(down, myIndex, myLength))
            mask ^= Moves.Down;

        // Check LEFT
        if ((mask & Moves.Left) != 0 && !IsMoveSafeDynamic(left, myIndex, myLength))
            mask ^= Moves.Left;

        // Check RIGHT
        if ((mask & Moves.Right) != 0 && !IsMoveSafeDynamic(right, myIndex, myLength))
            mask ^= Moves.Right;

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMoveSafeDynamic(ushort targetHead, int myIndex, int myLength)
    {
        // 1. Fetch unico SIMD dei vicini della futura testa
        var targetNeighbors = _neighborsMatrix.GetAll(targetHead);

        // --- STEP A: HEAD-TO-HEAD (Suicide Check) ---
        // Integrato qui per risparmiare una chiamata e riusare il registro 'targetNeighbors'
        for (var i = 0; i < System.Count; i++)
        {
            if (i == myIndex) continue;
            var enemy = System[i];

            // Se nemico morto o più piccolo, non è suicidio
            if (enemy.IsDead || enemy.Length < myLength) continue;

            // Se uno dei miei futuri vicini è la testa di un nemico pericoloso -> UNSAFE
            var vEnemyHead = Vector64.Create(enemy.Head);
            if (Vector64.Equals(targetNeighbors, vEnemyHead) != Vector64<ushort>.Zero)
                return false;
        }

        // --- STEP B: DEAD END (Vicolo Cieco Check) ---
        // Cerchiamo ALMENO UNA via di fuga libera.
        // Basta un vicino Valido E Vuoto (IsUnset) per dire che non siamo in trappola immediata.

        // UP
        var n = targetNeighbors.GetElement(0);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // DOWN
        n = targetNeighbors.GetElement(1);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // LEFT
        n = targetNeighbors.GetElement(2);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // RIGHT
        n = targetNeighbors.GetElement(3);
        if (NeighborsMatrix.IsValid(n) && Snakes.IsUnset(n)) return true;

        // Nessuna uscita trovata -> DEAD END -> UNSAFE
        return false;
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