// using System;
// using System.Collections.Generic;
// using System.Linq;
//
// // Enum per direzioni
// public enum Direction { Up, Down, Left, Right }
//
// // Stato semplice del gioco
// public class GameState
// {
//     public int Width { get; set; } = 11;
//     public int Height { get; set; } = 11;
//     public List<List<Point>> Snakes { get; set; } = new();
//     public List<Point> Food { get; set; } = new();
//     public List<Point> Hazards { get; set; } = new(); // AGGIUNTO: Zone pericolose
//     public List<int> SnakeHealths { get; set; } = new(); // AGGIUNTO: Salute snake
//     public int MySnakeIndex { get; set; } = 0;
//     public int Turn { get; set; } = 0;
//
//     public Point MyHead => Snakes[MySnakeIndex][0];
//     public int MyHealth => SnakeHealths[MySnakeIndex];
//     
//     public GameState Copy()
//     {
//         return new GameState
//         {
//             Width = Width,
//             Height = Height,
//             Snakes = Snakes.Select(snake => new List<Point>(snake)).ToList(),
//             Food = new List<Point>(Food),
//             Hazards = new List<Point>(Hazards), // AGGIUNTO
//             SnakeHealths = new List<int>(SnakeHealths), // AGGIUNTO
//             MySnakeIndex = MySnakeIndex,
//             Turn = Turn
//         };
//     }
//
//     public List<Direction> GetLegalMoves()
//     {
//         var moves = new List<Direction>();
//         var head = MyHead;
//
//         // Prova ogni direzione
//         foreach (Direction dir in Enum.GetValues<Direction>())
//         {
//             var newPos = Move(head, dir);
//             if (IsValidMove(newPos))
//                 moves.Add(dir);
//         }
//
//         return moves.Count > 0 ? moves : new List<Direction> { Direction.Up }; // Fallback
//     }
//
//     public void ApplyMove(Direction move)
//     {
//         var mySnake = Snakes[MySnakeIndex];
//         var newHead = Move(mySnake[0], move);
//         
//         // Muovi la testa
//         mySnake.Insert(0, newHead);
//         
//         // Controlla se mangia cibo
//         bool ateFood = Food.Contains(newHead);
//         if (ateFood)
//         {
//             Food.Remove(newHead);
//             // Snake cresce quando mangia (non rimuove coda)
//         }
//         else
//         {
//             // Rimuovi coda se non ha mangiato
//             mySnake.RemoveAt(mySnake.Count - 1);
//         }
//
//         // Controlla hazard - riduce salute
//         if (Hazards.Contains(newHead))
//         {
//             SnakeHealths[MySnakeIndex] = Math.Max(0, SnakeHealths[MySnakeIndex] - 15);
//         }
//         else
//         {
//             // Recupera salute se non su hazard
//             SnakeHealths[MySnakeIndex] = Math.Min(100, SnakeHealths[MySnakeIndex] + 1);
//         }
//
//         // Muovi altri snake (semplificato - mosse casuali)
//         for (int i = 0; i < Snakes.Count; i++)
//         {
//             if (i == MySnakeIndex) continue;
//             
//             var snake = Snakes[i];
//             var possibleMoves = GetValidMovesForSnake(i);
//             if (possibleMoves.Count > 0)
//             {
//                 var randomMove = possibleMoves[Random.Shared.Next(possibleMoves.Count)];
//                 var newEnemyHead = Move(snake[0], randomMove);
//                 snake.Insert(0, newEnemyHead);
//                 
//                 // Nemici mangiano cibo
//                 bool enemyAteFood = Food.Contains(newEnemyHead);
//                 if (enemyAteFood)
//                 {
//                     Food.Remove(newEnemyHead);
//                 }
//                 else
//                 {
//                     snake.RemoveAt(snake.Count - 1);
//                 }
//
//                 // Nemici perdono salute su hazard
//                 if (Hazards.Contains(newEnemyHead))
//                 {
//                     SnakeHealths[i] = Math.Max(0, SnakeHealths[i] - 15);
//                 }
//                 else
//                 {
//                     SnakeHealths[i] = Math.Min(100, SnakeHealths[i] + 1);
//                 }
//             }
//         }
//
//         // Rimuovi snake morti (salute = 0)
//         RemoveDeadSnakes();
//         
//         // Aggiungi cibo periodicamente
//         SpawnFoodIfNeeded();
//
//         Turn++;
//     }
//
//     private List<Direction> GetValidMovesForSnake(int snakeIndex)
//     {
//         var moves = new List<Direction>();
//         var snake = Snakes[snakeIndex];
//         var head = snake[0];
//
//         foreach (Direction dir in Enum.GetValues<Direction>())
//         {
//             var newPos = Move(head, dir);
//             if (IsInBounds(newPos) && !IsOccupied(newPos))
//                 moves.Add(dir);
//         }
//
//         return moves;
//     }
//
//     private bool IsValidMove(Point pos)
//     {
//         return IsInBounds(pos) && !IsOccupied(pos);
//     }
//
//     private bool IsInBounds(Point pos)
//     {
//         return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
//     }
//
//     private bool IsOccupied(Point pos)
//     {
//         return Snakes.Any(snake => snake.Contains(pos));
//     }
//
//     // AGGIUNTO: Rimuove snake morti
//     private void RemoveDeadSnakes()
//     {
//         for (int i = Snakes.Count - 1; i >= 0; i--)
//         {
//             if (SnakeHealths[i] <= 0)
//             {
//                 Snakes.RemoveAt(i);
//                 SnakeHealths.RemoveAt(i);
//                 
//                 // Aggiusta indice se necessario
//                 if (i < MySnakeIndex)
//                     MySnakeIndex--;
//                 else if (i == MySnakeIndex)
//                     MySnakeIndex = -1; // Sono morto
//             }
//         }
//     }
//
//     // AGGIUNTO: Spawna cibo periodicamente
//     private void SpawnFoodIfNeeded()
//     {
//         // Spawna cibo ogni 10 turni se ce n'è poco
//         if (Turn % 10 == 0 && Food.Count < 3)
//         {
//             var emptyCells = GetEmptyCells();
//             if (emptyCells.Count > 0)
//             {
//                 var randomCell = emptyCells[Random.Shared.Next(emptyCells.Count)];
//                 Food.Add(randomCell);
//             }
//         }
//     }
//
//     // AGGIUNTO: Trova celle vuote
//     private List<Point> GetEmptyCells()
//     {
//         var empty = new List<Point>();
//         for (int x = 0; x < Width; x++)
//         {
//             for (int y = 0; y < Height; y++)
//             {
//                 var point = new Point(x, y);
//                 if (!IsOccupied(point) && !Food.Contains(point) && !Hazards.Contains(point))
//                 {
//                     empty.Add(point);
//                 }
//             }
//         }
//         return empty;
//     }
//
//     private Point Move(Point pos, Direction dir)
//     {
//         return dir switch
//         {
//             Direction.Up => new Point(pos.X, pos.Y - 1),
//             Direction.Down => new Point(pos.X, pos.Y + 1),
//             Direction.Left => new Point(pos.X - 1, pos.Y),
//             Direction.Right => new Point(pos.X + 1, pos.Y),
//             _ => pos
//         };
//     }
//
//     public bool IsGameOver()
//     {
//         // Sono morto se il mio indice è -1 o la mia salute è 0
//         if (MySnakeIndex == -1 || MySnakeIndex >= Snakes.Count)
//             return true;
//             
//         if (SnakeHealths[MySnakeIndex] <= 0)
//             return true;
//             
//         var mySnake = Snakes[MySnakeIndex];
//         return mySnake.Count == 0 || !IsInBounds(MyHead) || IsOccupied(MyHead);
//     }
//
//     public double GetResult()
//     {
//         // Se sono morto = sconfitta
//         if (IsGameOver())
//             return 0.0;
//         
//         // Calcola punteggio basato su più fattori
//         double score = 0.0;
//         
//         // 1. LUNGHEZZA SNAKE (40% del punteggio)
//         var myLength = Snakes[MySnakeIndex].Count;
//         var avgEnemyLength = Snakes.Where((s, i) => i != MySnakeIndex).Average(s => s.Count);
//         if (myLength > avgEnemyLength)
//             score += 0.4;
//         else if (myLength == avgEnemyLength)
//             score += 0.2;
//         
//         // 2. SALUTE (30% del punteggio)
//         var healthRatio = MyHealth / 100.0;
//         score += 0.3 * healthRatio;
//         
//         // 3. DISTANZA DAL CIBO (20% del punteggio)
//         if (Food.Count > 0 && MyHealth < 50) // Cerca cibo se salute bassa
//         {
//             var nearestFoodDistance = Food.Min(f => ManhattanDistance(MyHead, f));
//             var maxDistance = Width + Height;
//             var distanceScore = 1.0 - (nearestFoodDistance / (double)maxDistance);
//             score += 0.2 * distanceScore;
//         }
//         else
//         {
//             score += 0.2; // Bonus se non serve cibo
//         }
//         
//         // 4. EVITARE HAZARD (10% del punteggio)
//         if (!Hazards.Contains(MyHead))
//             score += 0.1;
//         
//         return Math.Min(1.0, score);
//     }
//
//     // AGGIUNTO: Calcola distanza Manhattan
//     private int ManhattanDistance(Point a, Point b)
//     {
//         return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
//     }
// }
//
// // Punto semplice
// public record Point(int X, int Y);
//
// // Nodo MCTS
// public class MCTSNode
// {
//     public GameState State { get; set; }
//     public MCTSNode Parent { get; set; }
//     public Direction? Move { get; set; }
//     public List<MCTSNode> Children { get; set; } = new();
//     public List<Direction> UntriedMoves { get; set; }
//     public int Visits { get; set; } = 0;
//     public double Wins { get; set; } = 0;
//
//     public MCTSNode(GameState state, MCTSNode parent = null, Direction? move = null)
//     {
//         State = state;
//         Parent = parent;
//         Move = move;
//         UntriedMoves = state.GetLegalMoves();
//     }
//
//     public bool IsFullyExpanded => UntriedMoves.Count == 0;
//     public bool IsTerminal => State.IsGameOver();
//
//     public double UCB1Value(double c = 1.414) // sqrt(2)
//     {
//         if (Visits == 0) return double.MaxValue;
//         
//         double exploitation = Wins / Visits;
//         double exploration = c * Math.Sqrt(Math.Log(Parent.Visits) / Visits);
//         return exploitation + exploration;
//     }
//
//     public MCTSNode BestUCB1Child()
//     {
//         return Children.OrderByDescending(child => child.UCB1Value()).First();
//     }
//
//     public MCTSNode AddChild(Direction move)
//     {
//         var newState = State.Copy();
//         newState.ApplyMove(move);
//         var child = new MCTSNode(newState, this, move);
//         
//         UntriedMoves.Remove(move);
//         Children.Add(child);
//         return child;
//     }
//
//     public void Update(double result)
//     {
//         Visits++;
//         Wins += result;
//     }
// }
//
// // MCTS principale
// public class MCTS
// {
//     private readonly int maxIterations;
//
//     public MCTS(int maxIterations = 1000)
//     {
//         this.maxIterations = maxIterations;
//     }
//
//     public Direction Search(GameState rootState)
//     {
//         var root = new MCTSNode(rootState);
//
//         for (int i = 0; i < maxIterations; i++)
//         {
//             // 1. SELECTION
//             var node = Select(root);
//
//             // 2. EXPANSION  
//             if (!node.IsTerminal && node.UntriedMoves.Count > 0)
//             {
//                 var move = node.UntriedMoves[Random.Shared.Next(node.UntriedMoves.Count)];
//                 node = node.AddChild(move);
//             }
//
//             // 3. SIMULATION
//             var result = Simulate(node);
//
//             // 4. BACKPROPAGATION
//             Backpropagate(node, result);
//         }
//
//         // Restituisce mossa del figlio più visitato
//         var bestChild = root.Children.OrderByDescending(c => c.Visits).FirstOrDefault();
//         return bestChild?.Move ?? Direction.Up;
//     }
//
//     private MCTSNode Select(MCTSNode node)
//     {
//         while (node.Children.Count > 0 && node.IsFullyExpanded)
//         {
//             node = node.BestUCB1Child();
//         }
//         return node;
//     }
//
//     private double Simulate(MCTSNode node)
//     {
//         var state = node.State.Copy();
//         int maxMoves = 100; // Limita simulazione
//         int moves = 0;
//
//         while (!state.IsGameOver() && moves < maxMoves)
//         {
//             var legalMoves = state.GetLegalMoves();
//             if (legalMoves.Count == 0) break;
//             
//             var randomMove = legalMoves[Random.Shared.Next(legalMoves.Count)];
//             state.ApplyMove(randomMove);
//             moves++;
//         }
//
//         return state.GetResult();
//     }
//
//     private void Backpropagate(MCTSNode node, double result)
//     {
//         while (node != null)
//         {
//             node.Update(result);
//             node = node.Parent;
//         }
//     }
// }
//
// // Esempio di utilizzo
// public class BattlesnakeAI
// {
//     private readonly MCTS mcts = new MCTS(1000);
//
//     public string GetMove(GameState gameState)
//     {
//         var bestMove = mcts.Search(gameState);
//         return bestMove.ToString().ToLower();
//     }
// }
//
// // Test rapido
// public class Program
// {
//     public static void Main()
//     {
//         // Crea stato di gioco di esempio con food e hazard
//         var gameState = new GameState
//         {
//             Snakes = new List<List<Point>>
//             {
//                 new() { new(5, 5), new(5, 6), new(5, 7) }, // Il mio snake
//                 new() { new(3, 3), new(3, 4), new(3, 5) }  // Nemico
//             },
//             SnakeHealths = new List<int> { 80, 60 }, // AGGIUNTO: Salute
//             Food = new List<Point> { new(8, 8), new(2, 2) },
//             Hazards = new List<Point> // AGGIUNTO: Zone pericolose
//             { 
//                 new(1, 1), new(1, 2), new(2, 1), // Cluster hazard
//                 new(9, 9), new(9, 10), new(10, 9) 
//             },
//             MySnakeIndex = 0
//         };
//
//         var ai = new BattlesnakeAI();
//         var move = ai.GetMove(gameState);
//         
//         Console.WriteLine($"Mossa consigliata: {move}");
//         Console.WriteLine($"Mia salute: {gameState.MyHealth}");
//         Console.WriteLine($"Cibo disponibile: {gameState.Food.Count}");
//         Console.WriteLine($"Hazard presenti: {gameState.Hazards.Count}");
//         
//         // Simula alcuni turni per vedere l'evoluzione
//         Console.WriteLine("\n--- Simulazione ---");
//         for (int i = 0; i < 5; i++)
//         {
//             move = ai.GetMove(gameState);
//             Console.WriteLine($"Turno {gameState.Turn}: {move} (Salute: {gameState.MyHealth})");
//             
//             // Applica la mossa per testare
//             var moveEnum = Enum.Parse<Direction>(move, true);
//             gameState.ApplyMove(moveEnum);
//             
//             if (gameState.IsGameOver())
//             {
//                 Console.WriteLine("GAME OVER!");
//                 break;
//             }
//         }
//     }
// }