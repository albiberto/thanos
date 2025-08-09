using System.Text;
using System.Text.Json;
using Thanos;
using Thanos.Parsers;

// Riutilizziamo le stesse struct/classi per contenere i dati
public struct Point { public int X { get; set; } public int Y { get; set; } }
public struct SnakeCustomizations { public string Color { get; set; } public string Head { get; set; } public string Tail { get; set; } }
public class Snake { public string Id { get; set; } public string Name { get; set; } public int Health { get; set; } public List<Point> Body { get; set; } public string Latency { get; set; } public Point Head { get; set; } public int Length { get; set; } public string Shout { get; set; } public SnakeCustomizations Customizations { get; set; } }

public class LowLevelParser
{
    public static void Main(string[] args)
    {
        const string jsonString = "";

        int turn;

        var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(jsonBytes);

        // Iniziamo il singolo passaggio attraverso il JSON
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Avanza al valore della proprietà

                switch (propertyName)
                {
                    case "game":
                        GameParser.Parse(ref reader);
                        break;
                    case "turn":
                        turn = reader.GetU();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }
    }
}

    