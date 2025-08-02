# Cache Optimization and Memory Layout Analysis of `BattleSnake` and `Battlefield`

## Cache Line Management in `BattleSnake`

### `BattleSnake` Struct Layout

- **64-byte Header**  
  The `BattleSnake` struct places all critical fields—`Health`, `Length`, and `Head`—within the first 64 bytes (Cache Line 1).

- **Aligned Body Start**  
  The `Body` array starts at offset 64, exactly at the start of Cache Line 2.

```mermaid
flowchart TD
    subgraph Cache_Line_1 [Cache Line 1 (0–63 bytes)]
        A1[Health (int)]
        A2[Length (int)]
        A3[Head (ushort)]
        A4[Padding (54 bytes)]
    end

    subgraph Cache_Line_2 [Cache Line 2 (64–127 bytes)]
        B1[Body[0]]
        B2[Body[1]]
        B3[...]
    end
```

---

## Cache Line Management in `Battlefield`

- **128-byte Total Size**  
  Two cache lines:
  - First for configuration (`_boardWidth`, `_memory`, etc.)
  - Second for `_snakePointers` (used in loops)

```mermaid
flowchart TD
    subgraph Battlefield_Cache
        subgraph Line1 [Cache Line 1 (0–63 bytes)]
            C1[_boardWidth]
            C2[_boardHeight]
            C3[_turn]
            C4[_memory (pointer)]
        end
        subgraph Line2 [Cache Line 2 (64–127 bytes)]
            D1[_snakePointers[0]]
            D2[_snakePointers[1]]
            D3[...]
        end
    end
```

---

## Overall Memory Layout

### What the Code Actually Does

```mermaid
flowchart TD
    Battlefield[Battlefield Struct (128B)]
    MemoryBlock[Memory Block for Snakes]
    Snake0[Snake0 Header + Body]
    Snake1[Snake1 Header + Body]
    Snake2[...]
    Snake7[Snake7 Header + Body]

    Battlefield -->|_memory| MemoryBlock
    MemoryBlock --> Snake0
    MemoryBlock --> Snake1
    MemoryBlock --> Snake2
    MemoryBlock --> Snake7
```

---

## Summary

The current memory layout:

- Uses proper alignment to cache lines
- Separates configuration and runtime-accessed data
- Ensures each snake is isolated on its own cache line
- Follows idiomatic and safe C# memory patterns
