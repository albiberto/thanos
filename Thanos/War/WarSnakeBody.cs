using System.Runtime.CompilerServices;

namespace Thanos.War;

/// <summary>
/// Un wrapper ref struct che fornisce un accesso sicuro e "array-like"
/// alla memoria del corpo di un WarSnake.
/// </summary>
public readonly unsafe ref struct WarSnakeBody(ushort* start, uint capacity, int length)
{
    public readonly uint Capacity = capacity;

    /// <summary>
    /// Fornisce un accesso sicuro ai segmenti del corpo tramite un indexer.
    /// </summary>
    public ref ushort this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Aggiungiamo un controllo sui limiti per la sicurezza
            if (index < 0 || index >= length)
            {
                throw new IndexOutOfRangeException();
            }
            return ref start[index];
        }
    }

    public int Length => length;
}