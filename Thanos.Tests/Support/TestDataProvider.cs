using System.Text;
using System.Text.Json;

namespace Thanos.Tests.Support;

/// <summary>
/// Provider per i dati di test JSON con accesso tramite indexer
/// </summary>
public class TestDataProvider
{
    private readonly Dictionary<string, string> _testCases = LoadTestCases();

    /// <summary>
    /// Accesso ai test cases tramite indexer
    /// </summary>
    /// <param name="name">Nome del test case</param>
    /// <returns>Tupla con bytes del JSON, width e height del board</returns>
    /// <exception cref="ArgumentException">Se il test case non esiste</exception>
    public (byte[] bytes, byte width, byte height) this[string name]
    {
        get
        {
            if (!_testCases.TryGetValue(name, out var json)) throw new ArgumentException($"Test case '{name}' not found");
            
            var bytes = Encoding.UTF8.GetBytes(json);
            var (width, height) = ExtractBoardDimensions(json);
            
            return (bytes, width, height);
        }
    }
    
    private static (byte width, byte height) ExtractBoardDimensions(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("board", out var board)) return (11, 11);
        var width = board.GetProperty("width").GetByte();
        var height = board.GetProperty("height").GetByte();
        
        return (width, height);
    }

    private static Dictionary<string, string> LoadTestCases()
    {
        var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "battlesnake_test_cases.json");
        
        if (!File.Exists(testDataPath)) throw new FileNotFoundException($"Test data file not found: {testDataPath}");

        var jsonContent = File.ReadAllText(testDataPath);
        var testData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent) ?? throw new InvalidOperationException("Failed to deserialize test data");
        
        var result = new Dictionary<string, string>();
        foreach (var kvp in testData) result[kvp.Key] = kvp.Value.GetRawText();
        
        return result;
    }
}