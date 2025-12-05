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
        var layout = NodeMemoryLayout.Default;
        
        unsafe
        {
            That(layout.Size, Is.EqualTo(sizeof(Node)), "Default layout size should match sizeof(Node).");
        }
    }

    /// <summary>
    ///     Verifies that NodeMemoryLayout constructor correctly sets a custom size,
    ///     allowing for padded layouts with custom stride values.
    /// </summary>
    [Test]
    public void Constructor_Should_Set_CustomSize()
    {
        const int customSize = 64;
        var layout = new NodeMemoryLayout(customSize);

        That(layout.Size, Is.EqualTo(customSize), "Layout size should match the custom size provided.");
    }

    /// <summary>
    ///     Verifies that NodeMemoryLayout.Default.Size is positive,
    ///     ensuring the default layout is valid and usable.
    /// </summary>
    [Test]
    public void Default_Size_Should_Be_Positive()
    {
        That(NodeMemoryLayout.Default.Size, Is.GreaterThan(0), "Default layout size should be positive.");
    }
}