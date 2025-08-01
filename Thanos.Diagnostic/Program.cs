using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Thanos.Parsers;

namespace Thanos.Diagnostic;

public class PerformanceDiagnostic
{
    public static unsafe void Main(string[] args)
    {
        // Inizializza una volta
        GameEngine.Initialize();
        var state = GameEngine.State;
        
        // Test JSON
        var smallJson = """
        {
            "turn": 10,
            "board": {
                "height": 11,
                "width": 11,
                "food": [{"x": 5, "y": 5}],
                "snakes": [{
                    "id": "a",
                    "health": 90,
                    "body": [{"x": 1, "y": 1}],
                    "head": {"x": 1, "y": 1}
                }]
            },
            "you": {"id": "a"}
        }
        """u8.ToArray();

        // Carica test.json se disponibile
        var testJson = smallJson;
        var testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.json");
        if (File.Exists(testPath))
        {
            testJson = File.ReadAllBytes(testPath);
            Console.WriteLine($"Loaded test.json: {testJson.Length} bytes");
            
            // Analizza struttura
            AnalyzeJson(testJson);
        }

        // Warmup
        Console.WriteLine("\nWarming up JIT...");
        for (var i = 0; i < 10000; i++)
        {
            BattlesnakeParser.ParseDirect(smallJson, state);
            UltraFastParser.Parse(smallJson, state);
        }

        // Benchmark comparativo
        Console.WriteLine("\n=== BENCHMARK RESULTS ===");
        
        // Test piccolo
        BenchmarkParser("Small JSON", smallJson, state, 100000);
        
        // Test reale
        BenchmarkParser("Test JSON", testJson, state, 10000);
        
        // Profiling dettagliato
        Console.WriteLine("\n=== DETAILED PROFILING ===");
        ProfileParser(testJson, state);
        
        // Verifica correttezza
        Console.WriteLine("\n=== CORRECTNESS CHECK ===");
        VerifyParser(testJson, state);
    }

    private static unsafe void BenchmarkParser(string name, byte[] json, GameState* state, int iterations)
    {
        Console.WriteLine($"\n{name} ({json.Length} bytes):");
        
        // ParseDirect
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            BattlesnakeParser.ParseDirect(json, state);
        }
        sw.Stop();
        var parseDirectTime = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"  ParseDirect:    {parseDirectTime:F3} μs");
        
        // UltraFastParser
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            UltraFastParser.Parse(json, state);
        }
        sw.Stop();
        var ultraFastTime = sw.Elapsed.TotalMicroseconds / iterations;
        Console.WriteLine($"  UltraFast:      {ultraFastTime:F3} μs");
        
        // Speedup
        Console.WriteLine($"  Speedup:        {parseDirectTime / ultraFastTime:F2}x");
    }

    private static unsafe void ProfileParser(byte[] json, GameState* state)
    {
        const int iterations = 1000;
        
        // Misura sezioni specifiche
        var sw = new Stopwatch();
        
        // Test parsing numeri
        sw.Restart();
        for (var i = 0; i < iterations * 100; i++)
        {
            TestParseNumber();
        }
        sw.Stop();
        Console.WriteLine($"Parse number: {sw.Elapsed.TotalNanoseconds / (iterations * 100):F1} ns");
        
        // Test skip whitespace
        sw.Restart();
        for (var i = 0; i < iterations * 100; i++)
        {
            TestSkipWhitespace();
        }
        sw.Stop();
        Console.WriteLine($"Skip whitespace: {sw.Elapsed.TotalNanoseconds / (iterations * 100):F1} ns");
        
        // Test find quote
        sw.Restart();
        for (var i = 0; i < iterations * 100; i++)
        {
            TestFindQuote();
        }
        sw.Stop();
        Console.WriteLine($"Find quote: {sw.Elapsed.TotalNanoseconds / (iterations * 100):F1} ns");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestParseNumber()
    {
        var data = "12345"u8;
        var pos = 0;
        var result = 0;
        while (pos < data.Length && data[pos] >= '0' && data[pos] <= '9')
        {
            result = result * 10 + (data[pos] - '0');
            pos++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestSkipWhitespace()
    {
        var data = "    \t\n\r123"u8;
        var pos = 0;
        while (pos < data.Length && data[pos] <= 32) pos++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestFindQuote()
    {
        var data = "abcdefghijklmnopqrstuvwxyz\""u8;
        var pos = 0;
        while (pos < data.Length && data[pos] != '"') pos++;
    }

    private static void AnalyzeJson(byte[] json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            Console.WriteLine("\nJSON Structure:");
            Console.WriteLine($"- Turn: {root.GetProperty("turn").GetInt32()}");
            
            var board = root.GetProperty("board");
            Console.WriteLine($"- Board: {board.GetProperty("width").GetInt32()}x{board.GetProperty("height").GetInt32()}");
            Console.WriteLine($"- Snakes: {board.GetProperty("snakes").GetArrayLength()}");
            
            var totalBodySegments = 0;
            foreach (var snake in board.GetProperty("snakes").EnumerateArray())
            {
                var bodyLen = snake.GetProperty("body").GetArrayLength();
                totalBodySegments += bodyLen;
                Console.WriteLine($"  - Snake '{snake.GetProperty("id").GetString()}': {bodyLen} segments");
            }
            
            Console.WriteLine($"- Food: {board.GetProperty("food").GetArrayLength()}");
            
            if (board.TryGetProperty("hazards", out var hazards))
            {
                Console.WriteLine($"- Hazards: {hazards.GetArrayLength()}");
            }
            
            Console.WriteLine($"- Total body segments: {totalBodySegments}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to analyze JSON: {ex.Message}");
        }
    }

    private static unsafe void VerifyParser(byte[] json, GameState* state)
    {
        // Parse con entrambi i metodi
        BattlesnakeParser.ParseDirect(json, state);
        var turn1 = state->Turn;
        var snakes1 = state->SnakeCount;
        var food1 = state->FoodCount;
        
        UltraFastParser.Parse(json, state);
        var turn2 = state->Turn;
        var snakes2 = state->SnakeCount;
        var food2 = state->FoodCount;
        
        Console.WriteLine($"ParseDirect - Turn: {turn1}, Snakes: {snakes1}, Food: {food1}");
        Console.WriteLine($"UltraFast   - Turn: {turn2}, Snakes: {snakes2}, Food: {food2}");
        
        if (turn1 == turn2 && snakes1 == snakes2 && food1 == food2)
        {
            Console.WriteLine("✓ Parsers produce same results");
        }
        else
        {
            Console.WriteLine("✗ Parsers produce different results!");
        }
    }
}