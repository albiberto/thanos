using System.Text.Json;
using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.SourceGen;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CoordinateArrayToUshortArrayConverterTests
{
    private const int StandardWidth = 11;
    private JsonSerializerOptions _standardOptions;

    [SetUp]
    public void Setup()
    {
        _standardOptions = new JsonSerializerOptions
        {
            Converters = { new CoordinateArrayToUshortArrayConverter(StandardWidth) }
        };
    }

    [Test]
    public void Read_WhenJsonIsStandardArray_ThenReturnsCorrectIndicesPreservingOrder()
    {
        // (0,0)->0, (1,0)->1, (0,1)->11
        const string json = """[ { "x": 0, "y": 0 }, { "x": 1, "y": 0 }, { "x": 0, "y": 1 } ]""";
        ushort[] expected = [0, 1, 11];

        var result = JsonSerializer.Deserialize<ushort[]>(json, _standardOptions);

        That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Read_WhenJsonIsEmptyArray_ThenReturnsEmptyArray()
    {
        const string json = "[]";
        var result = JsonSerializer.Deserialize<ushort[]>(json, _standardOptions);
        
        That(result, Is.Not.Null);
        That(result, Is.Empty);
    }

    [Test]
    public void Read_WhenJsonHasWhitespaceAndFormatting_ThenFastScanCountsCorrectly()
    {
        const string json = """
                            [
                              { "x": 5, "y": 5 },
                                 { "y": 0, "x": 0 }
                            ]
                            """;
        ushort[] expected = [60, 0];

        var result = JsonSerializer.Deserialize<ushort[]>(json, _standardOptions);

        That(result, Is.EqualTo(expected));
    }
    
    [Test]
    public void Read_WhenArrayContainsComplexObjects_ThenExtractsCoordinatesOnly()
    {
        // Deve saltare le proprietà extra (id, health, ecc) e trovare solo x,y
        const string json = """
                            [
                                { "id": "snake1", "x": 1, "y": 0, "health": 100 },
                                { "x": 2, "y": 0, "object": { "nested": true } }
                            ]
                            """;
        ushort[] expected = [1, 2];

        var result = JsonSerializer.Deserialize<ushort[]>(json, _standardOptions);

        That(result, Is.EqualTo(expected));
    }
}