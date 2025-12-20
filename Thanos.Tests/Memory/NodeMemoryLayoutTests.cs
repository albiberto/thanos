using Thanos.MCST;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
public class NodeMemoryLayoutTests
{
    /// <summary>
    ///     Verifies that NodeMemoryLayout correctly calculates node size in memory.
    /// </summary>
    [Test]
    public unsafe void Layout_Should_Match_Exact_SizeOf_Node_Struct()
    {
        var layout = new NodeMemoryLayout();

        var expectedNodeLength = (long)sizeof(Node);
        const long expectedNodeOffset = 0;
        
        var actualNodeLength = (long)layout.Node.Length;
        var actualNodeOffset = (long)layout.Node.Offset;

        Multiple(() =>
        {
            That(actualNodeLength, Is.EqualTo(expectedNodeLength),
                $"Node.Length should be {expectedNodeLength} but was {actualNodeLength}.");
            That(actualNodeOffset, Is.EqualTo(expectedNodeOffset),
                $"Node.Offset should be {expectedNodeOffset} but was {actualNodeOffset}.");
        });
    }

    /// <summary>
    ///     Verifies that NodeMemoryLayout.Node.Length is positive,
    ///     ensuring the layout is valid and usable.
    /// </summary>
    [Test]
    public void Layout_Size_Should_Be_Positive()
    {
        var layout = new NodeMemoryLayout();
        var actualNodeLength = (long)layout.Node.Length;

        That(actualNodeLength, Is.GreaterThan(0),
            $"Node.Length should be greater than 0 but was {actualNodeLength}.");
    }
}