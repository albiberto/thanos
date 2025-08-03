using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos;

/// <summary>
/// Rappresenta una griglia di gioco ad alte prestazioni, ottimizzata per la cache e le operazioni SIMD,
/// per la rilevazione delle collisioni. Gestisce il proprio buffer di memoria allineata e non gestita.
/// </summary>
public unsafe struct CollisionMatrix : IDisposable
{
    private byte* _grid;        // Puntatore alla memoria non gestita che rappresenta la griglia.
    private int _boardSize;     // Dimensione logica della griglia (width * height).
    private nuint _totalMemory; // Dimensione fisica della memoria allocata, con padding per SIMD.

    /// <summary>
    /// Ottiene il contenuto di una cella a una coordinata specifica della griglia (indice).
    /// </summary>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _grid[index];
    }

    /// <summary>
    /// Inizializza la CollisionMatrix, allocando memoria allineata.
    /// Dovrebbe essere chiamata una volta all'inizio del gioco.
    /// </summary>
    /// <param name="boardSize">Dimensioni della board</param>
    public void Initialize(int boardSize)
    {
        _boardSize = boardSize;

        // Per le operazioni SIMD, la dimensione della memoria deve essere un multiplo della dimensione del vettore.
        // Aggiungiamo un padding per raggiungere il successivo multiplo della dimensione del vettore.
        var vectorSize = Vector<byte>.Count;
        _totalMemory = (nuint)((_boardSize + vectorSize - 1) / vectorSize * vectorSize);

        // Alloca memoria allineata a 64 byte per prestazioni ottimali della cache e di SIMD.
        _grid = (byte*)NativeMemory.AlignedAlloc(_totalMemory, 64);
        if (_grid == null)
            throw new OutOfMemoryException($"Impossibile allocare {_totalMemory} byte per la CollisionMatrix");
    }

    /// <summary>
    /// Azzera l'intera griglia di gioco utilizzando il metodo intrinseco più efficiente.
    /// Questa è l'equivalente di una `memset` altamente ottimizzata.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_grid == null) return;

        // Unsafe.InitBlock viene sostituito dal compilatore JIT con le istruzioni macchina
        // più veloci possibili per l'azzeramento della memoria (es. REP STOSB su x86).
        Unsafe.InitBlock(_grid, 0, (uint)_totalMemory);
    }

    /// <summary>
    /// Proietta lo stato corrente di tutti i serpenti da un Battlefield su questa griglia.
    /// </summary>
    /// <param name="battlefield">Un puntatore al Battlefield che contiene i dati dei serpenti.</param>
    /// <param name="maxSnakes">Il numero massimo di serpenti da processare.</param>
    public void ProjectBattlefield(Battlefield* battlefield, int maxSnakes)
    {
        // Itera su tutti i possibili slot per i serpenti.
        for (var i = 0; i < maxSnakes; i++)
        {
            var snake = battlefield->GetSnake(i);
            if (snake == null || snake->Health <= 0) continue;

            // Usa l'indice del serpente + 1 come suo ID sulla griglia (0 è riservato per 'Vuoto').
            var snakeId = (byte)(i + 1);

            // "Disegna" il corpo del serpente sulla griglia.
            var bodyPtr = (ushort*)((byte*)snake + BattleSnake.HeaderSize);
            for (int j = 0; j < snake->Length; j++)
            {
                _grid[bodyPtr[j]] = snakeId;
            }
            
            // "Disegna" la testa sopra qualsiasi parte del corpo che potesse trovarsi lì.
            _grid[snake->Head] = snakeId;
        }
    }

    /// <summary>
    /// Libera la memoria non gestita utilizzata dalla griglia.
    /// Deve essere chiamata quando la CollisionMatrix non è più necessaria.
    /// </summary>
    public void Dispose()
    {
        if (_grid == null) return;
        
        NativeMemory.AlignedFree(_grid);
        _grid = null;
        _boardSize = 0;
        _totalMemory = 0;
    }
}