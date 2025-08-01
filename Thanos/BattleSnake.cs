using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

/// <summary>
///     BattleSnake con i propri metodi di modifica
///     Sa come modificare se stesso, ma non conosce i limiti globali
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BattleSnake
{
    public const int HeaderSize = 64;

    // CACHE LINE 1
    // HEADER (64 bytes)
    public int Health; // 4 byte
    public int Length; // 4 byte
    public ushort Head; // 2 bytes
    private fixed byte _padding[64 - 10]; // 54 bytes padding

    // CACHE LINE (da 2 a N)
    // Il body inizia qui
    public fixed ushort Body[1]; // La dimensione reale è gestita dal Battlefield

    /// <summary>
    ///     Muove il serpente in base al contenuto della cella di destinazione,
    ///     aggiornando vita, lunghezza e posizione.
    /// </summary>
    /// <param name="newHeadPosition">La nuova coordinata della testa.</param>
    /// <param name="content">Il contenuto della cella di destinazione.</param>
    /// <param name="hazardDamage">Il danno inflitto da un hazard.</param>
    /// <returns>Restituisce true se il serpente è ancora vivo dopo la mossa, altrimenti false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort newHeadPosition, CellContent content, int hazardDamage = 15)
    {
        var hasEaten = content == CellContent.Food;

        if (hasEaten)
        {
            // CASO CIBO: cresce, non perde salute e non shifta il corpo.
            Health = 100;
            Body[Length] = Head; // La vecchia testa diventa un nuovo pezzo del corpo
            Length++;
        }
        else
        {
            // CASO MOVIMENTO NORMALE (o impatto): perde salute e shifta il corpo.
            switch (content)
            {
                case CellContent.Hazard:
                case CellContent.EnemySnake:
                    Health -= hazardDamage;
                    break;

                case CellContent.Food: // Se mangia ma è a lunghezza massima, è un movimento normale
                case CellContent.Empty:
                default:
                    Health -= 1; // Danno base per il movimento
                    break;
            }

            // Esegue lo shift del corpo per "dimenticare" l'ultima coda
            fixed (ushort* bodyPtr = Body)
            {
                Unsafe.CopyBlock(bodyPtr, bodyPtr + 1, (uint)(Length - 1) * sizeof(ushort));
            }

            Body[Length - 1] = Head;
        }

        // --- 2. Aggiornamento Posizione Testa ---
        Head = newHeadPosition;

        // --- 3. Controllo Finale e Ritorno Stato ---
        return Health > 0; // Vivo ✅ o Morto 💀 
    }
    
    /// <summary>
    /// Resetta lo stato del serpente ai valori iniziali.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Health = 100;
        Length = 3; // O una lunghezza iniziale a tua scelta
        Head = 0;   // O una posizione di partenza non valida/default
    }
}