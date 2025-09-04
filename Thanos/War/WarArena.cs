using System.Runtime.CompilerServices;
using Thanos.Common;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.War;

public readonly ref struct WarArena(WarGrid grid, WarSnakes snakes)
{
    public readonly WarGrid Grid = grid;
    public readonly WarSnake Me = me;
    public readonly Enemies Enemies = enemies;

    public bool GameOver => Me.Dead;

    public byte GetLegalMoves() => GetLegalMoves(Me.Head);

    public byte GetLegalMoves(ushort headPosition)
    {
        // Step 1: Get all potential neighbor positions from the LUT.
        var upPos = Grid.GetNeighbor(headPosition, Moves.Up);
        var downPos = Grid.GetNeighbor(headPosition, Moves.Down);
        var leftPos = Grid.GetNeighbor(headPosition, Moves.Left);
        var rightPos = Grid.GetNeighbor(headPosition, Moves.Right);

        // Step 2: Check each position and convert the boolean result to a byte (0 or 1).
        var isUpValid = Grid.IsUnset(upPos);
        var upValid = Unsafe.As<bool, byte>(ref isUpValid);

        var isDownValid = Grid.IsUnset(downPos);
        var downValid = Unsafe.As<bool, byte>(ref isDownValid);

        var isLeftValid = Grid.IsUnset(leftPos);
        var leftValid = Unsafe.As<bool, byte>(ref isLeftValid);

        var isRightValid = Grid.IsUnset(rightPos);
        var rightValid = Unsafe.As<bool, byte>(ref isRightValid);

        // Step 3: Combine the results into a final bitmask.
        return (byte)((upValid * Moves.Up) | (downValid * Moves.Down) | (leftValid * Moves.Left) | (rightValid * Moves.Right));
    }

    /// <summary>
    ///     Applica una singola mossa allo stato di gioco corrente, modificandolo.
    /// </summary>
    /// <summary>
    ///     Applica una singola mossa allo stato di gioco corrente, modificandolo.
    /// </summary>
    public void ApplySingleMove(byte move, bool logging = false)
    {
        // Rimosso: var me = Me;
        // Ora lavoriamo direttamente sul campo 'Me' della struct.
        if (Me.Dead) return;

        var oldTail = Me.Tail;
        var head = Me.Head;

        // Rimosso: var grid = Grid;
        // Ora lavoriamo direttamente sul campo 'Grid' della struct.

        var newHead = Grid.GetNeighbor(head, move);
        var hasEaten = Grid.IsFood(newHead);

        if (newHead == ushort.MaxValue)
        {
            Me.Kill();
            Grid.RemoveSnake(Me); // Usa direttamente 'Grid' e 'Me'
            return;
        }

        if (Grid.IsSet(newHead))
        {
            // È una collisione fatale, A MENO CHE non stiamo andando sulla nostra coda
            // e NON stiamo mangiando (se non mangiamo, la coda si sposterà).
            var isMovingOntoOwnVacatingTail = newHead == oldTail && !hasEaten;

            if (!isMovingOntoOwnVacatingTail)
            {
                Me.Kill();
                Grid.RemoveSnake(Me);
                return;
            }
        }

        var damage = Grid.IsHazard(newHead) ? 10 : 1; // Danno base 1, 10 su hazard
        Me.Move(newHead, hasEaten, damage); // Modifica direttamente lo stato di 'Me'

        if (Me.Dead)
        {
            Grid.RemoveSnake(Me);
            return;
        }

        // Modifica direttamente lo stato di 'Grid'
        Grid.SynchronizeSnakeOnGrid(Me, oldTail, hasEaten);
        if (hasEaten) Grid.RemoveFood(newHead);
    }

    public float Outcome() => OutcomeSolo();

    // if (Me.Dead) return -1.0f;
    // return _liveSnakesCount <= 1 ? 1.0f : 0.0f;
    private float OutcomeSolo()
    {
        if (Me.Dead) return -1.0f; // Sconfitta

        var availableSquares = Grid.Geography.Area;
        return Me.Length >= availableSquares
            ? 1.0f // Vittoria: hai riempito la mappa! 
            : 0.0f; // Partita in corso
    }
}