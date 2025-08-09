using GameTest = Thanos.Tests.Support.Model.Game;

namespace Thanos.Tests.Support;

public class TestModel(string raw, string nested, GameTest game)
{
    public string Raw { get; } = raw;
    public string NextedRaw => nested;
    public GameTest Game { get; } = game;
}