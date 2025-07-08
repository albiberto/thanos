using Thanos.Domain;
using Thanos.Model;

namespace Thanos.Tests.Support;

public static class Debug
{
    private const string UP_ICON = "⬆️";
    private const string DOWN_ICON = "⬇️";
    private const string LEFT_ICON = "⬅️";
    private const string RIGHT_ICON = "➡️";
    private const string MY_HEAD_ICON = "👽";
    private const string MY_BODY_ICON = "💲";
    private const string ENEMY_HEAD_ICON = "😈";
    private const string ENEMY_BODY_ICON = "⛔";
    private const string HAZARD_ICON = "💀";
    private const string EMPTY_ICON = "⬛";
    private const string KO = "❌";
    private const string OK = "✔️";
    private const string KO_SELF = "⭕";
    

    public static void PrintHeader()
    {
        Console.WriteLine("🎲🎲🎲 Combinazioni Mosse Valide 🎲🎲🎲");
        Console.WriteLine($" 1 => {UP_ICON} UP");
        Console.WriteLine($" 2 => {DOWN_ICON} DOWN");
        Console.WriteLine($" 3 => {UP_ICON}{DOWN_ICON} UP | DOWN");
        Console.WriteLine($" 4 => {LEFT_ICON} LEFT");
        Console.WriteLine($" 5 => {UP_ICON}{LEFT_ICON} UP | LEFT");
        Console.WriteLine($" 6 => {DOWN_ICON}{LEFT_ICON} DOWN | LEFT");
        Console.WriteLine($" 7 => {UP_ICON}{DOWN_ICON}{LEFT_ICON} UP | DOWN | LEFT");
        Console.WriteLine($" 8 => {RIGHT_ICON} RIGHT");
        Console.WriteLine($" 9 => {UP_ICON}{RIGHT_ICON} UP | RIGHT");
        Console.WriteLine($"10 => {DOWN_ICON}{RIGHT_ICON} DOWN | RIGHT");
        Console.WriteLine($"11 => {UP_ICON}{DOWN_ICON}{RIGHT_ICON} UP | DOWN | RIGHT");
        Console.WriteLine($"12 => {LEFT_ICON}{RIGHT_ICON} LEFT | RIGHT");
        Console.WriteLine($"13 => {UP_ICON}{LEFT_ICON}{RIGHT_ICON} UP | LEFT | RIGHT");
        Console.WriteLine($"14 => {DOWN_ICON}{LEFT_ICON}{RIGHT_ICON} DOWN | LEFT | RIGHT");
        Console.WriteLine($"15 => {UP_ICON}{DOWN_ICON}{LEFT_ICON}{RIGHT_ICON} ALL");
        Console.WriteLine();

        PrintLegenda();
    }

    /// <summary>
    ///     Stampa la griglia di gioco con tutti gli elementi visualizzati
    /// </summary>
    public static void PrintMap(uint width, uint height, Point[] myBody, Point[] hazards, Snake[] snakes, int expected, int scenario, string testName, string fileName, int id)
    {
        PrintCurrentScenario(width, height, snakes, hazards, scenario, testName, fileName);
        PrintGrid(width, height, myBody, hazards, snakes, expected, id);
    }

    private static void PrintCurrentScenario(uint width, uint height, Snake[] snakes, Point[] hazards, int scenario, string testName, string fileName)
    {
        Console.WriteLine();
        var center = $"🧪🧪🧪 {testName} 🧪🧪🧪";
        const string borderSymbol = "🧪";
        var borderCount = center.Length / borderSymbol.Length - 1;
        var border = string.Concat(Enumerable.Repeat(borderSymbol, borderCount));

        var hazardCount = hazards.Length;
        var hazardList = hazardCount > 0
            ? string.Join(", ", hazards.Select(h => $"({h.x},{h.y})"))
            : "NESSUNO";

        // Stampa
        Console.WriteLine(border);
        Console.WriteLine(center);
        Console.WriteLine(border);
        Console.WriteLine();
        Console.WriteLine($"🧩 Scenario: {scenario.ToString().PadLeft(2, '0')}");
        Console.WriteLine($"📄 File: {fileName}.json");
        Console.WriteLine($"🗺️ Griglia: {width}x{height}");
        Console.WriteLine($"💀 Hazards: {hazardList}");

        if (snakes.Length <= 0) return;

        Console.WriteLine("🐍 Serpenti:");
        foreach (var snake in snakes)
        {
            var isBetty = snake.id == "betty";
            var headIcon = isBetty ? MY_HEAD_ICON : ENEMY_HEAD_ICON;
            var bodyIcon = isBetty ? MY_BODY_ICON : ENEMY_BODY_ICON;

            // Costruisci la stringa delle icone
            var icons = headIcon + string.Concat(Enumerable.Repeat(bodyIcon, snake.body.Length - 1));
            // Costruisci la stringa delle coordinate
            var coords = string.Join(",", snake.body.Select(p => $"({p.x},{p.y})"));

            Console.WriteLine($"  - {icons} {coords}");
        }
    }

    public static void PrintLegenda()
    {
        Console.WriteLine("📖🗺️📖 Legenda 📖🗺️📖");

        Console.WriteLine($"{MY_HEAD_ICON} = Testa del tuo serpente");
        Console.WriteLine($"{MY_BODY_ICON} = Corpo del tuo serpente");
        Console.WriteLine($"{ENEMY_HEAD_ICON} = Testa serpente nemico");
        Console.WriteLine($"{ENEMY_BODY_ICON} = Corpo serpente nemico");
        Console.WriteLine($"{HAZARD_ICON} = Hazard");
        Console.WriteLine($"{EMPTY_ICON} = Spazio vuoto");
    }

private static void PrintMoveOptions(int expected, uint headX, uint headY, uint width, uint height, int id, Point[] myBody)
{
    Console.WriteLine($"🏁🏁🏁 Risultato Test: {id} 🏁🏁🏁");
    Console.WriteLine($"🎯🎯🎯 Expected: {expected}");

    var directions = new[]
    {
        new { Label = $"{UP_ICON} (1) UP",    DX =  0, DY = -1, Flag = MonteCarlo.UP },
        new { Label = $"{DOWN_ICON} (2) DOWN", DX =  0, DY =  1, Flag = MonteCarlo.DOWN },
        new { Label = $"{LEFT_ICON} (4)LEFT", DX = -1, DY =  0, Flag = MonteCarlo.LEFT },
        new { Label = $"{RIGHT_ICON} (8) RIGHT", DX =  1, DY =  0, Flag = MonteCarlo.RIGHT }
    };

    foreach (var dir in directions)
    {
        var targetX = (int)headX + dir.DX;
        var targetY = (int)headY + dir.DY;

        var inGrid = targetX >= 0 && targetX < (int)width && targetY >= 0 && targetY < (int)height;
        var isExpected = (expected & dir.Flag) != 0;
        var isSelf = myBody.Skip(1).Any(p => p.x == targetX && p.y == targetY);

        var positionStr = inGrid ? $"({targetX}, {targetY})" : $"{KO} FUORI GRIGLIA";

        if (!inGrid)
        {
            Console.WriteLine($"{KO} {dir.Label,-18} {KO} ({targetX}, {targetY}) FUORI GRIGLIA");
        }
        else if (isSelf)
        {
            Console.WriteLine($"{KO_SELF} {dir.Label,-18} {KO_SELF} {positionStr} TORNA SU SE STESSO");
        }
        else if (isExpected)
        {
            Console.WriteLine($"{OK} {dir.Label,-18} {OK} {positionStr}");
        }
        else
        {
            Console.WriteLine($"{KO} {dir.Label,-18} {KO} {positionStr} COLLISION");
        }
    }
}

    private static void PrintGrid(uint width, uint height, Point[] myBody, Point[] hazards, Snake[] snakes, int expected, int id)
    {
        var grid = new string[height, width];
    
        // Inizializza tutto vuoto
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            grid[y, x] = EMPTY_ICON;
    
        // Hazards
        foreach (var h in hazards)
            grid[h.y, h.x] = HAZARD_ICON;
    
        // Corpo nemici
        foreach (var s in snakes)
            for (var i = 0; i < s.body.Length; i++)
            {
                var p = s.body[i];
                if (i == 0)
                    grid[p.y, p.x] = ENEMY_HEAD_ICON;
                else
                    grid[p.y, p.x] = ENEMY_BODY_ICON;
            }
    
        // Corpo mio (sovrascrive nemici se overlap)
        for (var i = 0; i < myBody.Length; i++)
        {
            var p = myBody[i];
            if (i == 0)
                grid[p.y, p.x] = MY_HEAD_ICON;
            else
                grid[p.y, p.x] = MY_BODY_ICON;
        }
    
        // --- INSERIMENTO FRECCE E X ---
        var head = myBody[0];
        var directions = new[]
        {
            new { Icon = UP_ICON,    DX =  0, DY = -1, Flag = MonteCarlo.UP },
            new { Icon = DOWN_ICON,  DX =  0, DY =  1, Flag = MonteCarlo.DOWN },
            new { Icon = LEFT_ICON,  DX = -1, DY =  0, Flag = MonteCarlo.LEFT },
            new { Icon = RIGHT_ICON, DX =  1, DY =  0, Flag = MonteCarlo.RIGHT }
        };
    
        foreach (var dir in directions)
        {
            var tx = (int)head.x + dir.DX;
            var ty = (int)head.y + dir.DY;
            var inGrid = tx >= 0 && tx < (int)width && ty >= 0 && ty < (int)height;
            var isExpected = (expected & dir.Flag) != 0;

            if (!inGrid || grid[ty, tx] != EMPTY_ICON) continue;
            if (isExpected)
                grid[ty, tx] = dir.Icon;
            else
                grid[ty, tx] = KO;
        }
    
        // Stampa la griglia allineata
        Console.WriteLine("\n    " + string.Join("  ", Enumerable.Range(0, (int)width).Select(x => x.ToString("D2"))));
        for (var y = 0; y < height; y++)
        {
            Console.Write($"{y:D2} ");
            for (var x = 0; x < width; x++)
                Console.Write(grid[y, x] + "  ");
            Console.WriteLine();
        }
    
        Console.WriteLine();
    
        PrintMoveOptions(expected, myBody[0].x, myBody[0].y, width, height, id, myBody);
    }
}