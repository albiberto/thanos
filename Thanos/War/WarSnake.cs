using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.Enums;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarSnake
{
    // --- Header ---
    private uint _capacity;
    private int _nextHeadIndex;

    public int Health;
    public int Length;
    public ushort Head;
    public int TailIndex;
    
    // --- Body ---
    public ushort* Body;

    public static void PlacementNew(Span<byte> headerSpan, Span<ushort> bodySpan, in Snake snakeDto, in WarField field)
    {
        ref var snake = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<byte, WarSnake>(headerSpan));
        
        snake._capacity = (uint)bodySpan.Length;
        var sourceBody = snakeDto.Body.AsSpan();

        snake.Health = snakeDto.Health;
        snake.Length = System.Math.Min(snakeDto.Length, (int)snake._capacity);

        snake.TailIndex = 0;
        snake._nextHeadIndex = snake.Length & ((int)snake._capacity - 1);
        
        snake.Body = (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bodySpan));

        for (var i = 0; i < snake.Length; i++)
        {
            var coord1D = field.To1D(in sourceBody[snake.Length - 1 - i]);
            snake.Body[i] = coord1D;
            field.SetSnakeBit(coord1D);
        }

        snake.Head = snake.Length > 0 ? snake.Body[snake.Length - 1] : ushort.MaxValue;
    }

    /// <summary>
    /// Esegue il movimento del serpente. Questa è una funzione sull'hot-path
    /// e continua a usare l'accesso diretto ai puntatori per le massime performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 1)
    {
        Health = hasEaten ? 100 : Health - damage;
        if (Dead) return;

        var capacityMask = (int)_capacity - 1;

        // Il corpo è un buffer circolare. La vecchia testa prende il posto della "prossima testa".
        Body[_nextHeadIndex] = Head;
        Head = newHeadPosition;
        _nextHeadIndex = (_nextHeadIndex + 1) & capacityMask;

        if (hasEaten && Length < _capacity)
        {
            // Se il serpente mangia e non è alla massima capacità, cresce in lunghezza.
            Length++;
        }
        else
        {
            // Altrimenti, la coda avanza nel buffer circolare.
            TailIndex = (TailIndex + 1) & capacityMask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetTailPosition() => Body[TailIndex];

    public readonly bool Dead => Health <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Kill() => Health = 0;
}