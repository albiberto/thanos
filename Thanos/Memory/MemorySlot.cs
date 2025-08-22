using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;
using Thanos.War.Arena;
using Thanos.War.Grid;
using Thanos.War.Snake;

namespace Thanos.Memory;

public readonly ref struct MemorySlot(Span<byte> slot, in GameContext context, Dictionary<string, int> snakeIdMap)
{
    private readonly Span<byte> _slot = slot;
    private readonly GameContext _context = context;

    /// <summary>
    ///     --- PUNTO DI INGRESSO (ENTRY POINT) ---
    ///     Popola questo slot traducendo i dati da una Request del server.
    ///     Questo è l'UNICO metodo che dovrebbe dipendere da 'Request'.
    /// </summary>
    public void InitializeFromRequest(in Request request)
    {
        InitializeNode();
        var warField = InitializeWarFieldFromBoard( request.Board);
        InitializeWarSnakesFromBoard(ref warField,  request.Board, snakeIdMap);
        InitializeArenaHeader(request.Board.SnakeCount); // NUOVO

        var arena = GetArena();
        arena.InitializeHash();
    }

    /// <summary>
    ///     --- OPERAZIONE INTERNA ---
    ///     Clona l'intero stato di gioco da un altro MemorySlot.
    ///     Operazione interna, veloce e senza conoscenza della Request.
    /// </summary>
    public void CloneFrom(in MemorySlot source) => source._slot.CopyTo(_slot);

    // --- VISTE E ACCESSO ---

    /// <summary>
    ///     Restituisce un puntatore unsafe al Node all'inizio di questo slot.
    /// </summary>
    public unsafe Node* GetNodePtr() => (Node*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(_slot));

    /// <summary>
    ///     Restituisce la "vista" WarArena per i dati di questo slot,
    ///     agendo come una factory che nasconde i dettagli della memoria.
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
        var newHeadPositions = MemoryMarshal.Cast<byte, ushort>(workspaceMemory.Slice(_context.Layout.NewHeadPositionsWorkspaceOffset, _context.Layout.NewHeadPositionsSize));
        var hasEaten = MemoryMarshal.Cast<byte, bool>(workspaceMemory.Slice(_context.Layout.HasEatenWorkspaceOffset, _context.Layout.HasEatenSize));
        var isDead = MemoryMarshal.Cast<byte, bool>(workspaceMemory.Slice(_context.Layout.IsDeadWorkspaceOffset, _context.Layout.IsDeadSize));
        var oldTailPositions = MemoryMarshal.Cast<byte, ushort>(workspaceMemory.Slice(_context.Layout.OldTailPositionsWorkspaceOffset, _context.Layout.OldTailPositionsSize));

        // 3. Passa tutti i pezzi al costruttore di WarArena (invariato)
        return new WarArena(ref header, field, snakesMemory, _context.Layout.SnakeStride);
    }

    /// <summary>
    ///     Restituisce la "vista" WarField per i dati di questo slot.
    /// </summary>
    private WarGrid GetField()
    {
        var bitboardsSpan = _slot.Slice(_context.Layout.BitboardsOffset, _context.Layout.BitboardsSize);
        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _context.Layout.BitboardStride;

        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        // ORA È SICURO: Passiamo i valori primitivi estratti da _context.
        return new WarGrid(_context.Width, _context.Height, _context.Area, food, hazards, snakes);
    }

    // --- HELPERS PRIVATI PER L'INIZIALIZZAZIONE ---
    private void InitializeNode()
    {
        var nodeSpan = _slot.Slice(_context.Layout.NodeOffset, _context.Layout.NodeSize);
        ref var node = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Node>(nodeSpan));
        node = new Node(); // Inizializza a zero/default
    }

    private WarGrid InitializeWarFieldFromBoard(in Board board)
    {
        var bitboardsSpan = _slot.Slice(_context.Layout.BitboardsOffset, _context.Layout.BitboardsSize);
        bitboardsSpan.Clear();

        var bitboardsUlongSpan = MemoryMarshal.Cast<byte, ulong>(bitboardsSpan);
        var stride = _context.Layout.BitboardStride;

        var food = bitboardsUlongSpan[..stride];
        var hazards = bitboardsUlongSpan.Slice(stride, stride);
        var snakes = bitboardsUlongSpan.Slice(stride * 2, stride);

        return new WarGrid(_context.Width, _context.Height, _context.Area, food, hazards, snakes, board.Food, board.Hazards);
    }

    /// <summary>
    ///     Inizializza tutti i serpenti per un nuovo stato di gioco.
    ///     Questa è la funzione "orchestratore" che coordina WarField e WarSnake.
    /// </summary>
    private void InitializeWarSnakesFromBoard(ref WarGrid grid, in Board board, Dictionary<string, int> snakeIdMap)
    {
        var snakesSpan = _slot.Slice(_context.Layout.SnakesOffset, _context.Layout.SnakesSize);

        // PASSO 2: Noleggia il buffer dall'ArrayPool
        var body1DBuffer = ArrayPool<ushort>.Shared.Rent(_context.Area);

        // PASSO 3: Usa un blocco try...finally per garantire la restituzione
        try
        {
            // Ottieni uno Span dal buffer noleggiato per lavorare in sicurezza
            var body1D = body1DBuffer.AsSpan();

            for (var i = 0; i < board.SnakeCount; i++)
            {
                ref readonly var initialSnake = ref board.Snakes[i];

                // --- FASE 1, 2, 3 ---
                // Il resto del tuo codice qui dentro rimane IDENTICO
                var actualLength = Math.Min(initialSnake.Length, body1D.Length);
                var snakeBody1D = body1D[..actualLength];
                for (var j = 0; j < actualLength; j++)
                {
                    var index = actualLength - 1 - j;
                    snakeBody1D[j] = grid.To1D(in initialSnake.Body[index]);
                }

                var singleSnakeBlock = snakesSpan.Slice(i * _context.Layout.SnakeStride, _context.Layout.SnakeStride);
                var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<Health>()];
                var bodyBytesSpan = singleSnakeBlock[Unsafe.SizeOf<Health>()..];
                
                ref var profile = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(headerSpan));
                // TODO: correggi offsets
                ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(headerSpan));
                var body = MemoryMarshal.Cast<byte, ushort>(bodyBytesSpan);

                // Assegna l'ID intero all'header del serpente
                var id = snakeIdMap[initialSnake.Id];
                var health = initialSnake.Health;
                // TODO: verfica e passa il valore capacity corretto
                new WarSnake(ref profile, ref anatomy, body, id, health, snakeBody1D, 1);

                foreach (var coord1D in snakeBody1D) grid.Snakes.Set(coord1D);
            }
        }
        finally
        {
            // PASSO 4: Restituisci il buffer al pool
            ArrayPool<ushort>.Shared.Return(body1DBuffer);
        }
    }

    private void InitializeArenaHeader(int snakeCount)
    {
        var headerSpan = _slot.Slice(_context.Layout.WarArenaHeaderOffset, _context.Layout.WarArenaHeaderSize);
        ref var header = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarArenaHeader>(headerSpan));
        header.LiveSnakesCount = snakeCount;
    }
}