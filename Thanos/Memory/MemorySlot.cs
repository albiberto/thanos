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

    // ┌─────────────── NODE (64B) ────────────────┐┌──── WAR FIELD HDR (64B) ─────┐┌═══════════════════ BITBOARDS ═════════════════════┐┌═══════════════════ SNAKES ═══════════════════┐┌──────────── WAR ARENA (64B) ────────────┐
    // │ Pointers, visits, etc.                    ││ Width, Height, bitboard ptrs ││ [Food (64B)] | [Hazard (64B)] | [All Snakes (64B)]││ [Snake0: Header+Body (64B)] | [Snake1: ...]  ││ Snakes ptrs, field ptrs, stats          │
    public MemorySlot(byte* slotPtr, in WarContext context, in MemoryLayout layout)
    {
        _slotPtr = slotPtr;
        _context = context;
        _layout = layout;
        
        var nodePtr = (Node*)(_slotPtr + _layout.Offsets.Node);
        PlacementNewNode(nodePtr);
        
        var fieldPtr = (WarField*)(_slotPtr + _layout.Offsets.WarField);
        var bitboardsPtr = (ulong*)(fieldPtr + layout.Offsets.Bitboards);
        PlacementNewWarField(fieldPtr, bitboardsPtr, _context, _layout);
        
        // var snakesPtr = _slotPtr + _layout.Offsets.Snakes;
        // PlacementNewWarSnakes(snakesPtr, _context, _layout);
    }
    
    private static void PlacementNewNode(Node* nodePtr) => Node.PlacementNew(nodePtr);

    private static WarField* PlacementNewWarField(WarField* fieldPtr, ulong* bitboardsPtr, in WarContext context, in MemoryLayout layout)
    {
        // 1. Calcola i puntatori specifici per ogni singolo bitboard.
        var segmentsPerBitboard = layout.Offsets.BitboardSegments;
    
        var foodBitboardPtr = bitboardsPtr;
        var hazardBitboardPtr = foodBitboardPtr + segmentsPerBitboard;
        var snakesBitboardPtr = hazardBitboardPtr + segmentsPerBitboard;

        // 2. Chiama il metodo di inizializzazione di WarField, passando tutti i puntatori calcolati.
        WarField.PlacementNew(fieldPtr, in context, layout.Offsets.BitboardSegments, foodBitboardPtr, hazardBitboardPtr, snakesBitboardPtr);

        // 3. Restituisce il puntatore all'header di WarField per poterlo usare in seguito (es. per WarArena).
        return fieldPtr;
    }

    private void PlacementNewWarSnakes(byte* ptr, in WarContext context)
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