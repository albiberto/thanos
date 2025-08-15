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
        var bitboardsPtr = (ulong*)(fieldPtr + _layout.Offsets.Bitboards);
        PlacementNewWarField(fieldPtr, bitboardsPtr, in _context, in _layout, in request.Board);
        
        var snakesPtr = slotPtr + _layout.Offsets.Snakes;
        PlacementNewWarSnakes(snakesPtr, fieldPtr, _context, _layout, in request.You,request.Board.Snakes);
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
    
    private static WarSnake* PlacementNewWarSnakes(byte* slotPtr, WarField* fieldPtr, in WarContext context, in MemoryLayout layout, in Snake me, ReadOnlySpan<Snake> snakes)
    {
        var snakesBlockPtr = slotPtr + layout.Offsets.Snakes;
        var snakesBlockSpan = new Span<byte>(snakesBlockPtr, (int)layout.Sizes.Snakes);

        // --- 1. Inizializza il NOSTRO serpente ("You") usando il parametro `me` ---
        var mySnakeBlock = snakesBlockSpan.Slice(0, (int)layout.Sizes.SnakeStride);
        var myHeaderSpan = mySnakeBlock[..(int)layout.Sizes.WarSnakeHeader];
        var myBodySpan = MemoryMarshal.Cast<byte, ushort>(mySnakeBlock[(int)layout.Sizes.WarSnakeHeader..]);
    
        WarSnake.PlacementNew(myHeaderSpan, myBodySpan, in me, in *fieldPtr);

        // --- 2. Inizializza gli ALTRI serpenti usando il parametro `snakes` ---
        uint otherSnakesIndex = 1;
    
        // Itera sullo `snakes` span passato come argomento
        foreach (ref readonly var snakeDto in snakes)
        {
            // Salta il nostro serpente, il cui ID è ora letto da `me.Id`
            if (snakeDto.Id == me.Id)
            {
                continue;
            }

            var snakeBlock = snakesBlockSpan.Slice((int)(otherSnakesIndex * layout.Sizes.SnakeStride), (int)layout.Sizes.SnakeStride);
            var headerSpan = snakeBlock[..(int)layout.Sizes.WarSnakeHeader];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(snakeBlock[(int)layout.Sizes.WarSnakeHeader..]);
        
            WarSnake.PlacementNew(headerSpan, bodySpan, in snakeDto, in *fieldPtr);
        
            otherSnakesIndex++;
        }
    
        return (WarSnake*)snakesBlockPtr;
    }

    private void PlacementNewWarArena(Span<byte> arenaSpan, WarSnake* snakesPtr, WarField* fieldPtr)
    {
        WarArena.PlacementNew(arenaSpan, in _context, snakesPtr, fieldPtr);
    }
}