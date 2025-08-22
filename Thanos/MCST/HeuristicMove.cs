using Thanos.War;
using Thanos.War.Arena;
using Thanos.War.Snake;

namespace Thanos.MCST;

public readonly ref struct HeuristicMoveFinder(ref WarSnake warSnake, WarArena arena, byte legalMoveSet)
{
    public byte FindBestMove()
    {
        if (legalMoveSet == Moves.None)
        {
            return Moves.Up; // Morte inevitabile, restituisce una mossa di default.
        }

        var bestMove = Moves.None;
        var bestScore = -1.0f;

        // CAMBIAMENTO: Itera sulle direzioni possibili e controlla se il bit è acceso.
        foreach (var move in Moves.AllDirections)
        {
            if ((legalMoveSet & move) != 0)
            {
                // 'move' è una delle mosse legali.
                var currentScore = 0.0f;
            
                // --- QUI VA LA TUA LOGICA EURISTICA ---
                // Esempio:
                // var nextPos = _arena.GetField().GetNeighbor(_snake.Head, move);
                // if (_arena.GetField().IsFood(nextPos)) { currentScore += 10.0f; }
                // currentScore += CalcolaPunteggioSpazio(nextPos);
                // ... etc ...

                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestMove = move;
                }
            }
        }
        
        // Se nessuna euristica ha dato un punteggio, restituisci la prima mossa legale trovata.
        // BitOperations.TrailingZeroCount è un modo ultra-veloce per trovare il primo bit.
        return bestMove != Moves.None ? bestMove : (byte)(1 << System.Numerics.BitOperations.TrailingZeroCount(legalMoveSet));
    }
}