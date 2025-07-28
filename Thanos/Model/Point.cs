using System.Runtime.CompilerServices;
using Thanos.Enums;
using Direction = Thanos.CollisionMatrix.Direction;

namespace Thanos.Domain;

/// <summary>
///     Struttura immutabile ad alte prestazioni per rappresentare un punto 2D con coordinate uint.
///     Ultra-ottimizzata per deserializzazione JSON e performance massima con operazioni bitwise.
/// </summary>
public struct Point : IEquatable<Point>
{
    /// <summary>
    ///     Coordinata X del punto (uint per valori sempre positivi)
    /// </summary>
    public uint x { get; set; }

    /// <summary>
    ///     Coordinata Y del punto (uint per valori sempre positivi)
    /// </summary>
    public uint y { get; set; }

    /// <summary>
    ///     Lookup table statica per i vettori di direzione.
    ///     Pre-calcolata per evitare calcoli ripetuti e massimizzare la velocità.
    ///     Ordine: Up, Down, Left, Right
    /// </summary>
    private static readonly (int dx, int dy)[] DirectionVectors =
    [
        (0, 1), // Up (incrementa Y)
        (0, -1), // Down (decrementa Y)
        (-1, 0), // Left (decrementa X)
        (1, 0) // Right (incrementa X)
    ];

    /// <summary>
    ///     Lookup table statica per le direzioni opposte.
    ///     Pre-calcolata per performance ottimale in algoritmi di pathfinding.
    /// </summary>
    private static readonly Direction[] OppositeDirections =
    [
        Direction.Down, // Opposite of Up
        Direction.Up, // Opposite of Down
        Direction.Right, // Opposite of Left
        Direction.Left // Opposite of Right
    ];

    /// <summary>
    ///     Inizializza un nuovo punto con le coordinate specificate.
    /// </summary>
    /// <param name="x">Coordinata X</param>
    /// <param name="y">Coordinata Y</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point(uint x, uint y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    ///     Calcola l'hash code del punto utilizzando bit shifting per performance massima.
    ///     Ottimizzato per uint con operazioni bitwise ultra-veloci.
    /// </summary>
    /// <returns>Hash code ottimizzato per performance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => (int)((x << 16) ^ y);

    /// <summary>
    ///     Verifica l'uguaglianza con un altro punto usando confronto diretto delle coordinate.
    ///     Ottimizzato per performance massima senza allocazioni.
    /// </summary>
    /// <param name="other">Punto da confrontare</param>
    /// <returns>True se i punti sono uguali, false altrimenti</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Point other) => x == other.x && y == other.y;

    /// <summary>
    ///     Verifica l'uguaglianza con un oggetto generico.
    ///     Ottimizzato per evitare boxing quando possibile.
    /// </summary>
    /// <param name="obj">Oggetto da confrontare</param>
    /// <returns>True se l'oggetto è un Point uguale, false altrimenti</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Point other && Equals(other);

    /// <summary>
    ///     Calcola la distanza Manhattan tra due punti usando uint per performance massima.
    ///     La distanza Manhattan è la somma delle differenze assolute delle coordinate.
    ///     Ottimizzato per uint eliminando i controlli di segno.
    /// </summary>
    /// <param name="other">Punto di destinazione</param>
    /// <returns>Distanza Manhattan come uint</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ManhattanDistance(Point other)
    {
        // Con uint non servono operazioni bitwise per valore assoluto
        var dx = x > other.x ? x - other.x : other.x - x;
        var dy = y > other.y ? y - other.y : other.y - y;
        return dx + dy;
    }

    /// <summary>
    ///     Crea un nuovo punto spostato nella direzione specificata.
    ///     Utilizza lookup table pre-calcolata per performance massima.
    ///     Gestisce underflow per uint con controlli ottimizzati.
    /// </summary>
    /// <param name="direction">Direzione del movimento</param>
    /// <returns>Nuovo punto spostato (o punto corrente se movimento non valido)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point Move(Direction direction)
    {
        var (dx, dy) = DirectionVectors[(int)direction];

        // Controlli ottimizzati per underflow con uint
        var newX = dx < 0 ? x == 0 ? 0 : x - 1 : x + (uint)dx;
        var newY = dy < 0 ? y == 0 ? 0 : y - 1 : y + (uint)dy;

        return new Point(newX, newY);
    }

    /// <summary>
    ///     Crea un nuovo punto spostato nella direzione specificata senza controlli di bounds.
    ///     UNSAFE: Usa solo quando sei sicuro che non ci sarà underflow.
    ///     Performance massima per situazioni controlled.
    /// </summary>
    /// <param name="direction">Direzione del movimento</param>
    /// <returns>Nuovo punto spostato</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point MoveUnsafe(Direction direction)
    {
        var (dx, dy) = DirectionVectors[(int)direction];
        return new Point((uint)((int)x + dx), (uint)((int)y + dy));
    }

    /// <summary>
    ///     Ottiene la direzione opposta a quella specificata usando lookup table.
    ///     Operazione O(1) per performance massima in algoritmi di pathfinding.
    /// </summary>
    /// <param name="direction">Direzione di input</param>
    /// <returns>Direzione opposta</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Direction GetOpposite(Direction direction) => OppositeDirections[(int)direction];

    /// <summary>
    ///     Converte il punto in hash ulong per cache ultra-veloce.
    ///     Ottimizzato per HashSet<ulong> con performance massima.
    /// </summary>
    /// <returns>Hash ulong per cache bitwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ToHash() => ((ulong)x << 32) | y;

    /// <summary>
    ///     Crea un Point da hash ulong.
    ///     Operazione inversa di ToHash() per cache ultra-veloce.
    /// </summary>
    /// <param name="hash">Hash ulong</param>
    /// <returns>Point decodificato</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point FromHash(ulong hash) => new((uint)(hash >> 32), (uint)(hash & 0xFFFFFFFF));

    /// <summary>
    ///     Verifica se il punto è valido per una board di dimensioni specifiche.
    ///     Ottimizzato per uint con confronti diretti.
    /// </summary>
    /// <param name="width">Larghezza della board</param>
    /// <param name="height">Altezza della board</param>
    /// <returns>True se il punto è dentro i bounds</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(uint width, uint height) => x < width && y < height;

    /// <summary>
    ///     Operatore di uguaglianza ottimizzato per confronti ultra-veloci.
    /// </summary>
    /// <param name="left">Primo punto</param>
    /// <param name="right">Secondo punto</param>
    /// <returns>True se i punti sono uguali</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Point left, Point right) => left.Equals(right);

    /// <summary>
    ///     Operatore di disuguaglianza ottimizzato per confronti ultra-veloci.
    /// </summary>
    /// <param name="left">Primo punto</param>
    /// <param name="right">Secondo punto</param>
    /// <returns>True se i punti sono diversi</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Point left, Point right) => !left.Equals(right);

    /// <summary>
    ///     Rappresentazione testuale del punto nel formato (X, Y).
    ///     Utilizzare solo per debug, non in percorsi critici per performance.
    /// </summary>
    /// <returns>Stringa rappresentante il punto</returns>
    public override string ToString() => $"({x}, {y})";
}