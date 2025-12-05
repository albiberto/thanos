using Thanos.MCST;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class NodeMemoryLayoutTests
{
    /// <summary>
    ///     Verifies that NodeMemoryLayout.Default.Size matches the exact sizeof(Node) struct,
    ///     ensuring the layout correctly represents the actual node size in memory.
    /// </summary>
    [Test]
    public void Default_Should_Match_Exact_SizeOf_Node_Struct()
    {
        var layout = NodeMemoryLayout.Packed;
        
        unsafe
        {
            That(layout.Node.Length, Is.EqualTo(sizeof(Node)), "Default layout size should match sizeof(Node).");
            That(layout.Node.Offset, Is.EqualTo(0), "Default layout size should match sizeof(Node).");
        }
    }
    
    /// <summary>
    ///     Verifies that NodeMemoryLayout.Default.Size is positive,
    ///     ensuring the default layout is valid and usable.
    /// </summary>
    [Test]
    public void Default_Size_Should_Be_Positive()
    {
        That(NodeMemoryLayout.Packed.Node.Length, Is.GreaterThan(0), "Default layout size should be positive.");
    }
}