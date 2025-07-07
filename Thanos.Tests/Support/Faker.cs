using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Tests.Support;

public static class Faker
{
    private const int ALL_DIRECTIONS = MonteCarlo.UP | MonteCarlo.DOWN | MonteCarlo.LEFT | MonteCarlo.RIGHT;
    private const string Me = "Betty";

    public static IEnumerable<TestScenario> GetScenarios()
    {
        var boards = GetBoards();
        var scenarioNames = new[]
        {
            "11x11_CentralSnake_CornerEnemies",
            "19x19_LargeSnake_MultipleEnemies",
            "11x11_LongSnakes_TightSpace",
            "19x19_CenterBattle",
            "11x11_VeryLongSnakes",
            "19x19_SuperLongSnakes"
            // aggiungi altri nomi se hai più board/scenari
        };
        var expectedMoves = new[]
        {
            MonteCarlo.DOWN,
            MonteCarlo.UP,
            MonteCarlo.RIGHT,
            MonteCarlo.LEFT,
            MonteCarlo.DOWN,
            MonteCarlo.DOWN
            // aggiungi altri expected se hai più board/scenari
        };
        
        for (var i = 0; i < scenarioNames.Length && i < boards.Length; i++)
        {
            var board = boards[i];
            var moveRequest = new MoveRequest
            {
                Board = board,
                You = board.snakes.Single(s => s.id == Me)
            };
            
            yield return new TestScenario(i + 1, scenarioNames[i], moveRequest, expectedMoves[i]);
        }
    }

    private static Board[] GetBoards()
    {
        return [
            new Board
            {
                width = 11,
                height = 11,
                hazards = [
                    new Point { x = 5, y = 5 },
                    new Point { x = 3, y = 7 },
                    new Point { x = 8, y = 2 }
                ],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 90,
                        head = new Point { x = 5, y = 3 },
                        body = [
                            new Point { x = 5, y = 3 },
                            new Point { x = 5, y = 4 },
                            new Point { x = 5, y = 5 },
                            new Point { x = 4, y = 5 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 100,
                        head = new Point { x = 0, y = 0 },
                        body = [
                            new Point { x = 0, y = 0 },
                            new Point { x = 1, y = 0 },
                            new Point { x = 2, y = 0 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy2",
                        health = 80,
                        head = new Point { x = 10, y = 10 },
                        body = [
                            new Point { x = 10, y = 10 },
                            new Point { x = 9, y = 10 },
                            new Point { x = 8, y = 10 }
                        ]
                    }
                ]
            },
            new Board
            {
                width = 19,
                height = 19,
                hazards = [
                    new Point { x = 9, y = 9 },
                    new Point { x = 10, y = 10 },
                    new Point { x = 6, y = 10 },
                    new Point { x = 18, y = 17 },
                    new Point { x = 5, y = 5 }
                ],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 95,
                        head = new Point { x = 1, y = 1 },
                        body = [
                            new Point { x = 1, y = 1 },
                            new Point { x = 2, y = 1 },
                            new Point { x = 3, y = 1 },
                            new Point { x = 4, y = 1 },
                            new Point { x = 5, y = 1 },
                            new Point { x = 6, y = 1 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 100,
                        head = new Point { x = 18, y = 0 },
                        body = [
                            new Point { x = 18, y = 0 },
                            new Point { x = 17, y = 0 },
                            new Point { x = 16, y = 0 },
                            new Point { x = 15, y = 0 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy2",
                        health = 70,
                        head = new Point { x = 0, y = 18 },
                        body = [
                            new Point { x = 0, y = 18 },
                            new Point { x = 1, y = 18 },
                            new Point { x = 2, y = 18 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy3",
                        health = 85,
                        head = new Point { x = 18, y = 18 },
                        body = [
                            new Point { x = 18, y = 18 },
                            new Point { x = 17, y = 18 },
                            new Point { x = 16, y = 18 },
                            new Point { x = 15, y = 18 },
                            new Point { x = 14, y = 18 }
                        ]
                    }
                ]
            },
            new Board
            {
                width = 11,
                height = 11,
                hazards = [
                    new Point { x = 5, y = 5 },
                    new Point { x = 5, y = 6 },
                    new Point { x = 4, y = 5 },
                    new Point { x = 6, y = 5 }
                ],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 100,
                        head = new Point { x = 1, y = 1 },
                        body = [
                            new Point { x = 1, y = 1 },
                            new Point { x = 0, y = 1 },
                            new Point { x = 0, y = 2 },
                            new Point { x = 0, y = 3 },
                            new Point { x = 1, y = 3 },
                            new Point { x = 2, y = 3 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 90,
                        head = new Point { x = 10, y = 1 },
                        body = [
                            new Point { x = 10, y = 1 },
                            new Point { x = 9, y = 1 },
                            new Point { x = 8, y = 1 },
                            new Point { x = 7, y = 1 },
                            new Point { x = 6, y = 1 }
                        ]
                    }
                ]
            },
            new Board
            {
                width = 19,
                height = 19,
                hazards = [
                    new Point { x = 9, y = 8 },
                    new Point { x = 9, y = 10 },
                    new Point { x = 8, y = 9 },
                    new Point { x = 10, y = 9 }
                ],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 100,
                        head = new Point { x = 7, y = 9 },
                        body = [
                            new Point { x = 7, y = 9 },
                            new Point { x = 6, y = 9 },
                            new Point { x = 5, y = 9 },
                            new Point { x = 4, y = 9 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 100,
                        head = new Point { x = 11, y = 9 },
                        body = [
                            new Point { x = 11, y = 9 },
                            new Point { x = 12, y = 9 },
                            new Point { x = 13, y = 9 },
                            new Point { x = 14, y = 9 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy2",
                        health = 80,
                        head = new Point { x = 9, y = 7 },
                        body = [
                            new Point { x = 9, y = 7 },
                            new Point { x = 9, y = 6 },
                            new Point { x = 9, y = 5 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy3",
                        health = 90,
                        head = new Point { x = 9, y = 11 },
                        body = [
                            new Point { x = 9, y = 11 },
                            new Point { x = 9, y = 12 },
                            new Point { x = 9, y = 13 },
                            new Point { x = 9, y = 14 },
                            new Point { x = 9, y = 15 }
                        ]
                    }
                ]
            },
            new Board
            {
                width = 11,
                height = 11,
                hazards = [new Point { x = 5, y = 5 }],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 100,
                        head = new Point { x = 0, y = 0 },
                        body = [
                            new Point { x = 0, y = 0 },
                            new Point { x = 1, y = 0 },
                            new Point { x = 2, y = 0 },
                            new Point { x = 3, y = 0 },
                            new Point { x = 4, y = 0 },
                            new Point { x = 5, y = 0 },
                            new Point { x = 6, y = 0 },
                            new Point { x = 7, y = 0 },
                            new Point { x = 8, y = 0 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 100,
                        head = new Point { x = 10, y = 10 },
                        body = [
                            new Point { x = 10, y = 10 },
                            new Point { x = 9, y = 10 },
                            new Point { x = 8, y = 10 },
                            new Point { x = 7, y = 10 },
                            new Point { x = 6, y = 10 },
                            new Point { x = 5, y = 10 },
                            new Point { x = 4, y = 10 }
                        ]
                    }
                ]
            },
            new Board
            {
                width = 19,
                height = 19,
                hazards = [
                    new Point { x = 9, y = 9 },
                    new Point { x = 10, y = 9 },
                    new Point { x = 8, y = 9 }
                ],
                snakes = [
                    new Snake
                    {
                        id = Me,
                        health = 100,
                        head = new Point { x = 0, y = 0 },
                        body = [
                            new Point { x = 0, y = 0 },
                            new Point { x = 1, y = 0 },
                            new Point { x = 2, y = 0 },
                            new Point { x = 3, y = 0 },
                            new Point { x = 4, y = 0 },
                            new Point { x = 5, y = 0 },
                            new Point { x = 6, y = 0 },
                            new Point { x = 7, y = 0 },
                            new Point { x = 8, y = 0 },
                            new Point { x = 9, y = 0 },
                            new Point { x = 10, y = 0 },
                            new Point { x = 11, y = 0 },
                            new Point { x = 12, y = 0 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy1",
                        health = 100,
                        head = new Point { x = 18, y = 18 },
                        body = [
                            new Point { x = 18, y = 18 },
                            new Point { x = 17, y = 18 },
                            new Point { x = 16, y = 18 },
                            new Point { x = 15, y = 18 },
                            new Point { x = 14, y = 18 },
                            new Point { x = 13, y = 18 },
                            new Point { x = 12, y = 18 },
                            new Point { x = 11, y = 18 },
                            new Point { x = 10, y = 18 },
                            new Point { x = 9, y = 18 },
                            new Point { x = 8, y = 18 }
                        ]
                    },
                    new Snake
                    {
                        id = "enemy2",
                        health = 85,
                        head = new Point { x = 0, y = 18 },
                        body = [
                            new Point { x = 0, y = 18 },
                            new Point { x = 0, y = 17 },
                            new Point { x = 0, y = 16 },
                            new Point { x = 0, y = 15 },
                            new Point { x = 0, y = 14 },
                            new Point { x = 0, y = 13 },
                            new Point { x = 0, y = 12 },
                            new Point { x = 0, y = 11 }
                        ]
                    }
                ]
            }
        ];
    }
}

public class TestScenario(int id, string name, MoveRequest moveRequest, int expected)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public MoveRequest MoveRequest { get; } = moveRequest;
    public int Expected { get; } = expected;
}