using System.Text.Json;

namespace Thanos.Tests.Support;

public class TestsProvider(string filename, string directory = "JsonTests")
{
    private readonly Dictionary<string, string> _testCases = LoadTestCases(directory, filename);

    public IEnumerable<string> Names => _testCases.Keys;
    
    public string this[string name]
    {
        get
        {
            var result = _testCases.TryGetValue(name, out var test);
            if (!result || test is null) throw new KeyNotFoundException($"Test case '{name}' not found.");

            return test;
        }
    }

    private static Dictionary<string, string> LoadTestCases(string directory, string filename)
    {
        var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, directory, $"{filename}.json");
        
        if (!File.Exists(testDataPath)) throw new FileNotFoundException($"Test data file not found: {testDataPath}");

        var content = File.ReadAllText(testDataPath);
        using var document = JsonDocument.Parse(content);
        
        var testCases = new Dictionary<string, string>();

        foreach (var property in document.RootElement.EnumerateObject()) testCases[property.Name] = property.Value.GetRawText();

        return testCases;
    }
}