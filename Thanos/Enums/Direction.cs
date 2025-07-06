namespace Thanos.Enums;

/// <summary>
///     Enum che rappresenta le quattro direzioni cardinali.
///     Utilizza il tipo sottostante `byte` per ottimizzare la memoria e velocizzare i confronti.
///     Ideale per scenari in cui le direzioni vengono confrontate o memorizzate frequentemente.
///     A differenza del tipo predefinito `int` (32 bit), `byte` occupa solo 8 bit, riducendo l'uso di memoria quando sono
///     necessari pochi valori distinti.
///     Ogni valore dell'enum occuperà esattamente 1 byte (8 bit) in memoria invece di 4 byte con `int`.
/// </summary>
public enum Direction : byte
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
}