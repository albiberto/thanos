// using System.Runtime.InteropServices;
//
// namespace Thanos.War.Arena.Memory;
//
// public readonly ref struct WarArenaMemoryView(Span<byte> memory, in WarArenaMemoryLayout layout)
// {
//     private readonly Span<byte> _memory = memory;
//     private readonly WarArenaMemoryLayout _layout = layout;
//     
//     public ref WarArenaHeader Header => 
//         ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(_memory[.._layout.Header]));
// }