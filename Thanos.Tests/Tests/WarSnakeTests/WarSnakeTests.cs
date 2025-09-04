using Thanos.Memory;
using Thanos.Memory.Pools;
using Thanos.PreWarm.Memory;
using Thanos.SourceGen;

namespace Thanos.Tests.Tests.WarSnakeTests;

[TestFixture]
public class WarSnakeMoveTests
{
    [Test]
    public void Test()
    {
        const int width = 11;
        var map = BuildIdMap();
        var context = new GameContext(width, map);

        var lutProvider = new LutProvider(Constants.MaxWidth, Constants.MaxArea);
        var warPool = new SlotMemoryPool(context, 1000);
        var slot = warPool.GetNext();
        slot.InitializeFromRequest(CreateRequest_SingleSnake());
        var me = slot.General.Snakes.Me;
        var lenght = me.Length;
        me.Move(To1D(new Coordinate(5, 6), width), false, 1);

        Assert.That(lenght, Is.EqualTo(me.Length));
        lenght = me.Length + 1;

        me.Move(To1D(new Coordinate(6, 6), width), true, 1);
        Assert.That(lenght, Is.EqualTo(me.Length));
    }

    private static Dictionary<string, int> BuildIdMap() =>
        new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["me"] = 0
        };

    private static ushort To1D(in Coordinate coord, int width) => (ushort)(coord.Y * width + coord.X);

    private static Request CreateRequest_SingleSnake()
    {
        // 1. Definiamo il nostro serpente ("you")
        var you = new Snake(
            "me",
            "Thanos-Test",
            100,
            [
                new Coordinate(5, 5), // Head
                new Coordinate(5, 4),
                new Coordinate(5, 3),
                new Coordinate(5, 2),
                new Coordinate(5, 1) // Tail
            ],
            "123",
            new Coordinate(5, 5),
            5,
            "I am inevitable."
        );

        // 2. Definiamo il tabellone di gioco
        var board = new Board(
            11,
            11,
            [
                new Coordinate(1, 1),
                new Coordinate(9, 9)
            ],
            [], // Nessun pericolo per un test semplice
            [you] // Il tabellone contiene solo il nostro serpente
        );

        // 3. Definiamo le regole e i metadati della partita
        var game = new Game(
            Guid.NewGuid(),
            new Ruleset(
                new RulesetSettings(
                    15,
                    1,
                    0,
                    null, // No Royale settings
                    null // No Squad settings
                )
            ),
            "standard",
            "custom",
            500
        );

        // 4. Assembliamo l'oggetto Request finale
        var request = new Request(
            game,
            3, // Un numero di turno realistico
            board,
            you
        );

        return request;
    }
}