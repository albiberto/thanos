using System.Runtime.CompilerServices;
using System.Text;

namespace Thanos.Parsers;

/// <summary>
/// Parser JSON ottimizzato che sfrutta il JIT di .NET
/// Filosofia: codice semplice che il JIT può ottimizzare aggressivamente
/// </summary>
public static unsafe class BattlesnakeParser
{
    // Costanti per il response buffer
    private static readonly string[] _directions = { "up", "down", "left", "right" };

    /// <summary>
    /// Parser diretto ottimizzato per il JIT
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ParseDirect(ReadOnlySpan<byte> data, GameState* state)
    {
        // Reset stato - il JIT può vettorizzare questo
        state->SnakeCount = 0;
        state->FoodCount = 0;
        state->HazardCount = 0;

        // State machine semplice che il JIT può ottimizzare
        var parser = new JsonParser(data, state);
        parser.Parse();
    }

    /// <summary>
    /// Struct per evitare allocazioni e aiutare il JIT con inlining
    /// </summary>
    private ref struct JsonParser
    {
        private readonly ReadOnlySpan<byte> _data;
        private readonly GameState* _state;
        private int _pos;
        private int _depth;
        private byte _snakeIndex;
        private ushort _bodyOffset;
        private Snake* _currentSnake;
        private byte _pendingX;
        private bool _hasX;
        
        // Flags come campi bool - il JIT li ottimizza meglio di un bitmask
        private bool _inBoard;
        private bool _inSnakes;
        private bool _inFood;
        private bool _inHazards;
        private bool _inBody;
        private bool _inYou;
        
        // Buffer per youId
        private Span<byte> _youId;
        private int _youIdLen;

        public JsonParser(ReadOnlySpan<byte> data, GameState* state)
        {
            _data = data;
            _state = state;
            _pos = 0;
            _depth = 0;
            _snakeIndex = 0;
            _bodyOffset = 0;
            _currentSnake = null;
            _pendingX = 0;
            _hasX = false;
            _inBoard = false;
            _inSnakes = false;
            _inFood = false;
            _inHazards = false;
            _inBody = false;
            _inYou = false;
            _youId = stackalloc byte[36];
            _youIdLen = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Parse()
        {
            while (_pos < _data.Length)
            {
                var c = _data[_pos];
                
                // Il JIT ottimizza meglio switch semplici
                switch (c)
                {
                    case (byte)' ':
                    case (byte)'\r':
                    case (byte)'\n':
                    case (byte)'\t':
                        _pos++;
                        break;
                        
                    case (byte)'{':
                        HandleOpenBrace();
                        break;
                        
                    case (byte)'}':
                        HandleCloseBrace();
                        break;
                        
                    case (byte)'[':
                        _pos++;
                        break;
                        
                    case (byte)']':
                        HandleCloseArray();
                        break;
                        
                    case (byte)'"':
                        HandleString();
                        break;
                        
                    case (byte)',':
                    case (byte)':':
                        _pos++;
                        break;
                        
                    default:
                        if (c >= '0' && c <= '9')
                            HandleNumber();
                        else
                            _pos++;
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleOpenBrace()
        {
            _depth++;
            if (_inSnakes && _depth == 3 && _snakeIndex < 4)
            {
                _currentSnake = &_state->Snakes[_snakeIndex++];
                _currentSnake->Body = _state->SnakeBodies + _bodyOffset;
                _currentSnake->Length = 0;
                _currentSnake->BodyHash = 0;
                _state->SnakeCount = _snakeIndex;
            }
            _pos++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleCloseBrace()
        {
            _depth--;
            if (_currentSnake != null && _depth == 2)
            {
                _bodyOffset += _currentSnake->Length;
                _currentSnake = null;
            }
            _pos++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleCloseArray()
        {
            // Reset flags - il JIT può ottimizzare questi branch
            if (_inBody) _inBody = false;
            else if (_inSnakes) _inSnakes = false;
            else if (_inFood) _inFood = false;
            else if (_inHazards) _inHazards = false;
            _pos++;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void HandleString()
        {
            _pos++; // Skip opening quote
            var start = _pos;
            
            // Trova la fine della stringa
            while (_pos < _data.Length && _data[_pos] != '"') 
                _pos++;
                
            var span = _data.Slice(start, _pos - start);
            _pos++; // Skip closing quote
            
            // Skip whitespace e ':'
            while (_pos < _data.Length && (_data[_pos] <= 32 || _data[_pos] == ':'))
                _pos++;
            
            ProcessProperty(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ProcessProperty(ReadOnlySpan<byte> prop)
        {
            // Pattern matching ottimizzato per lunghezza
            // Il JIT può ottimizzare questi switch molto bene
            switch (prop.Length)
            {
                case 1:
                    if (prop[0] == 'x')
                    {
                        _pendingX = (byte)ParseNumber();
                        _hasX = true;
                    }
                    else if (prop[0] == 'y' && _hasX)
                    {
                        var y = (byte)ParseNumber();
                        var pos = GridMath.ToIndex(_pendingX, y, _state->Width);
                        ProcessCoordinate(pos);
                        _hasX = false;
                    }
                    break;
                    
                case 2:
                    if (prop[0] == 'i' && prop[1] == 'd') // "id"
                    {
                        ProcessId();
                    }
                    break;
                    
                case 3:
                    if (prop.SequenceEqual("you"u8))
                    {
                        _inYou = true;
                    }
                    break;
                    
                case 4:
                    ProcessLength4Property(prop);
                    break;
                    
                case 5:
                    if (prop.SequenceEqual("board"u8))
                    {
                        _inBoard = true;
                    }
                    else if (prop.SequenceEqual("width"u8))
                    {
                        _state->Width = (byte)ParseNumber();
                    }
                    break;
                    
                case 6:
                    ProcessLength6Property(prop);
                    break;
                    
                case 7:
                    if (prop.SequenceEqual("hazards"u8))
                    {
                        _inHazards = true;
                    }
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessLength4Property(ReadOnlySpan<byte> prop)
        {
            // Il JIT ottimizza bene questi confronti sequenziali
            if (prop.SequenceEqual("turn"u8))
            {
                _state->Turn = (ushort)ParseNumber();
            }
            else if (prop.SequenceEqual("food"u8))
            {
                _inFood = true;
            }
            else if (prop.SequenceEqual("head"u8))
            {
                // Head coordinates will follow
            }
            else if (prop.SequenceEqual("body"u8))
            {
                _inBody = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessLength6Property(ReadOnlySpan<byte> prop)
        {
            if (prop.SequenceEqual("height"u8))
            {
                _state->Height = (byte)ParseNumber();
                _state->TotalCells = (ushort)(_state->Width * _state->Height);
            }
            else if (prop.SequenceEqual("health"u8) && _currentSnake != null)
            {
                _currentSnake->Health = (byte)ParseNumber();
            }
            else if (prop.SequenceEqual("snakes"u8))
            {
                _inSnakes = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessId()
        {
            if (_inYou)
            {
                SkipToString();
                var start = _pos;
                while (_pos < _data.Length && _data[_pos] != '"') _pos++;
                
                _youIdLen = _pos - start;
                if (_youIdLen <= _youId.Length)
                {
                    _data.Slice(start, _youIdLen).CopyTo(_youId);
                }
                _pos++; // Skip closing quote
            }
            else if (_currentSnake != null)
            {
                SkipStringValue();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessCoordinate(ushort position)
        {
            if (_inBody && _currentSnake != null)
            {
                _currentSnake->Body[_currentSnake->Length++] = position;
                _currentSnake->BodyHash ^= (uint)position;
            }
            else if (_inFood && _state->FoodCount < 200)
            {
                _state->FoodPositions[_state->FoodCount++] = position;
            }
            else if (_inHazards && _state->HazardCount < 255)
            {
                _state->HazardPositions[_state->HazardCount++] = position;
            }
            else if (_currentSnake != null && !_inBody && !_inFood && !_inHazards)
            {
                _currentSnake->Head = position;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ParseNumber()
        {
            var result = 0;
            
            // Loop semplice che il JIT può vettorizzare
            while (_pos < _data.Length)
            {
                var c = _data[_pos];
                if (c >= '0' && c <= '9')
                {
                    result = result * 10 + (c - '0');
                    _pos++;
                }
                else
                {
                    break;
                }
            }
            
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleNumber()
        {
            // Se siamo qui, sappiamo che c'è un numero
            var value = ParseNumber();
            
            // Gestione del valore in base al contesto
            if (_hasX)
            {
                // Abbiamo già X, questo deve essere Y
                var pos = GridMath.ToIndex(_pendingX, (byte)value, _state->Width);
                ProcessCoordinate(pos);
                _hasX = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipToString()
        {
            while (_pos < _data.Length && _data[_pos] != '"') _pos++;
            if (_pos < _data.Length) _pos++; // Skip opening quote
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipStringValue()
        {
            SkipToString();
            while (_pos < _data.Length && _data[_pos] != '"') _pos++;
            if (_pos < _data.Length) _pos++; // Skip closing quote
        }
    }

    /// <summary>
    /// Genera risposta ottimizzata
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetMoveResponse(Direction dir)
    {
        return $"{{\"move\":\"{_directions[(int)dir]}\"}}";
    }

    /// <summary>
    /// Scrive direttamente nel buffer di output
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteMoveResponse(Span<byte> output, Direction dir)
    {
        // Il JIT può ottimizzare questa sequenza di copie
        var start = "{\"move\":\""u8;
        start.CopyTo(output);
        
        var direction = Encoding.UTF8.GetBytes(_directions[(int)dir]);
        direction.CopyTo(output.Slice(start.Length));
        
        var end = "\"}"u8;
        end.CopyTo(output.Slice(start.Length + direction.Length));
        
        return start.Length + direction.Length + end.Length;
    }
}