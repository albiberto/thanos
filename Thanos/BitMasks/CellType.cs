using System.Runtime.CompilerServices;

namespace Thanos.BitMasks;

/// <summary>
/// Provides constants and utility methods for encoding and decoding cell types using bitmasks.
/// </summary>
public static class CellType
{
    /// <summary>
    /// Represents an empty cell.
    /// Bit 1: no left shift is needed, as it is the first bit, simply the value of bit 1 is 0.
    /// </summary>
    public const ushort Empty = 0;

    /// <summary>
    /// Represents a food cell.
    /// Bit 1: no left shift is needed, as it is the first bit, simply the value of bit 1 is 1.
    /// </summary>
    public const ushort Food = 1 << 0; 

    /// <summary>
    /// Represents a hazard cell.
    /// Bit 1: left shift bit 1 by 1 position
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimal 1) 0b0000_0000_0000_0001 << (left-shift)
    ///                                 1  =
    /// ------------------------------------
    /// (decimal 2) 0b0000_0000_0000_0010
    /// ]]>
    /// </example>
    public const ushort Hazard = 1 << 1;

    /// <summary>
    /// Represents the head of your snake.
    /// Bit 2: left shift bit 1 by 2 positions
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimal 1) 0b0000_0000_0000_0001 << (left-shift)
    ///                                 2  =
    /// ------------------------------------
    /// (decimal 4) 0b0000_0000_0000_0100
    /// ]]>
    /// </example>
    public const ushort MyHead = 1 << 2;

    /// <summary>
    /// Represents the body of your snake.
    /// Bit 3: left shift by 3 positions
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimal 1) 0b0000_0000_0000_0001 << (left-shift)
    ///                                 3  =
    /// ------------------------------------
    /// (decimal 8) 0b0000_0000_0000_1000
    /// ]]>
    /// </example>
    public const ushort MyBody = 1 << 3;

    /// <summary>
    /// bit 4: left shift by 4 positions
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimal 1) 0b0000_0000_0000_0001 << (left-shift)
    ///                                 4  =
    /// ------------------------------------
    /// (decimal 16) 0b0000_0000_0001_0000
    /// ]]>
    /// </example>
    public const ushort EnemyHead = 1 << 4;

    /// <summary>
    /// bit 5: left shift by 5 positions
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimal 1) 0b0000_0000_0000_0001 << (left-shift)
    ///                                 5  =
    /// ------------------------------------
    /// (decimal 32) 0b0000_0000_0010_0000
    /// ]]>
    /// </example>
    public const ushort EnemyBody = 1 << 5;

    /// <summary>
    /// Number of bits to left-shift to encode the enemy index
    /// </summary>
    private const ushort EnemyIndexShift = 6;
    
    /// <summary>
    /// Bitmask usata per estrarre l’indice del nemico (bit 6–8).
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// (decimale 448) (hex 0x1C0)      0b0000_0001_1100_0000 & (and)
    /// (decimale 32)                   0b0001_0011_1010_0000 =
    /// -------------------------------------------------------
    ///                                 0b0000_0001_1000_0000 
    /// ]]>
    /// </example>
    private const ushort EnemyIndexMask = 0x1C0;

    /// <summary>
    ///     Checks if the cell contains a part of your snake (head or body).
    /// </summary>
    /// <param name="cell">Cell value</param>
    /// <returns>True if it is MyHead or MyBody</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMySnake(ushort cell) => (cell & (MyHead | MyBody)) != 0;

    /// <summary>
    ///     Checks if the cell contains a part of an enemy snake (head or body).
    /// </summary>
    /// <param name="cell">Cell value</param>
    /// <returns>True if it is EnemyHead or EnemyBody</returns>
    /// <example>
    ///     (MyBody, decimal 8)                     0b0000_0000_0000_1000  & (and)
    ///     (EnemyHead | EnemyBody, decimal 48)     0b0000_0000_0011_0000  =
    ///     ----------------------------------------------------------------
    ///     (decimal 0)                             0b0000_0000_0000_0000 != 0 (FALSE)
    /// </example>
    /// <example>
    ///     (EnemyHead, decimal 16)                 0b0000_0000_0001_0000  & (and)
    ///     (EnemyHead | EnemyBody, decimal 48)     0b0000_0000_0011_0000  =
    ///     ----------------------------------------------------------------
    ///     (decimal 16)                            0b0000_0000_0001_0000 != 0 (TRUE)
    /// </example>
    /// <example>
    ///     (EnemyBody, decimal 32)                 0b0000_0000_0010_0000  & (and)
    ///     (EnemyHead | EnemyBody, decimal 48)     0b0000_0000_0011_0000  =
    ///     ------------------------------------------------------------------
    ///     (decimal 32)                            0b0000_0000_0010_0000 != 0 (TRUE)
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEnemySnake(ushort cell) => (cell & (EnemyHead | EnemyBody)) != 0;

    /// <summary>
    ///     Creates a cell for an enemy snake.
    ///     Encodes both the identifier (0–7) and whether it is a head or body.
    /// </summary>
    /// <param name="enemyIndex">Index of the enemy snake (0–7)</param>
    /// <param name="isHead">True for head, false for body</param>
    /// <returns>Cell value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort MakeEnemyCell(byte enemyIndex, bool isHead) => (ushort)(((enemyIndex << EnemyIndexShift) & EnemyIndexMask) | (isHead ? EnemyHead : EnemyBody));


    /// <summary>
    ///     Returns the enemy snake index (0–7).
    ///     Works only if <c>IsEnemySnake(cell)</c> returns true.
    ///     <example>
    ///         Step 1: (not strictly necessary, but future-proof for additional bits)
    ///             (Bit Count)                                   .... ..98 7654 3210                                  
    ///             (Cell, decimal 992)                         0b0000_1111_1100_0000 & (and)
    ///             (EnemyIndexMask, decimal 448, hex 0x1C0)    0b0000_0000_1110_0000 =
    ///             -------------------------------------------------------------------
    ///             (decimal 192)                               0b0000_0000_1100_0000 
    ///         Step 2:
    ///             (Cell, decimal 192)                         0b0000_0010_1100_0000 >> (right-shift)
    ///                                                                             6  =
    ///             --------------------------------------------------------------------
    ///             (decimale 3)                                0b0000_0000_0000_0011 
    ///     </example>
    /// </summary>
    /// <param name="cell">Cell value</param>
    /// <returns>Enemy snake index (0–7)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetEnemyIndex(ushort cell) => (byte)((cell & EnemyIndexMask) >> EnemyIndexShift);
}