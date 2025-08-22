using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.War.Snake;

namespace Thanos.War.Arena;

/// <summary>
///     Wrapper per l'array di serpenti che fornisce accesso indicizzato.
/// </summary>
public readonly ref struct WarSnakeArray(Span<byte> snakesMemory, int count, int stride)
{
    private readonly Span<byte> _snakesMemory = snakesMemory;
    public int Length { get; } = count;

    /// <summary>
    ///     Restituisce una "vista" WarSnake per il serpente all'indice specificato.
    /// </summary>
    public WarSnake this[int index]
    {
        get
        {
            var singleSnakeBlock = _snakesMemory.Slice(index * stride, stride);
            var headerSpan = singleSnakeBlock[..Unsafe.SizeOf<Health>()];
            var bodySpan = MemoryMarshal.Cast<byte, ushort>(singleSnakeBlock[Unsafe.SizeOf<Health>()..]);
            ref var profile = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Health>(headerSpan));
                
            // TODO: correggi offsets
            ref var anatomy = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, Anatomy>(headerSpan));

            // Chiama il costruttore "vista"
            return new WarSnake(ref profile, ref anatomy, bodySpan);
        }
    }
}