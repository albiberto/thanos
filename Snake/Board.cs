namespace Snakes.Core;

using System.Text;
using Extensions;
using Spectre.Console;

using BoardEntry = byte;
using BoardIndex = int;

public static class BoardEntryExtensions
{
    const int maxSnakes = 4;

    extension(BoardEntry value)
    {
        public static BoardEntry Empty => 0;
        public static BoardEntry Food => 1;
        public static BoardEntry Snake(BoardIndex next) => (BoardEntry)(2 + next);
        public static BoardEntry Head(int id) => (BoardEntry)(BoardEntry.MaxValue - id);
        
        public bool IsEmpty => value == BoardEntry.Empty;
        public bool IsFood => value == BoardEntry.Food;
        public bool IsSnake => value >= 2;
        public bool IsHead => value >= BoardEntry.MaxValue - maxSnakes;

        public int Id => BoardEntry.MaxValue - value;
        public BoardEntry Next => (BoardEntry)(value - 2);
    }
}

public static class BoardIndexExtensions
{
    extension(BoardIndex index)
    {
        public BoardIndex Left => index - 1;
        public BoardIndex Right => index + 1;
        public BoardIndex Up(int width) => index + width;
        public BoardIndex Down(int width) => index - width;
        
        public static BoardIndex FromCoordinates(int x, int y, int height, int width)
            => x + (height - y - 1) * width;
    }
}

public struct Snake(BoardIndex head)
{
    public BoardIndex Head = head;
    public BoardIndex Tail = head;

    public int Health = 100;
    public int Lenght = 1;
    public int EatenFood = 2;
}

public struct Board : IClonable<Board.Parameters>
{
    public readonly ref struct Parameters
    {
        public required int Width { get; init; }
        public required int Height { get; init; }

        public required Span<BoardIndex> Snakes { get; init; }
    }

    int width;
    int height;
    
    public int Size => width * height;
    
    public RelativeSpan<BoardEntry> State;
    public RelativeSpan<Snake> Snakes;

    public void New(Allocator allocator, Parameters parameters)
    {
        width = parameters.Width;
        height = parameters.Height;

        var size = width * height;
        
        State.New(allocator, size);
        Snakes.New(allocator, parameters.Snakes.Length);
        
        State.Span.Clear();

        var snakes = Snakes.Span;
        for (var i = 0; i < snakes.Length; i++)
        {
            snakes[i] = new Snake(parameters.Snakes[i]);
        }
    }

    public void BeginTurn()
    {
        var snakes = Snakes.Span;
        var state = State.Span;
        
        foreach (ref var snake in snakes)
        {
            if (snake is { Health: > 0, EatenFood: 0 })
            {
                var tailIndex = snake.Tail;
                snake.Tail = state[tailIndex].Next;
                state[tailIndex] = BoardEntry.Empty;
            }
        }
    }

    public void MoveSnakes(byte[] moves)
    {
        var snakes = Snakes.Span;
        var state = State.Span;
        
        for (var id = 0; id < snakes.Length; id++)
        {
            if (snakes[id].Health <= 0)
            {
                continue;
            }
            
            var location = moves[id] switch
            {
                0 => snakes[id].Head.Left,
                1 => snakes[id].Head.Right,
                2 => snakes[id].Head.Up(width),
                3 => snakes[id].Head.Down(width),
                _ => throw new ArgumentOutOfRangeException(nameof(moves), "Invalid move value")
            };
            
            if(!IsValidMove(snakes[id].Head, location))
            {
                snakes[id].Health = 0;
                continue;
            }
            
            ref var snake = ref snakes[id];

            if (snake.EatenFood > 0)
            {
                snake.Lenght++;
                snake.EatenFood--;
            }

            snake.Health--;

            state[snake.Head] = BoardEntry.Snake(location);
            snake.Head = location;

            ref var current = ref state[location];

            if (current.IsFood)
            {
                snake.EatenFood++;
                snake.Health = 100;
                current = BoardEntry.Head(id);
            }
            else if (current.IsHead)
            {
                var opponentId = current.Id;
                ref var opponent = ref snakes[opponentId];

                if (snake.Lenght <= opponent.Lenght)
                {
                    snake.Health = 0;
                    current = BoardEntry.Head(opponentId);
                }

                if (snake.Lenght >= opponent.Lenght)
                {
                    opponent.Health = 0;
                    current = BoardEntry.Head(id);
                }
            }
            else
            {
                current = BoardEntry.Head(id);
            }
        }
    }
    
    public void EndTurn(bool spawnFood)
    {
        var snakes = Snakes.Span;
        var state = State.Span;
        
        for (var index = 0; index < snakes.Length; index++)
        {
            ref var snake = ref snakes[index];
            if (snake.Health == 0)
            {
                RemoveSnakeBody(ref snake);
                snake.Health = -1;

                if (state[snake.Head].Id == index)
                {
                    state[snake.Head] = BoardEntry.Empty;
                }
            }
        }

        if (spawnFood && Random.Shared.Next(0, 100) <= 20)
        {
            var empty = state.Count(BoardEntry.Empty);
            
            if (empty != 0)
            {
                var next = Random.Shared.Next(0, empty);
                var at = state.NthIndexOf(BoardEntry.Empty, next);

                state[at] = BoardEntry.Food;
            }
        }
    }
    
    void RemoveSnakeBody(ref Snake snake)
    {
        var state = State.Span;
        ref var current = ref state[snake.Tail];
        while (!current.IsHead)
        {
            var value = current;
            current = BoardEntry.Empty;
            current = ref state[value.Next];
        }
    }

    public string Render(IReadOnlyList<string> messages)
    {
        var snakes = Snakes.Span;
        var state = State.Span;
                
        messages =
        [
            ..messages,
            "",
            ..snakes.ToArray().Reverse().Select((snake, index) => $"Health {index}: {snake.Health.ToString(),-3}").ToArray(),
        ];
        
        static Style SnakeStyle(Span<BoardEntry> state, int index)
        {
            while (index < state.Length && !state[index].IsHead)
            {
                index = state[index].Next;
            }
            
            var id = index < state.Length ? state[index].Id : 4;
            return id switch
            {
                0 => new Style(foreground: Color.Yellow, background: Color.DarkGreen),
                1 => new Style(foreground: Color.Red, background: Color.DarkBlue),
                2 => new Style(foreground: Color.Blue, background: Color.DarkRed),
                3 => new Style(foreground: Color.Yellow, background: Color.DarkMagenta),
                _ => new Style(foreground: Color.White, background: Color.Black),
            };
        }

        var result = new StringBuilder();

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var index = BoardIndex.FromCoordinates(x, y, height, width);

                var snakeStyle = SnakeStyle(state, index);
                var cellStyle = (x + y) % 2 == 0 
                    ? new Style(background: Color.Black) 
                    : new Style(background: Color.Grey);

                var isFood = state[index].IsFood;
                var isSnake = state[index].IsSnake;
                var isHead = state[index].IsHead;
                var isTail = snakes.ToArray().Any(s => s.Tail == index);

                if (isHead)
                {
                    result.Append("OO".WithStyle(snakeStyle));
                }
                else if (isTail)
                {
                    result.Append("()".WithStyle(snakeStyle));
                }
                else if (isSnake)
                {
                    result.Append("  ".WithStyle(snakeStyle));
                }
                else if (isFood)
                {
                    result.Append("🍎".WithStyle(cellStyle));
                }
                else
                {
                    result.Append("  ".WithStyle(cellStyle));
                }
            }

            if (y < messages.Count)
            {
                result.Append($"  {messages[y]}");
            }

            result.AppendLine();
        }

        return result.ToString();
    }

    public bool IsValidMove(BoardIndex from, BoardIndex to)
    {
        var snakes = Snakes.Span;
        var state = State.Span;

        if (to < 0 || to >= state.Length || state[to].IsSnake)
        {
            return false;
        }

        if (from % width == 0 && to % width == width - 1)
        {
            return false;
        }
        
        if (from % width == width - 1 && to % width == 0)
        {
            return false;
        }

        return true;
    }

    public Span<BoardIndex> GetValidMoves(BoardIndex[] moves, int snakeId)
    {
        var snakes = Snakes.Span;
        var state = State.Span;
        
        ref var snake = ref snakes[snakeId];
        var head = snake.Head;
        var count = 0;

        if (head % width > 0)
        {
            var left = head.Left;
            if (!state[left].IsSnake)
            {
                moves[count++] = left;
            }
        }
        
        if (head % width < width - 1)
        {
            var right = head.Right;
            if (!state[right].IsSnake)
            {
                moves[count++] = right;
            }
        }
        
        if (head / width < height - 1)
        {
            var up = head.Up(width);
            if (!state[up].IsSnake)
            {
                moves[count++] = up;
            }
        }
        
        if (head / width > 0)
        {
            var down = head.Down(width);
            if (!state[down].IsSnake)
            {
                moves[count++] = down;
            }
        }
        
        return moves.AsSpan(0, count);
    }
}