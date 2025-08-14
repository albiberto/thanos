using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

/* INIZIO DELLO SLOT (allineato a 64B)
    +--------------------------+
    | NODE                     |
    | (puntatori, visite, ...) |
    | Size: 64B aligned        |
    +--------------------------+
    | WAR ARENA                |
    | (ptr snakes/field, stats)|
    | Size: 64B aligned        |
    +--------------------------+
    | WAR FIELD HEADER         |
    | (width, height, bitboards)
    | Size: 64B aligned       |
    +-------------------------+
    | SNAKES BLOCK            |
    | Size: allineato 64B     |
    |  +-------------------+  |
    |  | Snake0 Header 64B |  |
    |  +-------------------+  |
    |  | Snake0 Body + Pad |  |
    |  +-------------------+  |
    |  | Snake1 Header ... |  |
    |  | Snake1 Body + Pad |  |
    |  +-------------------+  |
    |          ...             |
    |  | SnakeN Header ... |   |
    |  | SnakeN Body + Pad |   |
    |  +-------------------+  |
    +-------------------------+
    | BITBOARDS BLOCK         |
    | Size: allineato 64B     |
    |  +-------------------+  |
    |  | Food Bitboard     |  |
    |  +-------------------+  |
    |  | Hazard Bitboard   |  |
    |  +-------------------+  |
    |  | AllSnakes Bitboard|  |
    |  +-------------------+  |
    +-------------------------+
    FINE DELLO SLOT
    */
public readonly unsafe ref struct MemorySlot
{
    private readonly byte* _slotPtr;
    private readonly WarContext _context;
    private readonly MemoryLayout _layout;

    // ┌─────────────── NODE (64B) ────────────────┐┌═══════════════════ SNAKES ═══════════════════┐┌═══════════════════ BITBOARDS ═════════════════════┐┌──── WAR FIELD HDR (64B) ─────┐┌──────────── WAR ARENA (64B) ────────────┐
    // │ Pointers, visits, etc.                    ││ [Snake0: Header+Body (64B)] | [Snake1: ...]  ││ [Food (64B)] | [Hazard (64B)] | [All Snakes (64B)]││ Width, Height, bitboard ptrs ││ Snakes ptrs, field ptrs, stats          │
    public MemorySlot(byte* slotPtr, in WarContext context, in MemoryLayout layout)
    {
        _slotPtr = slotPtr;
        _context = context;
        _layout = layout;
        
        var nodePtr = (Node*)(_slotPtr + _layout.Offsets.Node);
        PlacementNewNode(nodePtr);
        
        var snakesPtr = _slotPtr + _layout.Offsets.Snakes;
        PlacementNewWarSnakes(snakesPtr, _)
    }
    
    private static void PlacementNewNode(Node* ptr) => Node.PlacementNew(ptr);

    private void PlacementNewWarSnakes(byte* ptr, in WarContext context)
    {
        for (var i = 0; i < _context.SnakeCount; i++)
        {
            
        }
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

    private void PlacementNewWarSnakes(Span<byte> snakesBlockSpan, in Board board, WarField* fieldPtr)
    {
        for (var i = 0; i < _context.SnakeCount; i++)
        {
            
        }
    }

    private void PlacementNewWarArena(Span<byte> arenaSpan, WarSnake* snakesPtr, WarField* fieldPtr)
    {
        WarArena.PlacementNew(arenaSpan, in _context, snakesPtr, fieldPtr);
    }
}