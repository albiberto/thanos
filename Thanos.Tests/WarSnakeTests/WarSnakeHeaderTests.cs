using Thanos.War;

namespace Thanos.Tests.WarSnakeTests;

public partial class SnakeTests
{
    [Test]
    public void Header()
    {
        var sut = new WarSnakeHeader(
        {
            Index = 1,
            Health = 100,
            Capacity = 10,
            Length = 5,
            Head = 2,
            NextHeadIndex = 3,
            TailIndex = 0
        };
    }
}