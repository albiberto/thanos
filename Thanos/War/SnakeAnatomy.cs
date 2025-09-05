using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public ref struct SnakeAnatomy(ushort initialLength)
{
    private bool _isPendingGrowth = false;

    public ushort Length { get; private set; } = initialLength;

    public void ScheduleGrowth() => _isPendingGrowth = true;
    
    public void ProcessPendingGrowth()
    {
        if (!_isPendingGrowth) return;
        
        Length++;
        _isPendingGrowth = false;
    }
}