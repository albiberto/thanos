using Thanos;
using Thanos.Memory;
using Thanos.War.Rules;
using Thanos.War.State;

const int turns = 1;

GameContext context = new(LookupsMemoryPool.Medium.NeighborsMatrix, 0, 0, 20);
SlotMemoryPool pool = new(10, 0, Constants.MaxSnakesCount, LookupsMemoryPool.Medium, new(Constants.Medium.Area, 64, Constants.MaxSnakesCount));

var moves = new byte[][]
{
    // up
    [1, 1, 1, 1],
    // right
    [3, 3, 3, 3],
    // down
    [0, 0, 0, 0],
    // left
    [2, 2, 2, 2]
};

var game = pool.GetGameState(0);
StateMapper.Initialize(ref game, [
    5 + 2 * 11,
    5 + 8 * 11,
    2 + 5 * 11,
    8 + 5 * 11
]);

for (var i = 0; i < turns; i++)
{
    StandardRules.SimulateTurn(ref game, context, moves[i % moves.Length]);
    // EnvironmentRules.SimulateFoodSpawn(ref game, context);
}

pool.Reset();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();