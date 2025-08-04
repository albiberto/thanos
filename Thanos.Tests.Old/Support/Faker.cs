// using System.Text.Json;
//
// namespace Thanos.Tests.Old.Support;
//
// public static class Faker
// {
//     private static readonly uint[] Sizes = [7, 11, 15, 19];
//     
//     /// <summary>
//     /// 1. Scenari di base - posizioni critiche
//     /// 2. Scenari con diversi numeri di nemici
//     /// 3. Scenari con diversi livelli di hazard
//     /// 4. Scenari con lunghezze diverse dei serpenti
//     /// 5. Scenari di bordo/angolo
//     /// 6. Scenari di collisione imminente
//     /// 7. Scenari di salute critica
//     /// 8. Scenari di controllo territorio
//     /// </summary>
//     /// <returns>La lista di tutti i possibili scenari</returns>
//     public static List<Scenario> GetAllScenarios(string scenarioBatch)
//     {
//         var currentDir = Directory.GetCurrentDirectory();
//         var projectRoot = Directory.GetParent(currentDir)!.Parent!.Parent!.FullName;
//         var jsonPath = Path.Combine(projectRoot, "Boards", $"{scenarioBatch}.json");
//
//         if (!File.Exists(jsonPath)) throw new FileNotFoundException($"File '{scenarioBatch}.json' non trovato nel percorso: {jsonPath}");
//
//         try
//         {
//             var json = File.ReadAllText(jsonPath);
//
//             var scenarios = JsonSerializer.Deserialize<List<Scenario>>(json, new JsonSerializerOptions
//             {
//                 PropertyNameCaseInsensitive = true
//             }) ?? [];
//
//             foreach (var scenario in scenarios)
//             {
//                 scenario.MoveRequest.You = scenario.MoveRequest.Board.snakes.Single(snake => string.Equals(snake.id, Constants.Me, StringComparison.OrdinalIgnoreCase));
//                 scenario.FileName = scenarioBatch;
//             }
//             
//             return scenarios;
//         }
//         catch (JsonException ex)
//         {
//             throw new InvalidOperationException("Errore nella deserializzazione del file 'boards.json'.", ex);
//         }
//     }
// }