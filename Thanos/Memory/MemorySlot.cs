using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

public readonly unsafe ref struct MemorySlot
{
    private readonly byte* _slotPtr;
    private readonly WarContext _context;
    private readonly MemoryLayout _layout;

    public MemorySlot(byte* slotPtr, in WarContext context, in MemoryLayout layout)
    {
        _slotPtr = slotPtr;
        _context = context;
        _layout = layout;
    }

    public void Build(in Board board)
    {
        // 1. Inizializza il Node.
        PlacementNewNode();

        // 2. Ottieni gli "Span" che rappresentano le sezioni di memoria.
        var arenaSpan = GetArenaSpan();
        var fieldHeaderSpan = GetFieldHeaderSpan();
        var snakesBlockSpan = GetSnakesBlockSpan();
        var bitboardsBlockSpan = GetBitboardsBlockSpan();

        // 3. ORDINE CORRETTO: Crea prima il WarField, perché WarSnake ne ha bisogno.
        PlacementNewWarField(fieldHeaderSpan, bitboardsBlockSpan, in board);
        
        // Ottieni un puntatore al WarField appena creato.
        var fieldPtr = (WarField*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(fieldHeaderSpan));

        // 4. Ora crea i WarSnake, passando loro il puntatore al field e i dati della board.
        var snakesPtr = PlacementNewWarSnakes(snakesBlockSpan, in board, fieldPtr);
        
        // 5. Infine, crea la WarArena con i puntatori corretti.
        PlacementNewWarArena(arenaSpan, snakesPtr, fieldPtr);
    }

    // Metodi helper per ottenere gli Span
    private Span<byte> GetArenaSpan() => new(_slotPtr + _layout.Offsets.WarArena, (int)MemoryLayout.Size.WarArena);
    private Span<byte> GetFieldHeaderSpan() => new(_slotPtr + _layout.Offsets.WarField, (int)MemoryLayout.Size.WarFieldHeader);
    private Span<byte> GetSnakesBlockSpan() => new(_slotPtr + _layout.Offsets.Snakes, (int)_layout.Sizes.Snakes);
    private Span<ulong> GetBitboardsBlockSpan() => new((ulong*)(_slotPtr + _layout.Offsets.Bitboards), (int)(_layout.Sizes.Bitboards / sizeof(ulong)));


    private void PlacementNewNode()
    {
        var nodePtr = (Node*)(_slotPtr + _layout.Offsets.Node);
        Node.PlacementNew(nodePtr);
    }

    private void PlacementNewWarField(Span<byte> fieldHeaderSpan, Span<ulong> bitboardsBlockSpan, in Board board)
    {
        bitboardsBlockSpan.Clear();
        var segments = (int)_layout.Offsets.BitboardSegments;
        var foodBitboard = bitboardsBlockSpan[..segments];
        var hazardBitboard = bitboardsBlockSpan.Slice(segments, segments);
        var snakesBitboard = bitboardsBlockSpan.Slice(segments * 2, segments);
        WarField.PlacementNew(fieldHeaderSpan, foodBitboard, hazardBitboard, snakesBitboard, in _context, in board);
    }

    /// <summary>
    /// Metodo corretto. Ora accetta 'board' e 'fieldPtr' per inizializzare completamente
    /// i serpenti e restituisce un puntatore, non uno Span.
    /// </summary>
    private WarSnake* PlacementNewWarSnakes(Span<byte> snakesBlockSpan, in Board board, WarField* fieldPtr)
    {
        for (var i = 0; i < _context.SnakeCount; i++)
        {
            var singleSnakeBlock = snakesBlockSpan.Slice(i * (int)_layout.Sizes.SnakeStride, (int)_layout.Sizes.SnakeStride);
            
            var headerSpan = singleSnakeBlock[..(int)MemoryLayout.Size.WarSnakeHeader];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[(int)MemoryLayout.Size.WarSnakeHeader..]);
            
            // CORREZIONE: Passa tutti i parametri richiesti dal nuovo WarSnake.PlacementNew
            WarSnake.PlacementNew(headerSpan, bodySpan, in board.Snakes[i], in *fieldPtr);
        }

        // CORREZIONE: Restituisce un puntatore all'inizio del blocco. Non si può usare
        // uno Span<WarSnake> perché gli oggetti non sono contigui a causa dello stride.
        return (WarSnake*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(snakesBlockSpan));
    }

    private void PlacementNewWarArena(Span<byte> arenaSpan, WarSnake* snakesPtr, WarField* fieldPtr)
    {
        WarArena.PlacementNew(arenaSpan, in _context, snakesPtr, fieldPtr);
    }
}