using System.Runtime.CompilerServices;
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
}