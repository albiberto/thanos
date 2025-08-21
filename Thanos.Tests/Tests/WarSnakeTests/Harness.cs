using Thanos.War.Snake;

namespace Thanos.Tests.Tests.WarSnakeTests;

public static class Harness
{
    public struct SnakeTestContext
    {
        public Health Health;
        public Anatomy Anatomy;
        public ushort[] BodyBuffer;
    }
    
    public static SnakeTestContext CreateTestContext(int capacity, ushort[] initialBody, int initialHp = 100)
    {
        // Il buffer del corpo deve avere la capacità richiesta.
        var bodyBuffer = new ushort[capacity];

        return new SnakeTestContext
        {
            Health = new Health(), 
            Anatomy = new Anatomy(),
            BodyBuffer = bodyBuffer
        };
    }
}