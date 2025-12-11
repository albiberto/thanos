using Thanos.Abstract;
using Thanos.SourceGen;

namespace Thanos;

public sealed class BattleSnakeAgent(IBattleSnakeCluster cluster) : IBattleSnakeAgent
{
    private readonly IBattleSnakeCluster _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
    
    // Buffer riutilizzabile per gli ID ordinati (0=Hero, 1..N=Enemies)
    private readonly string[] _idBuffer = new string[Constants.MaxSnakesCount];

    public void Start(in Request request)
    {
        var myId = request.You.Id;

        // 1. Hero sempre all'indice 0
        _idBuffer[0] = myId;

        // 2. Copia dei nemici partendo dall'indice 1
        var enemiesCount = 0;
        foreach (var snake in request.Board.Snakes)
        {
            // Saltiamo noi stessi
            if (string.Equals(snake.Id, myId, StringComparison.Ordinal)) continue;

            // Safety check per non sforare il buffer (se per assurdo arrivano troppi snake)
            if (1 + enemiesCount >= _idBuffer.Length) break;

            _idBuffer[1 + enemiesCount] = snake.Id;
            enemiesCount++;
        }

        // 3. Pulizia opzionale degli slot rimanenti (per evitare ID vecchi)
        var totalCount = 1 + enemiesCount;
        if (totalCount < _idBuffer.Length)
        {
            Array.Clear(_idBuffer, totalCount, _idBuffer.Length - totalCount);
        }

        // 4. Inizializzazione Cluster
        // Nota: Non passiamo più 'totalCount' perché i Pool sono configurati staticamente.
        // Passiamo l'intero buffer o uno slice? L'API attuale accetta string[].
        // Se l'Engine usa orderedIds.Length nei cicli, dobbiamo passare l'array della dimensione esatta o usare uno Span/Slice.
        // Dato che InitializeFromRequest usa 'orderedIds.Length' nel loop di ricerca, 
        // è MEGLIO passare solo la parte valida dell'array per evitare di iterare su slot vuoti/null.
        
        // Creiamo un array della dimensione esatta per questo match. 
        // Allocazione piccola (array di string references) fatta una volta per partita.
        var activeIds = new string[totalCount];
        Array.Copy(_idBuffer, activeIds, totalCount);

        _cluster.InitializeGame(activeIds);
        
        // _cluster.Reset() è già chiamato implicitamente dentro InitializeGame -> Engine.InitializeGame
    }

    public Task<byte> Move(Request request) => _cluster.ComputeMoveAsync(request);

    public void End(in Request _)
    {
        // Pulizia finale opzionale, ma InitializeGame gestisce già il reset al prossimo Start.
    }

    public void Dispose() => _cluster.Dispose();
}