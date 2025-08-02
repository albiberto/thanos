using Thanos.BitMasks;

namespace Thanos.Tests
{
    [TestFixture]
    public class BattleSnakeTests
    {
        private BattleSnake _snake;
        private const int HazardDamage = 15;
        private const int InitialHealth = 100;
        
        [SetUp]
        public void Setup()
        {
            _snake = new BattleSnake();
            _snake.Reset();
        }
        
        [Test]
        public void Reset_ImpostaValoriIniziali()
        {
            Assert.That(_snake.Health, Is.EqualTo(100));
            Assert.That(_snake.Length, Is.EqualTo(3));
            Assert.That(_snake.Head, Is.EqualTo(0U));
        }

        [Test]
        public void Move_EntraInHazard_DiminuisceSaluteDiHazardDamage()
        {
            var alive = _snake.Move(newHeadPosition: 2, content: CellContent.Hazard, hazardDamage: 15);

            Assert.That(alive, Is.True);
            Assert.That(_snake.Health, Is.EqualTo(InitialHealth - 15));
            Assert.That(_snake.Length, Is.EqualTo(3));
            Assert.That(_snake.Head, Is.EqualTo(2U));
        }

        [Test]
        public void Move_EntraInHazard_MuoreSeSaluteScendeSottoZero()
        {
            var snake = new BattleSnake();
            snake.Reset();

            snake.Health = 10;

            var alive = snake.Move(newHeadPosition: 3, content: CellContent.Hazard, hazardDamage: 15);

            Assert.That(alive, Is.False);
            Assert.That(snake.Health, Is.LessThanOrEqualTo(0));
            Assert.That(snake.Head, Is.EqualTo((ushort)3));
        }

        [Test]
        public void Move_MovimentoNormale_DiminuisceSaluteDiUno()
        {
            var snake = new BattleSnake();
            snake.Reset();

            var initialHealth = snake.Health;

            var alive = snake.Move(newHeadPosition: 4, content: CellContent.Empty);

            Assert.That(alive, Is.True);
            Assert.That(snake.Health, Is.EqualTo(initialHealth - 1));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)4));
        }

        [Test]
        public void Move_EntraInEnemySnake_DiminuisceSaluteDiHazardDamage()
        {
            var snake = new BattleSnake();
            snake.Reset();

            var initialHealth = snake.Health;

            var alive = snake.Move(newHeadPosition: 5, content: CellContent.EnemySnake, hazardDamage: 15);

            Assert.That(alive, Is.True);
            Assert.That(snake.Health, Is.EqualTo(initialHealth - 15));
            Assert.That(snake.Length, Is.EqualTo(3));
            Assert.That(snake.Head, Is.EqualTo((ushort)5));
        }
    }
}
