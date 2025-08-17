using Thanos.War;

namespace Thanos.MCST;

public readonly ref struct HeuristicMoveFinder(ref WarSnake snake, WarArena arena, ReadOnlySpan<MoveDirection> legalMoves)
{
    private readonly WarSnake _snake = snake;
    private readonly WarArena _arena = arena;
    private readonly ReadOnlySpan<MoveDirection> _legalMoves = legalMoves;

    public MoveDirection FindBestMove()
    {
        if (_legalMoves.IsEmpty)
        {
            return MoveDirection.Up; // Morte inevitabile
        }

        MoveDirection bestMove = _legalMoves[0];
        float bestScore = -1.0f;

        // Itera sulle mosse legali e calcola un punteggio per ciascuna
        foreach (var move in _legalMoves)
        {
            float currentScore = 0.0f;
            
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

        return bestMove;
    }
}