using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Thanos.Benchmark;

// [MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 3)]
// [Config(typeof(Config))] 
public unsafe partial class BattleSnakeBenchmark
{
    private const int OperationsPerInvoke = ushort.MaxValue;
    private const int Capacity = 256;
    
    private byte* _memory;
    private BattleSnake* _snake;

    // This parameter controls whether the snake has eaten or not
    [Params(true, false)]
    public bool hasEaten;

    [GlobalSetup]
    public void GlobalSetup()
    {
        const int snakeStride = BattleSnake.HeaderSize + Capacity * sizeof(ushort);
        _memory = (byte*)NativeMemory.AlignedAlloc((nuint)snakeStride, 64);
        _snake = (BattleSnake*)_memory;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        NativeMemory.AlignedFree(_memory);
        _memory = null;
        _snake = null;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // 1. Definisci lo stato di partenza completo per il serpente
        const int startHealth = 100;
        const int startLength = 1;
        var startBody = new ushort[] { 1 }; // Il corpo contiene solo la testa

        // 2. Chiama il nuovo metodo con tutti i parametri richiesti
        BattleSnake.PlacementNew(
            snake: _snake, 
            health: startHealth, 
            length: startLength, 
            capacity: Capacity, 
            sourceBody: startBody // L'array viene convertito implicitamente in ReadOnlySpan<ushort>
        );
    }

    [Benchmark(Description = "BattleSnake.Move", OperationsPerInvoke = OperationsPerInvoke)]
    public void RunBattleSnakeMove()
    {
        for (var i = 0; i < OperationsPerInvoke; i++) _snake->Move((ushort)(2 + i), hasEaten, damage: 0);
    }
}