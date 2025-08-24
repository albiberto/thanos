using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential)]
public ref struct Node
{
    public int StateSlotId;

    // Connessioni dell'albero tramite indici
    public int ParentIndex;
    public int FirstChildIndex; // Indice del primo figlio
    public int NextSiblingIndex; // Indice del prossimo fratello (per liste concatenate)

    // Statistiche
    public double Wins;
    public int Visits;
    public byte MoveThatLedToThisNode;
    public bool IsTerminal;
}