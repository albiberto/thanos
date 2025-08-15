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
    
    public void CloneFrom(MemorySlot other) => throw new NotImplementedException("CloneFrom(MemorySlot) is not implemented yet.");
    
    public void CloneFrom(in Request request)
    {
        var nodePtr = (Node*)(slotPtr + _layout.Offsets.Node);
        PlacementNewNode(nodePtr);
        
        var fieldPtr = (WarField*)(slotPtr + _layout.Offsets.WarField);
        var bitboardsPtr = (ulong*)(slotPtr + _layout.Offsets.Bitboards);
        PlacementNewWarField(fieldPtr, bitboardsPtr, in _context, in _layout, in request.Board);
        
        var snakesPtr = slotPtr + _layout.Offsets.Snakes;
        PlacementNewWarSnakes(snakesPtr, fieldPtr, _context, _layout, in request.You,request.Board.Snakes);
        
        var arenaPtr = (WarArena*)(slotPtr + _layout.Offsets.WarArena);
        PlacementNewWarArena(arenaPtr, (WarSnake*)snakesPtr, fieldPtr, in request.Board);
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
        
        // 4. Chiama il metodo di inizializzazione di WarField, passando tutti i puntatori calcolati.
        WarField.PlacementNew(fieldPtr, in context, board.Food, board.Hazards, layout.Offsets.BitboardSegments, foodBitboardPtr, hazardBitboardPtr, snakesBitboardPtr);
    }
    
    // All'interno del MemorySlotBuilder

    private static WarSnake* PlacementNewWarSnakes(byte* slotPtr, WarField* fieldPtr, in WarContext context, in MemoryLayout layout, in Snake me, ReadOnlySpan<Snake> snakes)
    {
        var sizeOfHeader = MemoryLayout.SizesLayout.WarSnakeHeader;
        
        // Puntatore all'inizio del blocco di tutti i serpenti
        var snakesBlockPtr = slotPtr + layout.Offsets.Snakes;
        var capacity = layout.SnakeBodyCapacity;

        // --- 1. Inizializza il NOSTRO serpente ("You") in posizione 0 ---
        var mySnakePtr = (WarSnake*)snakesBlockPtr;
        var myBodyPtr = (ushort*)((byte*)mySnakePtr + sizeOfHeader);
        WarSnake.PlacementNew(mySnakePtr, myBodyPtr, fieldPtr, in me, me.Body, capacity);

        // --- 2. Inizializza tutti gli ALTRI serpenti nelle posizioni successive ---
        uint otherSnakesIndex = 1;
        foreach (ref readonly var snake in snakes)
        {
            if (snake.Id == me.Id) continue;

            // Calcola il puntatore al blocco del serpente corrente
            var currentSnakePtrBytes = snakesBlockPtr + otherSnakesIndex * layout.Sizes.SnakeStride;
        
            // Calcola i puntatori specifici per header e body
            var currentSnakeHeaderPtr = (WarSnake*)currentSnakePtrBytes;
            var currentSnakeBodyPtr = (ushort*)(currentSnakePtrBytes + sizeOfHeader);
        
            WarSnake.PlacementNew(currentSnakeHeaderPtr, currentSnakeBodyPtr, fieldPtr, in snake, snake.Body, capacity);

            otherSnakesIndex++;
        }

        return (WarSnake*)snakesBlockPtr;
    }
    
    // All'interno della tua ref struct MemorySlot

    private void PlacementNewWarArena(WarArena* ptr, WarSnake* snakesPtr, WarField* fieldPtr, in Board board)
    {
        // 1. Il conteggio dei serpenti, dal parametro `board`
        var liveSnakeCount = (uint)board.Snakes.Length;
    
        // 2. Lo stride dei serpenti, dal campo `_layout` di MemorySlot
        var snakeStride = _layout.Sizes.SnakeStride;

        // Chiamiamo il nuovo metodo PlacementNew con tutti i parametri corretti
        WarArena.PlacementNew(ptr, snakesPtr, fieldPtr, in _context, liveSnakeCount, snakeStride);
    }
}