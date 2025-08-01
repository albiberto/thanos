using Thanos.BitMasks;

namespace Thanos.Tests
{
    [TestFixture]
    public class BattleSnakeTests
    {
        [Test]
        public void Reset_ImpostaValoriIniziali()
        {
            BattleSnake snake = new BattleSnake();
            snake.Reset();

            Assert.That(snake.Health, Is.EqualTo(100));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)0));
        }

        // [Test]
        // public void Move_MangiaCibo_AumentaLunghezzaERipristinaSalute()
        // {
        //     BattleSnake snake = new BattleSnake();
        //     snake.Reset();
        //
        //     int oldLength = snake.Length;
        //     ushort oldHead = snake.Head;
        //
        //     bool alive = snake.Move(newHeadPosition: 1, content: CellContent.Food);
        //
        //     Assert.That(alive, Is.True);
        //     Assert.That(snake.Health, Is.EqualTo(100));
        //     Assert.That(snake.Length, Is.EqualTo(oldLength + 1));
        //     Assert.That(snake.Head, Is.EqualTo((ushort)1));
        //
        //     fixed (ushort* bodyPtr = snake.Body)
        //     {
        //         Assert.That(bodyPtr[oldLength], Is.EqualTo(oldHead));
        //     }
        // }

        [Test]
        public void Move_EntraInHazard_DiminuisceSaluteDiHazardDamage()
        {
            BattleSnake snake = new BattleSnake();
            snake.Reset();

            int initialHealth = snake.Health;

            bool alive = snake.Move(newHeadPosition: 2, content: CellContent.Hazard, hazardDamage: 15);

            Assert.That(alive, Is.True);
            Assert.That(snake.Health, Is.EqualTo(initialHealth - 15));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)2));
        }

        [Test]
        public void Move_EntraInHazard_MuoreSeSaluteScendeSottoZero()
        {
            BattleSnake snake = new BattleSnake();
            snake.Reset();

            snake.Health = 10;

            bool alive = snake.Move(newHeadPosition: 3, content: CellContent.Hazard, hazardDamage: 15);

            Assert.That(alive, Is.False);
            Assert.That(snake.Health, Is.LessThanOrEqualTo(0));
            Assert.That(snake.Head, Is.EqualTo((ushort)3));
        }

        [Test]
        public void Move_MovimentoNormale_DiminuisceSaluteDiUno()
        {
            BattleSnake snake = new BattleSnake();
            snake.Reset();

            int initialHealth = snake.Health;

            bool alive = snake.Move(newHeadPosition: 4, content: CellContent.Empty);

            Assert.That(alive, Is.True);
            Assert.That(snake.Health, Is.EqualTo(initialHealth - 1));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)4));
        }

        [Test]
        public void Move_EntraInEnemySnake_DiminuisceSaluteDiHazardDamage()
        {
            BattleSnake snake = new BattleSnake();
            snake.Reset();

            int initialHealth = snake.Health;

            bool alive = snake.Move(newHeadPosition: 5, content: CellContent.EnemySnake, hazardDamage: 15);

            Assert.That(alive, Is.True);
            Assert.That(snake.Health, Is.EqualTo(initialHealth - 15));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)5));
        }
    }
}
