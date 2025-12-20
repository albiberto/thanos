using Thanos.MCST;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class NodeMemoryLayoutTests
{
    [Test]
    public unsafe void Layout_Should_Match_Exact_SizeOf_Node_Struct()
    {
        var layout = new NodeMemoryLayout();

        // Node è layout explicit size 64
        var expectedNodeLength = 64L; 
        
        var actualNodeLength = (long)layout.Node.Length;

        That(actualNodeLength, Is.EqualTo(expectedNodeLength),
            $"Node.Length should match struct size/stride.");
    }

    [Test]
    public void Layout_Stride_Should_Be_CacheLine_Aligned()
    {
        var layout = new NodeMemoryLayout();
        var stride = (long)layout.Node.Next;

        That(stride % 64, Is.Zero, "Node stride must be multiple of 64 bytes (Cache Line).");
    }
}