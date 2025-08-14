using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Thanos.MCST;
using Thanos.SourceGen;
using Thanos.War;

namespace Thanos.Memory;

// ┌─────────────── NODE (64B) ────────────────┐┌──── WAR FIELD HDR (64B) ─────┐┌═══════════════════ BITBOARDS ═════════════════════┐┌═══════════════════ SNAKES ═══════════════════┐┌──────────── WAR ARENA (64B) ────────────┐
// │ Pointers, visits, etc.                    ││ Width, Height, bitboard ptrs ││ [Food (64B)] | [Hazard (64B)] | [All Snakes (64B)]││ [Snake0: Header+Body (64B)] | [Snake1: ...]  ││ Snakes ptrs, field ptrs, stats          │
public readonly unsafe ref struct MemorySlot(byte* slotPtr, in WarContext context, in MemoryLayout layout)
{
    private readonly WarContext _context = context;
    private readonly MemoryLayout _layout = layout;
    
    public void CloneFrom(in Request request)
    {
        var nodePtr = (Node*)(slotPtr + _layout.Offsets.Node);
        PlacementNewNode(nodePtr);
        
        var fieldPtr = (WarField*)(slotPtr + _layout.Offsets.WarField);
        var bitboardsPtr = (ulong*)(fieldPtr + _layout.Offsets.Bitboards);
        PlacementNewWarField(fieldPtr, bitboardsPtr, in _context, in _layout, in request.Board);
        
        // var snakesPtr = _slotPtr + _layout.Offsets.Snakes;
        // PlacementNewWarSnakes(snakesPtr, _context, _layout);
    }
    
    public void CloneFrom(MemorySlot other)
    {
        
    }
    
    private static void PlacementNewNode(Node* nodePtr) => Node.PlacementNew(nodePtr);

    private static void PlacementNewWarField(WarField* fieldPtr, ulong* bitboardsPtr, in WarContext context, in MemoryLayout layout, in Board board)
    {
        // 1. Calcola i puntatori specifici per ogni singolo bitboard.
        var segmentsPerBitboard = layout.Offsets.BitboardSegments;
    
        // 2. Pulisci l'INTERO blocco dei bitboard in un colpo solo 
        NativeMemory.Clear(bitboardsPtr, layout.Sizes.Bitboards);
        
        // 3. Ora calcola i puntatori ai singoli bitboard...
        var foodBitboardPtr = bitboardsPtr;
        var hazardBitboardPtr = foodBitboardPtr + segmentsPerBitboard;
        var snakesBitboardPtr = hazardBitboardPtr + segmentsPerBitboard;
        
        // 3. Chiama il metodo di inizializzazione di WarField, passando tutti i puntatori calcolati.
        WarField.PlacementNew(fieldPtr, in context, board.Food, board.Hazards, layout.Offsets.BitboardSegments, foodBitboardPtr, hazardBitboardPtr, snakesBitboardPtr);
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