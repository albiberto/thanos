using Thanos.SourceGen;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Integration.SnakeSystem;

public partial class SnakesSystemTests
{
    [TestCaseSource(nameof(SystemScenarios))]
    public void Me_WhenAccessed_ShouldAlwaysReturnFirstSnake(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Initialize Snake 0 uniquely
            system[0].Initialize(new Snake("me", 99, [10]));

            // Act
            var me = system.Me;

            // Assert
            That(me.HP, Is.EqualTo(99));
            That(me.Head, Is.EqualTo(10));

            // Verify structural identity (Index 0)
            // If we modify 'Me', system[0] should reflect it
            me.Kill();
            That(system[0].IsDead, Is.True, "Me property is not pointing to Snake[0] reference.");
        }
    }

    [TestCaseSource(nameof(SystemScenarios))]
    public void Indexer_WhenAccessed_ShouldReturnCorrectInstance(SnakesSystemTestContext ctx)
    {
        using (ctx)
        {
            // Arrange
            var system = ctx.Build();

            // Initialize distinct states for every active snake
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                // Assign unique HP/Head based on index
                var hp = (byte)(10 + i);
                var head = (ushort)(i * 2);
                system[i].Initialize(new Snake($"s{i}", hp, [head]));
            }

            // Act & Assert
            for (var i = 0; i < ctx.ActiveCount; i++)
            {
                var snake = system[i];
                var expectedHp = 10 + i;
                var expectedHead = i * 2;

                That(snake.HP, Is.EqualTo(expectedHp), $"Snake {i} HP mismatch.");
                That(snake.Head, Is.EqualTo(expectedHead), $"Snake {i} Head mismatch.");
            }
        }
    }
}