using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.MCST;

[StructLayout(LayoutKind.Sequential)]
public struct Node
{
    public int StateSlotId;

    public int ParentIndex;
    public int FirstChildIndex;
    public int NextSiblingIndex;

    public double Wins;
    public int Visits;
    public byte MoveThatLedToThisNode;
    public bool IsTerminal;
}