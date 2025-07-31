using System.Text;
using System.Text.Json;
using Thanos.Tests.Support.Requests;

namespace Thanos.Tests.Support;

public class TestsProvider
{
    private const string TestDataDirectory = "TestData";
    private const string TestDataFileName = "battlesnake_test_cases";
    
    private readonly Dictionary<string, TestModel> _testCases = LoadTestCases();
    
    public IEnumerable<string> Names => _testCases.Keys;
    
    public TestModel this[string name]
    {
        get
        {
            var result = _testCases.TryGetValue(name, out var test);
            if (!result || test is null) throw new KeyNotFoundException($"Test case '{name}' not found.");

            return test;
        }
    }

    private static Dictionary<string, TestModel> LoadTestCases()
    {
        var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestDataDirectory, $"{TestDataFileName}.json");
        
        if (!File.Exists(testDataPath)) throw new FileNotFoundException($"Test data file not found: {testDataPath}");

        var content = File.ReadAllText(testDataPath);
        using var document = JsonDocument.Parse(content);
        
        var testCases = new Dictionary<string, TestModel>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var name = property.Name;
            var section = property.Value.GetRawText();

            var jsonBytes = BuildContent(section);
            var gameState = Deserialize(section);

            testCases[name] = new TestModel(jsonBytes, gameState);
        }

        return testCases;
    }

    private static byte[] BuildContent(string section) => Encoding.UTF8.GetBytes(section);

    private static MoveRequest Deserialize(string section)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Deserialize<MoveRequest>(section, options) ?? throw new JsonException("Failed to deserialize GameState from section.");
    }
}