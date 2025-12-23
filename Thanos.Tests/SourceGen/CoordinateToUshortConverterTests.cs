using System.Text.Json;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.SourceGen;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CoordinateToUshortConverterTests
{
    private const int StandardWidth = 11;
    private JsonSerializerOptions _standardOptions;

    [SetUp]
    public void Setup()
    {
        _standardOptions = new JsonSerializerOptions
        {
            Converters = { new CoordinateToUshortConverter(StandardWidth) }
        };
    }

    [Test]
    public void Read_WhenJsonIsValidCoordinate_ThenReturnsCorrectFlattenedIndex()
    {
        // (x=5, y=5, w=11) -> 5 * 11 + 5 = 60
        const string json = """{ "x": 5, "y": 5 }""";
        const ushort expectedIndex = 60;

        var result = JsonSerializer.Deserialize<ushort>(json, _standardOptions);

        That(result, Is.EqualTo(expectedIndex));
    }

    [Test]
    public void Read_WhenPropertiesAreUnordered_ThenReturnsCorrectIndex()
    {
        // (x=10, y=0, w=11) -> 0 * 11 + 10 = 10
        const string json = """{ "y": 0, "x": 10 }"""; 
        const ushort expectedIndex = 10;

        var result = JsonSerializer.Deserialize<ushort>(json, _standardOptions);

        That(result, Is.EqualTo(expectedIndex));
    }

    [Test]
    public void Read_WhenJsonContainsExtraProperties_ThenIgnoresThem()
    {
        // (x=0, y=1, w=11) -> 1 * 11 + 0 = 11
        const string json = """{ "x": 0, "extra": "trash", "y": 1 }""";
        const ushort expectedIndex = 11;

        var result = JsonSerializer.Deserialize<ushort>(json, _standardOptions);

        That(result, Is.EqualTo(expectedIndex));
    }

    [Test]
    public void Read_WhenGridWidthIsDifferent_ThenCalculatesIndexUsingProvidedWidth()
    {
        // Testiamo larghezza 7 (Small Map)
        var smallMapOptions = new JsonSerializerOptions
        {
            Converters = { new CoordinateToUshortConverter(7) }
        };
        
        // (x=2, y=2, w=7) -> 2 * 7 + 2 = 16 (Se usasse 11 sarebbe 24)
        const string json = """{ "x": 2, "y": 2 }""";
        const ushort expectedIndex = 16;

        var result = JsonSerializer.Deserialize<ushort>(json, smallMapOptions);

        That(result, Is.EqualTo(expectedIndex));
    }
}