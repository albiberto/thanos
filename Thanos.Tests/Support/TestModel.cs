using GameTest = Thanos.Tests.Support.Model.Game;

namespace Thanos.Tests.Support;

public class TestModel(string raw, GameTest game)
{
    public string Raw { get; } = raw;
    public GameTest Game { get; } = game;
}