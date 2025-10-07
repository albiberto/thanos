using System.Runtime.CompilerServices;
using System.Text;
using Thanos.Memory;

namespace Thanos.Extensions;

public static class NodePoolExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SelectMostVisitedChild(this NodeMemoryPool nodePool, int rootIndex)
    {
        ref var parentNode = ref nodePool[rootIndex];
        
        if (parentNode.IsLeafNode) return -1;

        var bestChildIndex = -1;
        var maxVisits = -1;

        var currentChildIndex = parentNode.FirstChildIndex;
        while (currentChildIndex != -1)
        {
            ref var childNode = ref nodePool[currentChildIndex];
            
            if (childNode.Visits > maxVisits)
            {
                maxVisits = childNode.Visits;
                bestChildIndex = currentChildIndex;
            }
            
            currentChildIndex = childNode.NextSiblingIndex;
        }

        return bestChildIndex;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SelectMostVisitedChildWithLogging(this NodeMemoryPool nodePool, int rootIndex)
    {
        ref var parentNode = ref nodePool[rootIndex];

        if (parentNode.IsLeafNode)
        {
            Console.WriteLine("[Engine.Select] Node has no children to select from.");
            return -1;
        }

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[Engine.Select] Analyzing children of Node {rootIndex} (Total Visits: {parentNode.Visits}):");

        var bestChildIndex = -1;
        var maxVisits = -1;

        var currentChildIndex = parentNode.FirstChildIndex;
        while (currentChildIndex != -1)
        {
            ref var childNode = ref nodePool[currentChildIndex];
            var winRate = childNode.Visits > 0 ? (childNode.Wins / childNode.Visits) : 0;

            // Add a line for each child
            logBuilder.AppendLine($"  -> Child {currentChildIndex}: Move: {childNode.Move.ToApiMove(),-5} | Visits: {childNode.Visits,-7} | Win Rate: {winRate:P2}");

            if (childNode.Visits > maxVisits)
            {
                maxVisits = childNode.Visits;
                bestChildIndex = currentChildIndex;
            }
        
            currentChildIndex = childNode.NextSiblingIndex;
        }

        if (bestChildIndex != -1)
        {
            ref var bestNode = ref nodePool[bestChildIndex];
            logBuilder.AppendLine($"[Engine.Select] Best Move Selected: {bestNode.Move.ToApiMove()} with {bestNode.Visits} visits.");
        }
        else
        {
            logBuilder.AppendLine("[Engine.Select] CRITICAL: No valid child found to select.");
        }

        Console.WriteLine(logBuilder.ToString());
        return bestChildIndex;
    }
}