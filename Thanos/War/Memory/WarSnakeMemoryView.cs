using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.War.Snake;

namespace Thanos.War.Memory.Views;

public readonly ref struct WarSnakeMemoryView(Span<byte> hotMemory, in WarSnakeHeaderLayout headerLayout)
{
    public readonly Health Health = Unsafe.As<byte, Health>(ref MemoryMarshal.GetReference(hotMemory));
    public readonly Anatomy Anatomy = Unsafe.As<byte, Anatomy>(ref MemoryMarshal.GetReference(hotMemory[headerLayout.AnatomyOffset..]));
}