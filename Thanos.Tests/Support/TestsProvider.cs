using System.Text.Json;
using GameTest = Thanos.Tests.Support.Model.Game;

namespace Thanos.Tests.Support;

public class TestsProvider(string field, string filename, string directory = "JsonTests")
{
    private readonly Dictionary<string, TestModel> _testCases = LoadTestCases(directory, filename, field);

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

    private static Dictionary<string, TestModel> LoadTestCases(string directory, string filename, string field)
    {
        var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, directory, $"{filename}.json");
        
        if (!File.Exists(testDataPath)) throw new FileNotFoundException($"Test data file not found: {testDataPath}");

        var content = File.ReadAllText(testDataPath);
        using var document = JsonDocument.Parse(content);
        
        var testCases = new Dictionary<string, TestModel>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var name = property.Name;
            
            var raw = property.Value.GetRawText();
            var nestedRaw = property.Value.GetProperty(field).GetRawText();
            var gameState = Deserialize(nestedRaw);

            testCases[name] = new TestModel(raw, gameState);
        }

        return testCases;
    }

    private static GameTest Deserialize(string section)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Deserialize<GameTest>(section, options) ?? throw new JsonException("Failed to deserialize game state from test case.");
    }
}