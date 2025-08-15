using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Thanos.SourceGen;

namespace Thanos.War;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct WarSnake
{
    // --- Header ---
    public int Health; // Lasciamo Health pubblica per un accesso facile.

    // Ora sono tutti privati! L'accesso esterno è impossibile.
    private uint _capacity;
    private uint _length;
    
    private ushort _head;
    
    private uint _nextHeadIndex;
    private uint _tailIndex;
    
    // --- Body ---
    private ushort* _body;

    /// <summary>
    /// Inizializza la struct in una data locazione di memoria e la popola con i dati di gioco.
    /// </summary>
    public static void PlacementNew(WarSnake* snakePtr, ushort* bodyPtr, in WarField* field, in Snake snake, ReadOnlySpan<Coordinate> body, uint capacity)
    {
        var lenght = (uint)System.Math.Min(snake.Length, capacity);
        
        // Assegna il puntatore grezzo
        snakePtr->_body = bodyPtr;
        
        // Inizializza header
        snakePtr->_capacity = capacity;
        snakePtr->_length = lenght;
        snakePtr->Health = snake.Health;
        snakePtr->_tailIndex = 0;
        snakePtr->_nextHeadIndex = lenght & (capacity - 1);

        // Popola il corpo usando lo Span sicuro che ci è stato passato
        for (var i = 0; i < lenght; i++)
        {
            var index = (int)(lenght - 1 - i); // Invertiamo l'ordine per il corpo
            ref readonly var coordinate = ref body[index];
            var coord1D = field->To1D(in coordinate);

            bodyPtr[i] = coord1D;
            field->SetSnakeBit(coord1D);
        }

        snakePtr->_head = lenght > 0
            ? bodyPtr[lenght - 1]
            : ushort.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Move(ushort newHeadPosition, bool hasEaten, int damage = 1)
    {
        Health = hasEaten ? 100 : Health - damage;
        
        if (Dead) return;
        
        var body = this.AsBody();
        
        body.PushHead(newHeadPosition);
        
        if (hasEaten) 
            body.IncrementLength(); 
        else 
            body.PopTail();
    }

    public readonly bool Dead => Health <= 0;
    
    public void Kill() => Health = 0;
    
    /// <summary>
    /// Un wrapper ref struct che fornisce un accesso sicuro e strutturato
    /// ai campi PRIVATI della WarSnake che lo contiene.
    /// </summary>
    public readonly ref struct WarSnakeBody(ref WarSnake snake)
    {
        private readonly ref WarSnake _snake = ref snake;

        // Le proprietà ora accedono ai campi privati di _snake
        public ushort Head => _snake._head;
        public ushort Tail => _snake._body[_snake._tailIndex];
        public uint Length => _snake._length;
        
        /// <summary>
        /// Restituisce due Span che, insieme, rappresentano il corpo del serpente in ordine logico.
        /// Non esegue alcuna copia di memoria. I risultati vengono scritti nei parametri 'out'.
        /// </summary>
        public void GetSpans(out Span<ushort> first, out Span<ushort> second)
        {
            var tailIndex = (int)_snake._tailIndex;
            var length = (int)_snake._length;
            var capacity = (int)_snake._capacity;
            var bodyPtr = _snake._body;

            if (length == 0)
            {
                first = Span<ushort>.Empty;
                second = Span<ushort>.Empty;
                return;
            }

            var headIndex = (tailIndex + length - 1) & (capacity - 1);

            if (tailIndex <= headIndex)
            {
                // Caso contiguo: il primo span contiene tutto, il secondo è vuoto.
                first = new Span<ushort>(bodyPtr + tailIndex, length);
                second = Span<ushort>.Empty;
            }
            else
            {
                // Caso "spezzato": riempiamo entrambi gli span.
                var firstLength = capacity - tailIndex;
                var secondLength = length - firstLength;

                first = new Span<ushort>(bodyPtr + tailIndex, firstLength);
                second = new Span<ushort>(bodyPtr, secondLength);
            }
        }
        
        // I metodi ora modificano i campi privati di _snake
        public void PushHead(ushort newHeadPosition)
        {
            _snake._body[_snake._nextHeadIndex] = _snake._head;
            _snake._head = newHeadPosition;
            _snake._nextHeadIndex = (_snake._nextHeadIndex + 1) & (_snake._capacity - 1);
        }

        public void PopTail() => _snake._tailIndex = (_snake._tailIndex + 1) & (_snake._capacity - 1);

        public void IncrementLength()
        {
            if (_snake._length < _snake._capacity) _snake._length++;
        }
    }
}

public static class WarSnakeExtensions
{
    /// <summary>
    /// Metodo di estensione che crea un wrapper WarSnakeBody per una data WarSnake.
    /// Sostituisce la proprietà Body per eliminare il warning di Rider.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WarSnake.WarSnakeBody AsBody(this ref WarSnake snake) => new(ref snake);
}