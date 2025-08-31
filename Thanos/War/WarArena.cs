using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.SourceGen;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War;

public readonly ref struct WarArena(WarGrid grid, WarSnake me, Enemies enemies, ReadOnlySpan<Coordinate> conversionsMap, ReadOnlySpan<double> positionalScores)
{
    public readonly WarGrid Grid = grid;
    public readonly WarSnake Me = me;
    private readonly Enemies Enemies = enemies;
    
    public bool ILose => Me.Dead;
    
    private readonly ReadOnlySpan<Coordinate> _conversionsMap = conversionsMap;
    private readonly ReadOnlySpan<double> _positionalScores = positionalScores;

    private readonly int _liveSnakesCount;
	
    public byte GetLegalMoves() => Me.Dead 
	    ? (byte)0 
	    : Grid.GetLegalMoves(Me.Head);
    
    public byte GetLegalMoves(ushort position) => Grid.GetLegalMoves(position);

    /// <summary>
	/// Applica una singola mossa allo stato di gioco corrente, modificandolo.
	/// </summary>
	public void ApplySingleMove(byte move)
	{
		if (Me.Dead) return;

		var oldTail = Me.Tail;
		var head = Me.Head;
		
		var newHead = Grid.GetNeighbor(head, move);
		var hasEaten = Grid.IsFood(newHead);
		
		if (Grid.IsOccupied(newHead))
		{
			// È una collisione fatale, A MENO CHE non stiamo andando sulla nostra coda
			// e NON stiamo mangiando (se non mangiamo, la coda si sposterà).
			var isMovingOntoOwnVacatingTail = (newHead == oldTail && !hasEaten);

			if (!isMovingOntoOwnVacatingTail)
			{
				Me.Kill();
				Grid.RemoveSnake(Me);
				return;
			}
		}

		var damage = Grid.IsHazard(newHead) ? 10 : 1; // Danno base 1, 10 su hazard

		Me.Move(newHead, hasEaten, damage);
		
		if (Me.Dead)
		{
			Grid.RemoveSnake(Me);
			return;
		}

		Grid.UpdateSnakePosition(oldTail, newHead, hasEaten);
		if (hasEaten) Grid.RemoveFood(newHead);
	}
	
    public ushort GetMyNeighbor(byte move) => Grid.GetNeighbor(Me.Head, move);
    
	public float Outcome()
	{
		return OutcomeSolo();
		
		if (Me.Dead) return -1.0f;
		return _liveSnakesCount <= 1 ? 1.0f : 0.0f;
	}
	
	private float OutcomeSolo()
	{
		if (Me.Dead) return -1.0f; // Sconfitta

		var availableSquares = Grid.Geography.Area;
		return Me.Length >= availableSquares 
			? 1.0f // Vittoria: hai riempito la mappa! 
			: 0.0f; // Partita in corso
	}

    public double Evaluate()
    {
        // 1. Condizione Terminale: Se siamo morti, questo è lo scenario peggiore in assoluto.
        if (Me.Dead) return double.NegativeInfinity;

        var head = Me.Head;
        var health = Me.Health;
        var food = Grid.Food.GetRawData;
        var headCoord = _conversionsMap[head];
        var score = 0.0;

        // --- 2. EURISTICA POSIZIONALE (Statica, dalla LUT) ---
        // Fornisce una "spinta" strategica a lungo termine, favorendo il centro
        // e penalizzando la vicinanza ai bordi.
        score += _positionalScores[head];

        // --- 3. EURISTICA DEL CIBO (Dinamica) ---
        // Calcola l'urgenza di mangiare in base alla salute e alla distanza dal cibo più vicino.
        score += HeuristicWeights.FoodWeight * CalculateFoodIncentive(headCoord, health, food, _conversionsMap);

        // --- 4. EURISTICA DELLO SPAZIO/MOBILITÀ (Dinamica) ---
        // La componente principale: calcola l'area sicura raggiungibile da ora (flood fill).
        // Questo è il nostro indicatore di libertà di movimento a medio termine.
        var safeSpace = EstimateSafeSpaceBitset(head, HeuristicWeights.SafeSpaceNodeBudget, in Grid);
        score += HeuristicWeights.SpaceWeight * safeSpace;

        // --- 5. EURISTICA ANTI-TRAPPOLA (Dinamica, Visione a Breve Termine) ---
        // Riconosce il pericolo imminente. Se dalla nostra posizione attuale abbiamo
        // una sola via di fuga, siamo quasi in trappola e dobbiamo penalizzare pesantemente questo stato.
        var immediateMoves = Grid.GetLegalMoves(head);
    
        // BitOperations.IsPow2 è un modo velocissimo per controllare se c'è un solo bit a '1'.
        // Aggiungiamo un controllo sulla lunghezza per non penalizzare i primissimi turni di gioco.
        if (BitOperations.IsPow2(immediateMoves) && Me.Length > 3)
        {
            // Applica una penalità pesante e fissa per le situazioni disperate.
            score += HeuristicWeights.TrapPenaltyValue;
        }

        return score;
    }

// --- Euristiche di Supporto ---
/// <summary>
///     Calcola l'incentivo al cibo trovando la distanza minima in modo super-performante
///     usando la LUT per le coordinate.
/// </summary>
private static double CalculateFoodIncentive(Coordinate head, int health, ReadOnlySpan<ulong> food, ReadOnlySpan<Coordinate> map)
    {
        var distance = int.MaxValue;

        for (var i = 0; i < food.Length; i++)
        {
            var chunk = food[i];
            if (chunk == 0) continue;

            while (chunk != 0)
            {
                var bitIndex = BitOperations.TrailingZeroCount(chunk);
                var pos1D = (ushort)((i << 6) + bitIndex);

                var foodCoords = map[pos1D];

                var d = Abs(head.X - foodCoords.X) + Abs(head.Y - foodCoords.Y);
                if (d < distance)
                {
                    distance = d;
                    if (distance == 1) goto EndLoop;
                }

                chunk &= ~(1UL << bitIndex);
            }
        }

        EndLoop: // Etichetta per uscire da entrambi i cicli
        if (distance is >= int.MaxValue or 0) return 0.0;

        var urgency = 100.0 - health + 30.0;
        return urgency / distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n)
    {
        // Maschera con tutti i bit a 1 se n è negativo, 0 se positivo
        var mask = n >> 31;
        // (n XOR mask) - mask
        return (n + mask) ^ mask;
    }

    private static int EstimateSafeSpaceBitset(ushort start, int nodeBudget, in WarGrid grid)
    {
	    var area = grid.Geography.Area;
        if (area <= 0) return 0;
        var words = (area + 63) >> 6;
        ulong[]? rentedVisited = null;
        var visitedBits = words <= 16 ? stackalloc ulong[words] : (rentedVisited = ArrayPool<ulong>.Shared.Rent(words)).AsSpan(0, words);
        visitedBits.Clear();

        ushort[]? rentedQueue = null;
        var qCap = Math.Min(area, Math.Max(nodeBudget, 16));
        var queue = qCap <= 1024 ? stackalloc ushort[qCap] : (rentedQueue = ArrayPool<ushort>.Shared.Rent(qCap)).AsSpan(0, qCap);

        int qHead = 0, qTail = 0, count = 0, visitedCount = 0;

        static bool TryMarkVisited(Span<ulong> bits, int idx)
        {
            var word = idx >> 6;
            var m = 1UL << (idx & 63);
            if ((bits[word] & m) != 0) return false;
            bits[word] |= m;
            return true;
        }

        if (TryMarkVisited(visitedBits, start))
        {
            queue[qTail++] = start;
            visitedCount = 1;
        }

        while (qHead != qTail && count < nodeBudget)
        {
            var pos = queue[qHead];
            qHead = (qHead + 1) % qCap;
            count++;
            var moves = grid.GetLegalMoves(pos);
            while (moves != 0)
            {
                var moveIndex = BitOperations.TrailingZeroCount(moves);
                var currentMove = (byte)(1 << moveIndex);
                var next = grid.GetNeighbor(pos, currentMove);
                if (next != ushort.MaxValue && TryMarkVisited(visitedBits, next))
                {
                    visitedCount++;
                    if ((qTail + 1) % qCap != qHead)
                    {
                        queue[qTail] = next;
                        qTail = (qTail + 1) % qCap;
                    }
                }

                moves &= (byte)~currentMove;
            }
        }

        if (rentedVisited is not null) ArrayPool<ulong>.Shared.Return(rentedVisited);
        if (rentedQueue is not null) ArrayPool<ushort>.Shared.Return(rentedQueue);

        return visitedCount;
    }
}