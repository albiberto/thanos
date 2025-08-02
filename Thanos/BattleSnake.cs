// File: BattleSnake.cs

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.BitMasks;

namespace Thanos;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BattleSnake
{
    public const int HeaderSize = 64;

    // CACHE LINE 1 - HEADER (64 bytes)
    public int Health;
    public int Length;
    public int MaxLength;
    public ushort Head;
    private fixed byte _padding[HeaderSize - (sizeof(int) * 3 + sizeof(ushort))]; // Padding calcolato dinamicamente

    // CACHE LINE 2+
    public fixed ushort Body[1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Move(ushort* bodyPtr, ushort newHeadPosition, CellContent content, int hazardDamage = 15)
    {
        // --- 1. Aggiornamento primario di stato e salute in base alla destinazione ---
        // Gestiamo i casi principali che determinano vita, morte o recupero.
        switch (content)
        {
            case CellContent.EnemySnake:
                // Aggiungi qui altre condizioni di morte istantanea (es. CellContent.Wall, CellContent.Self)
                Health = 0;
                return false; // MORTE IMMEDIATA: usciamo subito, non serve aggiornare il corpo.

            case CellContent.Food:
                Health = 100;
                break; // Sopravvive e recupera salute. La crescita è gestita dopo.

            case CellContent.Hazard:
                Health -= hazardDamage;
                break; // Subisce danno.

            case CellContent.Empty:
            default:
                Health -= 1; // Danno base per il movimento.
                break;
        }

        // --- 2. Controllo di morte post-movimento ---
        // Se il danno da hazard o il movimento normale ha ucciso il serpente.
        if (Health <= 0)
        {
            return false; // MORTO: non aggiorniamo il corpo.
        }

        // --- 3. Aggiornamento Corpo e Testa (solo se vivo) ---
        var hasEaten = content == CellContent.Food;
        var canGrow = hasEaten && Length < MaxLength;
        ushort oldHead = Head;

        Head = newHeadPosition;

        if (canGrow)
        {
            // CRESCITA
            Body[Length] = oldHead;
            Length++;
        }
        else
        {
            // SPOSTAMENTO
            if (Length > 1)
            {
                Unsafe.CopyBlock(bodyPtr, bodyPtr + 1, (uint)(Length - 1) * sizeof(ushort));
            }
            if (Length > 0)
            {
                Body[Length - 1] = oldHead;
            }
        }

        return true; // VIVO
    }

    /// <summary>
    /// Resetta lo stato del serpente ai valori iniziali.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset(int maxLength)
    {
        Health = 100;
        Length = 3; // Lunghezza iniziale standard
        MaxLength = maxLength;
        Head = 0; // Posizione iniziale fittizia
        // Il corpo non necessita di essere azzerato perché la memoria è già stata azzerata da Battlefield
        // e la lunghezza ('Length') definisce la porzione valida.
    }
}