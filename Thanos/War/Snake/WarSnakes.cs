// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
// using Thanos.Memory;
//
// namespace Thanos.War.Snake;
//
// public readonly ref struct Snakes(in SnakeLayout layout, Span<byte> snakesMemory)
// {
//     private readonly SnakeLayout _layout = layout;
//     private readonly Span<byte> _snakesMemory = snakesMemory;
//     
//     public WarSnake this[int index]
//     {
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         get
//         {
//             var stride = _layout.Stride;
//             var healthSize = _layout.HealthSize;
//             var anatomySize = _layout.AnatomySize;
//             var headerSize = _layout.HeaderSize;
//             
//             // Ottiene il blocco di memoria per un singolo serpente.
//             var snakeMemory = _snakesMemory.Slice(index * stride, stride);
//
//             // Dividiamo (Slice) il blocco di memoria nelle sue tre parti e Otteniamo i riferimenti ('ref') alle aree di memoria.
//             var healthMemory = snakeMemory[..healthSize];
//             ref var health = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(healthMemory));
//             
//             var anatomyMemory = snakeMemory.Slice(healthSize, anatomySize);
//             ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(anatomyMemory));
//             
//             var bodyMemory = snakeMemory[headerSize..];
//             var body = MemoryMarshal.Cast<byte, ushort>(bodyMemory);
//
//             // Creiamo e ritorniamo la vista WarSnake.
//             return new WarSnake(ref health, ref anatomy, body);
//         }
//     }
// }