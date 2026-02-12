using System.Runtime.CompilerServices;
using System.Text;
using Spectre.Console;
using Thanos.War.Snake;
using Thanos.War.Structures;

namespace Thanos.War.State;

public readonly ref struct GameState(
    SnakesSystem system,
    Bitboard food,
    Bitboard hazards,
    Bitboard snakes) // Removed NeighborsMatrix
{
    public readonly SnakesSystem System = system;
    public readonly Bitboard Food = food;
    public readonly Bitboard Hazards = hazards;
    public readonly Bitboard Snakes = snakes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(in GameState source)
    {
        System.CopyFrom(in source.System);
        Food.CopyFrom(in source.Food);
        Hazards.CopyFrom(in source.Hazards);
        Snakes.CopyFrom(in source.Snakes);
    }

    public string Render(int width, int height)
    {
        var result = new StringBuilder();

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var position = (ushort)(x + y * width);

                var cellStyle = (x + y) % 2 == 0
                    ? new Style(background: Color.Black)
                    : new Style(background: Color.Grey);

                var isFood = Food.IsSet(position);
                var isHazard = Hazards.IsSet(position);
                var isSnake = Snakes.IsSet(position);

                if (isSnake)
                {
                    // Find which snake and determine if head/tail
                    var snakeId = -1;
                    var isHead = false;
                    var isTail = false;

                    for (var i = 0; i < System.Count; i++)
                    {
                        var snake = System[i];
                        if (!snake.IsDead && snake.Body.IsSet(position))
                        {
                            snakeId = i;
                            isHead = snake.Head == position;
                            isTail = snake.Tail == position;
                            break;
                        }
                    }

                    var snakeStyle = snakeId switch
                    {
                        0 => new Style(foreground: Color.Yellow, background: Color.DarkGreen),
                        1 => new Style(foreground: Color.Red, background: Color.DarkBlue),
                        2 => new Style(foreground: Color.Blue, background: Color.DarkRed),
                        3 => new Style(foreground: Color.Yellow, background: Color.DarkMagenta),
                        _ => new Style(foreground: Color.White, background: Color.Black),
                    };

                    if (isHead)
                    {
                        result.Append("OO".WithStyle(snakeStyle));
                    }
                    else if (isTail)
                    {
                        result.Append("()".WithStyle(snakeStyle));
                    }
                    else
                    {
                        result.Append("  ".WithStyle(snakeStyle));
                    }
                }
                else if (isFood)
                {
                    result.Append("🍎".WithStyle(cellStyle));
                }
                else if (isHazard)
                {
                    result.Append("XX".WithStyle(new Style(foreground: Color.DarkRed, background: Color.Black)));
                }
                else
                {
                    result.Append("  ".WithStyle(cellStyle));
                }
            }

            result.AppendLine();
        }

        return result.ToString();
    }
}

public static class StringExtensions
{
    extension(string self)
    {
        public string WithStyle(Style style)
            => $"[{style.ToMarkup()}]{self}[/]";
    }
}