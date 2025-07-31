using System.Runtime.CompilerServices;

namespace Thanos.Parsers;

/// <summary>
/// Parser minimalista spinto al limite fisico
/// </summary>
public static unsafe class UltraFastParser
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Parse(ReadOnlySpan<byte> json, GameState* state)
    {
        // Reset
        state->SnakeCount = 0;
        state->FoodCount = 0;
        state->HazardCount = 0;

        var pos = 0;
        var len = json.Length;
        
        // Skip to first property
        while (pos < len && json[pos] != '"') pos++;
        
        // State
        byte snakeIndex = 0;
        ushort bodyOffset = 0;
        Snake* currentSnake = null;
        var inSnakes = false;
        var inFood = false;
        var inBody = false;
        byte pendingX = 0;
        var hasX = false;
        
        while (pos < len)
        {
            if (json[pos] == '"')
            {
                pos++;
                var start = pos;
                
                // Find end of property name
                while (pos < len && json[pos] != '"') pos++;
                var propLen = pos - start;
                
                // Skip quote and colon
                pos++;
                while (pos < len && (json[pos] <= 32 || json[pos] == ':')) pos++;
                
                // Check property by length and first char
                if (propLen == 1)
                {
                    if (json[start] == 'x')
                    {
                        pendingX = ParseByte(json, ref pos);
                        hasX = true;
                    }
                    else if (json[start] == 'y' && hasX)
                    {
                        var y = ParseByte(json, ref pos);
                        var coord = GridMath.ToIndex(pendingX, y, state->Width);
                        
                        if (inBody && currentSnake != null)
                        {
                            currentSnake->Body[currentSnake->Length++] = coord;
                            currentSnake->BodyHash ^= coord;
                        }
                        else if (inFood)
                        {
                            state->FoodPositions[state->FoodCount++] = coord;
                        }
                        else if (currentSnake != null)
                        {
                            currentSnake->Head = coord;
                        }
                        hasX = false;
                    }
                }
                else if (propLen == 4)
                {
                    if (json[start] == 't') // turn
                    {
                        state->Turn = ParseUShort(json, ref pos);
                    }
                    else if (json[start] == 'f') // food
                    {
                        inFood = true;
                    }
                    else if (json[start] == 'b') // body
                    {
                        inBody = true;
                    }
                }
                else if (propLen == 5)
                {
                    if (json[start] == 'w') // width
                    {
                        state->Width = ParseByte(json, ref pos);
                    }
                }
                else if (propLen == 6)
                {
                    if (json[start] == 'h')
                    {
                        if (json[start + 2] == 'i') // height
                        {
                            state->Height = ParseByte(json, ref pos);
                            state->TotalCells = (ushort)(state->Width * state->Height);
                        }
                        else if (currentSnake != null) // health
                        {
                            currentSnake->Health = ParseByte(json, ref pos);
                        }
                    }
                    else if (json[start] == 's') // snakes
                    {
                        inSnakes = true;
                    }
                }
            }
            else if (json[pos] == '{')
            {
                if (inSnakes && snakeIndex < 4)
                {
                    currentSnake = &state->Snakes[snakeIndex++];
                    currentSnake->Body = state->SnakeBodies + bodyOffset;
                    currentSnake->Length = 0;
                    currentSnake->BodyHash = 0;
                    state->SnakeCount = snakeIndex;
                }
                pos++;
            }
            else if (json[pos] == '}')
            {
                if (currentSnake != null)
                {
                    bodyOffset += currentSnake->Length;
                    currentSnake = null;
                }
                pos++;
            }
            else if (json[pos] == ']')
            {
                if (inBody) inBody = false;
                else if (inSnakes) inSnakes = false;
                else if (inFood) inFood = false;
                pos++;
            }
            else
            {
                pos++;
            }
        }
        
        state->YouIndex = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ParseByte(ReadOnlySpan<byte> json, ref int pos)
    {
        byte result = 0;
        while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
        {
            result = (byte)(result * 10 + (json[pos] - '0'));
            pos++;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ParseUShort(ReadOnlySpan<byte> json, ref int pos)
    {
        ushort result = 0;
        while (pos < json.Length && json[pos] >= '0' && json[pos] <= '9')
        {
            result = (ushort)(result * 10 + (json[pos] - '0'));
            pos++;
        }
        return result;
    }
}