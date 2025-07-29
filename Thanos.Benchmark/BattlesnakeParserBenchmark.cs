using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Thanos.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[Config(typeof(Config))]
public unsafe class BattlesnakeParserBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    private byte[] _smallJson = null!;
    private byte[] _mediumJson = null!;
    private byte[] _testJson = null!;
    private GameState* _state;

    [GlobalSetup]
    public void Setup()
    {
        // Inizializza UNA SOLA VOLTA
        GameManager.Initialize();
        _state = GameManager.State;
        
        // JSON piccolo per test veloce
        _smallJson = """
        {
            "turn": 10,
            "board": {
                "height": 11,
                "width": 11,
                "food": [{"x": 5, "y": 5}],
                "hazards": [],
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

        // JSON medio più realistico
        _mediumJson = """
        {
            "turn": 50,
            "board": {
                "height": 11,
                "width": 11,
                "food": [{"x": 5, "y": 5}, {"x": 7, "y": 2}, {"x": 1, "y": 9}],
                "hazards": [],
                "snakes": [
                    {
                        "id": "snake1",
                        "health": 85,
                        "body": [{"x": 5, "y": 5}, {"x": 5, "y": 4}, {"x": 5, "y": 3}, {"x": 4, "y": 3}],
                        "head": {"x": 5, "y": 5}
                    },
                    {
                        "id": "snake2", 
                        "health": 70,
                        "body": [{"x": 8, "y": 8}, {"x": 8, "y": 7}, {"x": 7, "y": 7}],
                        "head": {"x": 8, "y": 8}
                    }
                ]
            },
            "you": {"id": "snake1"}
        }
        """u8.ToArray();
        
        // Carica test.json se esiste
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.json");
        if (File.Exists(path))
        {
            _testJson = File.ReadAllBytes(path);
            System.Console.WriteLine($"Loaded test.json: {_testJson.Length} bytes");
        }
        else
        {
            _testJson = _mediumJson;
        }
    }

    [Benchmark]
    public void ParseDirect_Small()
    {
        BattlesnakeParser.ParseDirect(_smallJson, _state);
    }

    [Benchmark]
    public void ParseDirect_Medium()
    {
        BattlesnakeParser.ParseDirect(_mediumJson, _state);
    }
    
    [Benchmark]
    public void ParseDirect_TestFile()
    {
        BattlesnakeParser.ParseDirect(_testJson, _state);
    }
    
    [Benchmark]
    public void UltraFastParseDirect_Small()
    {
        UltraFastParser.Parse(_smallJson, _state);
    }

    [Benchmark]
    public void UltraFastParseDirect_Medium()
    {
        UltraFastParser.Parse(_mediumJson, _state);
    }
    
    [Benchmark]
    public void UltraFastParseDirect_TestFile()
    {
        UltraFastParser.Parse(_testJson, _state);
    }
    
    
}