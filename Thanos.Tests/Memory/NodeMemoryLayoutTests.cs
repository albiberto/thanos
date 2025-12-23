using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.Memory;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Memory;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class NodeMemoryLayoutTests
{
    [Test]
    public void Constructor_WhenInitialized_ThenNodeSizeMatchesStructSize()
    {
        // Arrange
        var layout = new NodeMemoryLayout();

        // Node è una struct con LayoutKind.Explicit e Size = 64
        var expectedNodeSize = Unsafe.SizeOf<Node>(); 
        
        // Act & Assert
        Multiple(() =>
        {
            That((long)layout.Node.Length, Is.EqualTo(expectedNodeSize), 
                "MemoryBlock length must match sizeof(Node).");
            
            That(expectedNodeSize, Is.EqualTo(64), 
                "Node struct size must be exactly 64 bytes (Cache Line). Check StructLayout.");
        });
    }

    [Test]
    public void Constructor_WhenInitialized_ThenStrideIsCacheLineAligned()
    {
        // Arrange
        var layout = new NodeMemoryLayout();

        // Act
        var stride = (long)layout.Node.Next;

        // Assert
        // Questo è cruciale per evitare False Sharing tra thread se i nodi fossero processati in parallelo,
        // e per garantire che l'indirizzo base di ogni nodo sia allineato a 64 byte.
        That(stride % Constants.CacheLine, Is.Zero, 
            "Node stride must be multiple of 64 bytes (Cache Line).");
    }
}