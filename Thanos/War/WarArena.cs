using System.Runtime.InteropServices;
using Thanos.War.Grid;
using Thanos.War.Snake.Memory;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct WarArena(WarGrid grid, WarSnakesMemoryView snakes)
{
    public readonly WarGrid Grid = grid;
    public readonly WarSnakesMemoryView Snakes = snakes;

    private readonly int _liveSnakesCount;
    
    /// <summary>
    /// Applica una singola mossa allo stato di gioco corrente, modificandolo.
    /// </summary>
    public void ApplySingleMove(byte move)
    {
        var mySnake =  Snakes.Me;
        if (mySnake.Dead) return;

        var oldTail = mySnake.Tail;
        var head = mySnake.Head;
        
        // 1. Calcola la nuova posizione della testa
        var newHead = Grid.GetNeighbor(head, move);

        // 2. Controlla collisione immediata (morte)
        if (Grid.IsOccupied(newHead))
        {
            Grid.KillSnakeOnGrid(mySnake);
            mySnake.Kill();
            return;
        }

        // 3. Controlla interazioni con la cella di destinazione
        var hasEaten = Grid.IsFood(newHead);
        var damage = Grid.IsHazard(newHead) ? 10 : 1; // Danno base 1, 10 su hazard

        // 4. Aggiorna lo stato interno del serpente (corpo, vita, lunghezza)
        mySnake.Move(newHead, hasEaten, damage);
        
        // Se il serpente è morto per il danno subito, esegui la logica di morte
        if (mySnake.Dead)
        {
            Grid.KillSnakeOnGrid(mySnake);
            return;
        }

        // 5. Aggiorna lo stato della griglia (bitboards)
        Grid.UpdateSnakePosition(oldTail, newHead, hasEaten);
        if (hasEaten) Grid.RemoveFood(newHead);
    }
    
    public float Evaluate()
    {
        // if (Snakes.Me.Dead) return -1.0f;
        // return _liveSnakesCount <= 1 ? 1.0f : 0.0f;
        
        return Snakes.Me.Dead ? -1.0f : 0.0f;
    }
}