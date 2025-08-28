namespace Thanos.PreWarm.Memory;

/// <summary>
/// Contiene le informazioni di offset e dimensione per una singola LUT.
/// </summary>
public readonly struct LutInfo(int offset, int area)
{
    public int Offset { get; } = offset; // Offset in byte dall'inizio del blocco di memoria
    public int Area { get; } = area;   // Numero di elementi nella LUT
}