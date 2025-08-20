namespace Thanos.Tests.WarSnakeTests;

public record TestCase(int Health, uint Capacity, ushort[] Body);

[TestFixtureSource(nameof(Cases))]
public partial class SnakeTests(TestCase @case)
{
    private static readonly uint[] Capacities = [16, 32, 64, 128, 256, 512, 1024];

    private static readonly TestCase[] Cases = BuildSnakeUnitTests(Capacities).ToArray();

    private static IEnumerable<TestCase> BuildSnakeUnitTests(uint[] capacities)
    {
        foreach (var capacity in capacities)
        {
            // Scenario 1: Serpente minimo (lunghezza 1)
            var lenght = 1u;
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));

            // Scenario 2: Serpente corto (25% della capacità)
            lenght = (uint)(capacity * .25);
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));

            // Scenario 3: Serpente medio (50% della capacità)
            lenght = (uint)(capacity * .50);
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));
            
            // Scenario 4: Serpente medio (75% della capacità)
            lenght = (uint)(capacity * .75);
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));
            
            // Scenario 5: Serpente quasi pieno (capacità - 1) - per testare il "wrap-around" del buffer
            lenght = capacity - 1;
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));
            
            // Scenario 6: Serpente completamente pieno
            lenght = capacity;
            yield return new TestCase(100, capacity, CreateSequentialBody(lenght));
        }
    }
    
    private static ushort[] CreateSequentialBody(uint length)
    {
        return length <= 0 
            ? [] :
            Enumerable.Range(0, (int)length).Select(i => (ushort)i).ToArray();
    }
}