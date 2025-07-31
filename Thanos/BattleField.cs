using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos;
using Thanos.BitMasks;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BattleField
{
    public fixed uint Field[Configuration.MaxHeight * Configuration.MaxWidth];

}