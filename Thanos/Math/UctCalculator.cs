using System.Runtime.CompilerServices;
using Thanos.MCST;

// Assicurati che il namespace per Node sia corretto

namespace Thanos.Math; // Creiamo un nuovo namespace per le utilità matematiche

public static unsafe class UctCalculator
{
    // --- Inizio Sezione Aritmetica a Virgola Fissa ---
    
    private const int FIXED_POINT_SHIFT = 16;
    private const long FIXED_POINT_SCALE = 1L << FIXED_POINT_SHIFT;
    private const long EXPLORATION_CONSTANT_FIXED = (long)(1.41 * FIXED_POINT_SCALE);

    private static readonly long[] _logLutFixed = new long[1_000_000];

    /// <summary>
    /// Inizializza la Look-Up Table per i logaritmi a virgola fissa.
    /// Questo costruttore viene chiamato automaticamente una sola volta all'avvio.
    /// </summary>
    static UctCalculator()
    {
        for (var i = 1; i < _logLutFixed.Length; i++) _logLutFixed[i] = (long)(System.Math.Log(i) * FIXED_POINT_SCALE);
    }
    
    /// <summary>
    /// Calcola il punteggio UCT per un nodo figlio usando aritmetica a virgola fissa.
    /// </summary>
    /// <param name="parentVisits">Il numero di visite del nodo genitore.</param>
    /// <param name="child">Il puntatore al nodo figlio da valutare.</param>
    /// <returns>Il punteggio UCT come intero a 64 bit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetScore(long parentVisits, Node* child)
    {
        // 1. Termine di Exploitation (Wins / Visits)
        var exploitationFixed = ((long)child->Wins << FIXED_POINT_SHIFT) / child->Visits;

        // 2. Termine di Exploration (sqrt(log(P)/V))
        var parentLogFixed = _logLutFixed[parentVisits];
        var termInsideSqrt = (parentLogFixed << FIXED_POINT_SHIFT) / child->Visits;
        var sqrtVal = IntegerSqrt(termInsideSqrt);
        var explorationFixed = (EXPLORATION_CONSTANT_FIXED * sqrtVal) >> FIXED_POINT_SHIFT;
        
        // 3. Punteggio finale
        return exploitationFixed + explorationFixed;
    }

    /// <summary>
    /// Calcola la radice quadrata di un numero a virgola fissa usando solo interi.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long IntegerSqrt(long n)
    {
        switch (n)
        {
            case < 0:
            case 0:
                return 0;
        }

        var root = n;
        var bit = (n > 0x3FFFFFFFFFFFFFFFL) ? (1L << 62) : (1L << 30);
        
        while (bit > root) bit >>= 2;
        
        for (var i = 0; i < 2; i++) { // Due iterazioni di Newton-Raphson
            var x_k = root;
            if (x_k == 0) continue;
            root = (x_k + (n / x_k)) >> 1;
        }

        return root;
    }
}