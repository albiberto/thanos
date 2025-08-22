using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Memory;

namespace Thanos.War.Snake;

public readonly ref struct WarSnakes(in MemoryLayout layout, Span<byte> snakesMemory)
{
    private readonly MemoryLayout _layout = layout;
    private readonly Span<byte> _snakesMemory = snakesMemory;
    
    public WarSnake this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Inlining aggressivo per performance
        get
        {
            var stride = _layout.SnakeStride;
            var healthSize = _layout.SnakeHealthSize;
            var anatomySize = _layout.SnakeAnatomySize;
            var headerSize = _layout.SnakeHeaderSize;
            
            // Ottiene il blocco di memoria per un singolo serpente.
            var singleSnakeMemoryBlock = _snakesMemory.Slice(index * stride, stride);

            // Dividiamo (Slice) il blocco di memoria nelle sue tre parti corrette.
            // Otteniamo i riferimenti ('ref') alle aree di memoria corrette.
            var healthSpan = singleSnakeMemoryBlock[..healthSize];
            ref var health = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(healthSpan));
            
            var anatomySpan = singleSnakeMemoryBlock.Slice(healthSize, anatomySize);
            ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(anatomySpan));
            
            var bodyByteSpan = singleSnakeMemoryBlock[headerSize..];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(bodyByteSpan);

            // Creiamo e ritorniamo la vista WarSnake.
            return new WarSnake(ref health, ref anatomy, bodySpan);
        }
    }
}