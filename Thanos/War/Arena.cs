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

        // 1. Mapping Serpenti (O(N^2) apparente, ma N=4/8 è velocissimo per la CPU)
        // Il JIT gestisce questo pattern in modo eccellente senza overhead di chiamate.
        foreach (var snakeData in board.Snakes)
        {
            for (var i = 0; i < orderedIds.Length; i++)
            {
                // Reference Equality se le stringhe sono internate, o confronto veloce
                if (orderedIds[i] != snakeData.Id) continue;
                
                var snake = System[i];
                snake.Initialize(snakeData);
                Snakes.Or(snake.Body);
                break;
            }
        }

        // 2. Caricamento Bitboards
        foreach (var p in board.Food) Food.Set(p);
        foreach (var p in board.Hazards) Hazards.Set(p);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloneFrom(in Arena source)
    {
        System.CopyFrom(in source.System);
        source.Food.CopyTo(Food);
        source.Hazards.CopyTo(Hazards);
        source.Snakes.CopyTo(Snakes);
    }
    
    // --- SIMULATION ENGINE (BITMASK + STACK) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SimulateTurn(ReadOnlySpan<byte> moves, int hazardDamage)
    {
        Span<ushort> nextHeads = stackalloc ushort[Constants.MaxSnakesCount];
        
        var aliveMask = 0;
        var deadMask = 0;
        var eatMask = 0;

        // FASE 1 COMBINATA: Snapshot Stato + Calcolo Collisioni Statiche
        for (var i = 0; i < System.Count; i++)
        {
            // Controllo vita (Memory Access: System[i].IsDead)
            if (System[i].IsDead) 
            {
                nextHeads[i] = ushort.MaxValue; // Init safe per i morti
                continue; 
            }

            // Se siamo qui, è vivo. Costruiamo la maschera on-the-fly.
            aliveMask |= 1 << i;

            var move = moves[i];
            
            // Calcolo posizione
            var nextPos = _neighborsMatrix.Get(System[i].Head, move);
            nextHeads[i] = nextPos;

            if (nextPos == ushort.MaxValue || Snakes.IsSet(nextPos))
            {
                deadMask |= 1 << i;
                continue; // Salta il check del Cibo! (Risparmio CPU/Cache)
            }

            // Cibo
            if (Food.IsSet(nextPos)) eatMask |= 1 << i;
        }

        // FASE 2: Risoluzione Head-to-Head
        // I "candidati" sono i serpenti che erano vivi E non sono morti contro un muro/corpo nella Fase 1.
        // Solo loro hanno il "diritto" di scontrarsi tra loro.
        var candidatesMask = aliveMask & ~deadMask;

        // Brian Kernighan's algorithm: (n & (n-1)) != 0 controlla se ci sono ALMENO 2 bit settati.
        // Se c'è 0 o 1 solo serpente vivo, è impossibile che avvenga uno scontro testa-a-testa -> Skip.
        var hasPotentialCollisions = candidatesMask != 0 && (candidatesMask & (candidatesMask - 1)) != 0;

        if (hasPotentialCollisions) 
        {
            // Ciclo esterno: Primo sfidante (Snake A)
            for (var snakeIndexA = 0; snakeIndexA < System.Count; snakeIndexA++)
            {
                // Se Snake A non è un candidato valido, saltiamo
                if ((candidatesMask & (1 << snakeIndexA)) == 0) continue;
                
                // HOISTING: Carichiamo la testa di A in un registro locale.
                // Evita di rileggere nextHeads[snakeIndexA] dallo stack nel ciclo interno.
                var headA = nextHeads[snakeIndexA];

                // Ciclo interno: Secondo sfidante (Snake B)
                // Partiamo da A + 1 per evitare confronti doppi (A vs B copre anche B vs A) e auto-confronti.
                for (var snakeIndexB = snakeIndexA + 1; snakeIndexB < System.Count; snakeIndexB++)
                {
                    // --- OTTIMIZZAZIONE UNIFICATA ---
                    // 1. Bitwise Check: Snake B è candidato? (Solo Registri)
                    // 2. Memory Check: Le teste sono diverse? (Lettura Stack)
                    // Grazie allo short-circuit (||), se B non è candidato non tocchiamo nemmeno la memoria.
                    if (((candidatesMask & (1 << snakeIndexB)) == 0) || headA != nextHeads[snakeIndexB]) continue;
                    
                    var lengthA = System[snakeIndexA].Length;
                    var lengthB = System[snakeIndexB].Length;

                    // REGOLE BATTLESNAKE:
                    // 1. Lunghezze uguali -> Muoiono entrambi
                    // 2. Lunghezze diverse -> Muore il più corto
                    if (lengthA <= lengthB) deadMask |= 1 << snakeIndexA; // A muore (è più corto o uguale)
                    if (lengthB <= lengthA) deadMask |= 1 << snakeIndexB; // B muore (è più corto o uguale)
                }
            }
        }

        // FASE 3: Commit (Scrittura in memoria)
        for (var i = 0; i < System.Count; i++)
        {
            // Indica: "Il serpente i era vivo all'inizio di questo turno?"
            // Se la risposta è NO (risultato 0), il continue fa saltare il ciclo.
            if ((aliveMask & (1 << i)) == 0) continue;

            var snake = System[i];
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

    // --- PRUNING INTELLIGENTE ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetSmartMoveMask(int snakeIndex)
    {
        var snake = System[snakeIndex];
        if (snake.IsDead) return 0;

        // 1. Legalità Fisica
        var mask = GetLegalMoves(snake.Head, snake.Tail, snake.ElementBeforeTail);
        return mask == 0 
            ? (byte)0 
            : FilterRiskyMoves(mask, snakeIndex, snake.Length, snake.Head);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte GetLegalMoves(ushort head, ushort tail, ushort neck)
    {
        byte mask = 0;
        
        // Unrolling manuale per le 4 direzioni (più veloce di un loop su array mosse)
        var pos = _neighborsMatrix.Get(head, Moves.Up);
        if (pos != ushort.MaxValue && IsSafe(pos, tail, neck)) mask |= Moves.Up;

        pos = _neighborsMatrix.Get(head, Moves.Down);
        if (pos != ushort.MaxValue && IsSafe(pos, tail, neck)) mask |= Moves.Down;

        pos = _neighborsMatrix.Get(head, Moves.Left);
        if (pos != ushort.MaxValue && IsSafe(pos, tail, neck)) mask |= Moves.Left;

        pos = _neighborsMatrix.Get(head, Moves.Right);
        if (pos != ushort.MaxValue && IsSafe(pos, tail, neck)) mask |= Moves.Right;

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSafe(ushort pos, ushort tail, ushort neck)
    {
        if (!Snakes.IsSet(pos)) return true;
        // Tail chasing consentito se non è il collo
        return pos == tail && pos != neck;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FilterRiskyMoves(byte mask, int myIndex, int myLen, ushort myHead)
    {
        for (var i = 0; i < System.Count; i++)
        {
            if (i == myIndex || System[i].IsDead) continue;
            
            // Nemico pericoloso (>= lunghezza)
            if (System[i].Length >= myLen)
            {
                var eHead = System[i].Head;
                
                // Rimuovi mosse che portano adiacenti alla testa nemica
                if ((mask & Moves.Up) != 0 && IsNeighbor(_neighborsMatrix.Get(myHead, Moves.Up), eHead)) 
                    mask &= (byte)~Moves.Up;
                
                if ((mask & Moves.Down) != 0 && IsNeighbor(_neighborsMatrix.Get(myHead, Moves.Down), eHead)) 
                    mask &= (byte)~Moves.Down;
                
                if ((mask & Moves.Left) != 0 && IsNeighbor(_neighborsMatrix.Get(myHead, Moves.Left), eHead)) 
                    mask &= (byte)~Moves.Left;
                
                if ((mask & Moves.Right) != 0 && IsNeighbor(_neighborsMatrix.Get(myHead, Moves.Right), eHead)) 
                    mask &= (byte)~Moves.Right;
            }
        }

        // Dead End Check (Tunnel 1x1)
        if (mask != 0)
        {
            var safeMask = mask;
            
            if ((mask & Moves.Up) != 0 && IsDeadEnd(_neighborsMatrix.Get(myHead, Moves.Up))) 
                safeMask &= (byte)~Moves.Up;
            
            if ((mask & Moves.Down) != 0 && IsDeadEnd(_neighborsMatrix.Get(myHead, Moves.Down))) 
                safeMask &= (byte)~Moves.Down;
            
            if ((mask & Moves.Left) != 0 && IsDeadEnd(_neighborsMatrix.Get(myHead, Moves.Left))) 
                safeMask &= (byte)~Moves.Left;
            
            if ((mask & Moves.Right) != 0 && IsDeadEnd(_neighborsMatrix.Get(myHead, Moves.Right))) 
                safeMask &= (byte)~Moves.Right;

            // Se tutto è bloccato, meglio provare a sopravvivere che arrendersi subito
            if (safeMask != 0) mask = safeMask;
        }

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsNeighbor(ushort pos, ushort target)
    {
        if (pos == ushort.MaxValue) return false;
        return _neighborsMatrix.Get(pos, Moves.Up) == target ||
               _neighborsMatrix.Get(pos, Moves.Down) == target ||
               _neighborsMatrix.Get(pos, Moves.Left) == target ||
               _neighborsMatrix.Get(pos, Moves.Right) == target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDeadEnd(ushort pos)
    {
        // Conta uscite libere. Se 0 -> Vicolo Cieco.
        if (_neighborsMatrix.Get(pos, Moves.Up) != ushort.MaxValue && !Snakes.IsSet(_neighborsMatrix.Get(pos, Moves.Up))) return false;
        if (_neighborsMatrix.Get(pos, Moves.Down) != ushort.MaxValue && !Snakes.IsSet(_neighborsMatrix.Get(pos, Moves.Down))) return false;
        if (_neighborsMatrix.Get(pos, Moves.Left) != ushort.MaxValue && !Snakes.IsSet(_neighborsMatrix.Get(pos, Moves.Left))) return false;
        if (_neighborsMatrix.Get(pos, Moves.Right) != ushort.MaxValue && !Snakes.IsSet(_neighborsMatrix.Get(pos, Moves.Right))) return false;
        return true;
    }

    // --- UTILS ---

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetNewHeadPosition(ushort head, byte move) => _neighborsMatrix.Get(head, move);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetPlausibleMoves(int index) => GetSmartMoveMask(index);
}