using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public struct WarSnakeHeader
{
    public int Health;
    public uint Capacity;
    public uint Length;
    public ushort Head;
    public uint NextHeadIndex;
    public uint TailIndex;
}

public ref struct WarSnake
{
    // Riferimenti ai dati, non i dati stessi
    private ref WarSnakeHeader _header;
    private readonly Span<ushort> _body;

    // Il costruttore assembla la "vista"
    public WarSnake(ref WarSnakeHeader header, Span<ushort> body)
    {
        _header = ref header;
        _body = body;
    }

    // --- PROPRIETÀ AD ACCESSO DIRETTO ---
    public readonly int Health { get => _header.Health; private set => _header.Health = value; }
    public readonly ushort Head => _header.Head;
    public readonly ushort Tail => _body[(int)_header.TailIndex];
    public readonly uint Length => _header.Length;
    public readonly bool Dead => Health <= 0;
    
    // --- METODI PRINCIPALI ---
    public void Kill() => Health = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 1)
    {
        Health = hasEaten ? 100 : Health - damage;
        if (Dead) return;
        
        PushHead(newHeadPosition);
        
        if (hasEaten) 
            IncrementLength(); 
        else 
            PopTail();
    }
    
    public readonly void GetSpans(out Span<ushort> first, out Span<ushort> second)
    {
        var tailIndex = (int)_header.TailIndex;
        var length = (int)_header.Length;
        var capacity = (int)_header.Capacity;

        if (length == 0)
        {
            first = Span<ushort>.Empty;
            second = Span<ushort>.Empty;
            return;
        }

        var headIndex = (tailIndex + length - 1) & (capacity - 1);

        if (tailIndex <= headIndex)
        {
            first = _body.Slice(tailIndex, length);
            second = Span<ushort>.Empty;
        }
        else
        {
            var firstLength = capacity - tailIndex;
            first = _body.Slice(tailIndex, firstLength);
            second = _body[..(length - firstLength)];
        }
    }
    
    // --- METODI PRIVATI DI LOGICA INTERNA ---
    private void PushHead(ushort newHeadPosition)
    {
        _body[(int)_header.NextHeadIndex] = _header.Head;
        _header.Head = newHeadPosition;
        _header.NextHeadIndex = (_header.NextHeadIndex + 1) & (_header.Capacity - 1);
    }

    private void PopTail() => _header.TailIndex = (_header.TailIndex + 1) & (_header.Capacity - 1);

    private void IncrementLength()
    {
        if (_header.Length < _header.Capacity) _header.Length++;
    }
}