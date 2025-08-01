using System.Runtime.CompilerServices;

namespace Thanos.BitMasks;

public enum CellContent : byte
{
    Empty,
    Food,
    Hazard,
    EnemySnake // Utile per distinguere da un hazard generico, se necessario
}