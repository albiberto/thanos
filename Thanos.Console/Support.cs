using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Console;

public static class Support
{
    public static Board BuildBoard() =>
        new()
        {
            width = 19,
            height = 19,
            hazards =
            [
                new Point { x = 9, y = 9 },
                new Point { x = 10, y = 10 },
                new Point { x = 6, y = 10 },
                new Point { x = 18, y = 17 }
            ],
            snakes =
            [
                new Snake
                {
                    id = "me",
                    health = 90,
                    head = new Point { x = 1, y = 1 },
                    body =
                    [
                        new Point { x = 2, y = 1 },
                        new Point { x = 3, y = 1 },
                        new Point { x = 4, y = 1 }
                    ]
                },
                new Snake
                {
                    id = "enemy1",
                    health = 100,
                    head = new Point { x = 18, y = 0 },
                    body =
                    [
                        new Point { x = 18, y = 0 },
                        new Point { x = 17, y = 0 },
                        new Point { x = 16, y = 0 }
                    ]
                },
                new Snake
                {
                    id = "enemy2",
                    health = 70,
                    head = new Point { x = 0, y = 18 },
                    body =
                    [
                        new Point { x = 0, y = 18 },
                        new Point { x = 1, y = 18 },
                        new Point { x = 2, y = 18 }
                    ]
                },
                new Snake
                {
                    id = "enemy3",
                    health = 60,
                    head = new Point { x = 18, y = 18 },
                    body =
                    [
                        new Point { x = 18, y = 18 },
                        new Point { x = 17, y = 18 },
                        new Point { x = 16, y = 18 }
                    ]
                }
            ]
        };
}