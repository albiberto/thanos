using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CircularQueueState
{
    // Questi 3 byte vivono nel Pool di memoria (Heap/Unmanaged)
    public byte Length;
    public byte HeadIndex;
    public byte TailIndex;
    
    // Helper per azzeramento veloce. 
    // Il JIT lo inlinerà come tre istruzioni MOV [addr], 0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Length = 0;
        HeadIndex = 0;
        TailIndex = 0;
    }
}