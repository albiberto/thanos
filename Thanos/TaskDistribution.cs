using System.Runtime.CompilerServices;

namespace Thanos;

public readonly struct TaskDistribution
{
    public readonly int TasksPerMove;
    public readonly double TimePerTask;
    public readonly int TotalTasks;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskDistribution(int movesCount, int cores, double totalTimeMs)
    {
        TasksPerMove = Math.Max(1, cores / movesCount);
        var timePerMove = totalTimeMs / movesCount;
        TimePerTask = timePerMove / TasksPerMove;
        TotalTasks = movesCount * TasksPerMove;
    }
}