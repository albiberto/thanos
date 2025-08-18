using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(Span<byte> slot, in GameContext context)
{
    private readonly Span<byte> _slot = slot;
    private readonly GameContext _context = context;

    /// <summary>
    /// Inizializza questo slot di memoria con uno stato di gioco iniziale da una Request.
    /// </summary>
    public void CloneFrom(in Request request)
    {
        InitializeNode();
        var warField = InitializeWarField(in request.Board);
        InitializeWarSnakes(ref warField, in request.Board);
        InitializeArenaHeader(request.Board.SnakeCount); // NUOVO
        var arena = GetArena(in request.Board);
        arena.InitializeHash(); // <- Chiamata al nuovo metodo
    }

    /// <summary>
    /// Clona l'intero stato di gioco da un altro MemorySlot in questo.
    /// </summary>
    public void CloneFrom(in MemorySlot source) => source._slot.CopyTo(_slot);
    
    /// <summary>
    /// Restituisce un puntatore unsafe al Node all'inizio di questo slot.
    /// </summary>
    public unsafe Node* GetNodePtr() => (Node*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_slot));

    private void InitializeNode()
    {
        var nodeSpan = _slot.Slice(_context.Layout.NodeOffset, _context.Layout.NodeSize);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node(); // Inizializza a zero/default
    }

    private WarField InitializeWarField(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_context.Layout.BitboardsOffset, _context.Layout.BitboardsSize);
        bitboardsSpan.Clear();
        
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _context.Layout.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);
    
        return new WarField(_context.Width, _context.Height, _context.Area, food, hazards, snakes, board.Food, board.Hazards);
    }
    
    /// <summary>
/// Inizializza tutti i serpenti per un nuovo stato di gioco.
/// Questa è la funzione "orchestratore" che coordina WarField e WarSnake.
/// </summary>
private void InitializeWarSnakes(ref WarField field, in Board _)
{
    var snakesSpan = _slot.Slice(_context.Layout.SnakesOffset, _context.Layout.SnakesSize);
    
    // Buffer temporaneo riutilizzato per le coordinate 1D di ogni serpente
    scoped Span<ushort> body1D = stackalloc ushort[_context.Area];

    for (var i = 0; i < board.SnakeCount; i++)
    {
        ref readonly var initialSnake = ref board.Snakes[i];
        
        // --- FASE 1: Conversione Coordinate ---
        // Il chiamante (questo metodo) chiede a WarField di fare le conversioni.
        // Questo rispetta la separazione delle responsabilità.
        var actualLength = Math.Min(initialSnake.Length, body1D.Length);
        var snakeBody1D = body1D[..actualLength];
        for (int j = 0; j < actualLength; j++)
        {
            // Il corpo del serpente nell'API è in ordine inverso (testa->coda),
            // mentre il nostro buffer interno è in ordine di inserimento (coda->testa).
            var index = actualLength - 1 - j;
            snakeBody1D[j] = field.To1D(in initialSnake.Body[index]);
        }

        // --- FASE 2: Recupero Memoria ---
        // Prende il blocco di memoria per il serpente corrente.
        var singleSnakeBlock = snakesSpan.Slice(i * _context.Layout.SnakeStride, _context.Layout.SnakeStride);
        
        // **INIZIO PARTE COMPLETATA**
        // Separa il blocco in due parti: l'header...
        var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<WarSnakeHeader>()];
        // ...e il corpo (il resto del blocco).
        var bodyBytesSpan = singleSnakeBlock[Unsafe.SizeOf<WarSnakeHeader>()..];

        // Ottiene un riferimento modificabile all'header usando MemoryMarshal.
        ref var header = ref MemoryMarshal.GetReference(
            MemoryMarshal.Cast<byte, WarSnakeHeader>(headerSpan));
        
        // Converte lo span di byte del corpo in uno span di ushort.
        var bodySpan = MemoryMarshal.Cast<byte, ushort>(bodyBytesSpan);
        // **FINE PARTE COMPLETATA**

        // --- FASE 3: Creazione e Aggiornamento Stato ---
        // Chiama il nuovo costruttore di WarSnake, che ora è semplice e pulito.
        new WarSnake(ref header, bodySpan, in initialSnake, snakeBody1D);

        // Il chiamante (questo metodo) aggiorna la bitboard di WarField.
        foreach (var coord1D in snakeBody1D)
        {
            field.Snakes.Set(coord1D);
        }
    }
}
    
    /// <summary>
    /// Restituisce la "vista" WarField per i dati di questo slot.
    /// </summary>
    private WarField GetField()
    {
        var bitboardsSpan = _slot.Slice(_context.Layout.BitboardsOffset, _context.Layout.BitboardsSize);
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _context.Layout.BitboardStrideInUlongs;
        
        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        // ORA È SICURO: Passiamo i valori primitivi estratti da _context.
        return new WarField(_context.Width, _context.Height, _context.Area, food, hazards, snakes); 
    }

    /// <summary>
    /// Restituisce la "vista" WarArena per i dati di questo slot,
    /// agendo come una factory che nasconde i dettagli della memoria.
    /// </summary>
    public WarArena GetArena()
    {
        // 1. Estrae i componenti principali (invariato)
        ref var header = ref MemoryMarshal.GetReference(
            MemoryMarshal.Cast<byte, WarArenaHeader>(_slot.Slice(_context.Layout.WarArenaHeaderOffset, _context.Layout.WarArenaHeaderSize)));
        
        var field = GetField();
        var snakesMemory = _slot.Slice(_context.Layout.SnakesOffset, _context.Layout.SnakesSize);

        // 2. Prepara i buffer del workspace LEGGENDO DAL LAYOUT
        // Ottieni il blocco di memoria principale del workspace
        var workspaceMemory = _slot.Slice(_context.Layout.WorkspaceOffset, _context.Layout.WorkspaceSize);
        
        // Affetta il workspace usando le dimensioni e gli offset pre-calcolati dal layout
        var newHeadPositions = MemoryMarshal.Cast<byte, ushort>(
            workspaceMemory.Slice(_context.Layout.NewHeadPositionsWorkspaceOffset, _context.Layout.NewHeadPositionsSize));
        
        var hasEaten = MemoryMarshal.Cast<byte, bool>(
            workspaceMemory.Slice(_context.Layout.HasEatenWorkspaceOffset, _context.Layout.HasEatenSize));

        var isDead = MemoryMarshal.Cast<byte, bool>(
            workspaceMemory.Slice(_context.Layout.IsDeadWorkspaceOffset, _context.Layout.IsDeadSize));

        var oldTailPositions = MemoryMarshal.Cast<byte, ushort>(
            workspaceMemory.Slice(_context.Layout.OldTailPositionsWorkspaceOffset, _context.Layout.OldTailPositionsSize));

        // 3. Passa tutti i pezzi al costruttore di WarArena (invariato)
        return new WarArena(ref header, field, snakesMemory, newHeadPositions, hasEaten, isDead, oldTailPositions, _context.Layout.SnakeStride);
    }
    
    private void InitializeArenaHeader(int snakeCount)
    {
        var headerSpan = _slot.Slice(_context.Layout.WarArenaHeaderOffset, _context.Layout.WarArenaHeaderSize);
        ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(headerSpan));
        header.LiveSnakesCount = snakeCount;
    }
}