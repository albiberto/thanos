using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace BattlesnakeGenerators
{
    /// <summary>
    /// Source Generator per deserializzazione JSON Battlesnake al limite hardware
    /// </summary>
    [Generator]
    public class BattlesnakeJsonGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var source = GenerateJsonParser();
            context.AddSource("BattlesnakeJsonParser.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private string GenerateJsonParser()
        {
            return @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Buffers;

namespace BattlesnakeOptimized
{
    /// <summary>
    /// Parser JSON generato per massime performance
    /// </summary>
    public static unsafe class BattlesnakeJsonParser
    {
        // Costanti per parsing veloce
        private static readonly byte[] TURN_BYTES = ""turn""u8.ToArray();
        private static readonly byte[] BOARD_BYTES = ""board""u8.ToArray();
        private static readonly byte[] WIDTH_BYTES = ""width""u8.ToArray();
        private static readonly byte[] HEIGHT_BYTES = ""height""u8.ToArray();
        private static readonly byte[] SNAKES_BYTES = ""snakes""u8.ToArray();
        private static readonly byte[] FOOD_BYTES = ""food""u8.ToArray();
        private static readonly byte[] HAZARDS_BYTES = ""hazards""u8.ToArray();
        private static readonly byte[] ID_BYTES = ""id""u8.ToArray();
        private static readonly byte[] HEAD_BYTES = ""head""u8.ToArray();
        private static readonly byte[] BODY_BYTES = ""body""u8.ToArray();
        private static readonly byte[] HEALTH_BYTES = ""health""u8.ToArray();
        private static readonly byte[] X_BYTES = ""x""u8.ToArray();
        private static readonly byte[] Y_BYTES = ""y""u8.ToArray();
        private static readonly byte[] YOU_BYTES = ""you""u8.ToArray();
        
        // Buffer pool per evitare allocazioni
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
        
        /// <summary>
        /// Parsing ottimizzato con Utf8JsonReader per zero allocazioni
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void ParseIntoGameState(ReadOnlySpan<byte> jsonData, GameState* state)
        {
            var reader = new Utf8JsonReader(jsonData, new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = false
            });
            
            // Pre-alloca buffer per ID comparison
            Span<byte> youIdBuffer = stackalloc byte[64];
            int youIdLength = 0;
            
            // Variabili di stato per parsing
            ushort bodyOffset = 0;
            byte snakeIndex = 0;
            byte foodIndex = 0;
            byte hazardIndex = 0;
            
            // Parsing state machine
            int depth = 0;
            bool inBoard = false;
            bool inSnakes = false;
            bool inFood = false;
            bool inHazards = false;
            bool inYou = false;
            bool inBody = false;
            bool inHead = false;
            
            Snake* currentSnake = null;
            int currentBodyIndex = 0;
            
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        ReadOnlySpan<byte> propertyName = reader.ValueSpan;
                        
                        // Usa confronti diretti di byte per velocità
                        if (propertyName.SequenceEqual(TURN_BYTES))
                        {
                            reader.Read();
                            state->Turn = (ushort)reader.GetInt32();
                        }
                        else if (propertyName.SequenceEqual(BOARD_BYTES))
                        {
                            inBoard = true;
                        }
                        else if (inBoard && propertyName.SequenceEqual(WIDTH_BYTES))
                        {
                            reader.Read();
                            if (state->Width == 0)
                            {
                                var width = (byte)reader.GetInt32();
                                reader.Read(); // Skip to height property
                                reader.Read(); // Get height value
                                var height = (byte)reader.GetInt32();
                                GameManager.Initialize(width, height);
                            }
                        }
                        else if (inBoard && propertyName.SequenceEqual(SNAKES_BYTES))
                        {
                            inSnakes = true;
                            state->SnakeCount = 0;
                        }
                        else if (inBoard && propertyName.SequenceEqual(FOOD_BYTES))
                        {
                            inFood = true;
                            state->FoodCount = 0;
                        }
                        else if (inBoard && propertyName.SequenceEqual(HAZARDS_BYTES))
                        {
                            inHazards = true;
                            state->HazardCount = 0;
                        }
                        else if (propertyName.SequenceEqual(YOU_BYTES))
                        {
                            inYou = true;
                        }
                        else if (inYou && propertyName.SequenceEqual(ID_BYTES))
                        {
                            reader.Read();
                            var idSpan = reader.ValueSpan;
                            idSpan.CopyTo(youIdBuffer);
                            youIdLength = idSpan.Length;
                        }
                        else if (currentSnake != null)
                        {
                            if (propertyName.SequenceEqual(ID_BYTES))
                            {
                                reader.Read();
                                // Confronta con you ID
                                if (reader.ValueSpan.SequenceEqual(youIdBuffer.Slice(0, youIdLength)))
                                {
                                    state->YouIndex = (byte)(snakeIndex - 1);
                                }
                            }
                            else if (propertyName.SequenceEqual(HEAD_BYTES))
                            {
                                inHead = true;
                            }
                            else if (propertyName.SequenceEqual(HEALTH_BYTES))
                            {
                                reader.Read();
                                currentSnake->Health = (byte)reader.GetInt32();
                            }
                            else if (propertyName.SequenceEqual(BODY_BYTES))
                            {
                                inBody = true;
                                currentBodyIndex = 0;
                                currentSnake->Length = 0;
                            }
                            else if ((inHead || inBody) && propertyName.SequenceEqual(X_BYTES))
                            {
                                reader.Read();
                                int x = reader.GetInt32();
                                reader.Read(); // Skip Y property name
                                reader.Read(); // Get Y value
                                int y = reader.GetInt32();
                                
                                ushort pos = GridMath.ToIndex(x, y, state->Width);
                                
                                if (inHead)
                                {
                                    currentSnake->Head = pos;
                                    inHead = false;
                                }
                                else if (inBody)
                                {
                                    currentSnake->Body[currentBodyIndex++] = pos;
                                    currentSnake->Length++;
                                    currentSnake->BodyHash = (currentSnake->BodyHash * 31) + pos;
                                }
                            }
                        }
                        else if ((inFood || inHazards) && propertyName.SequenceEqual(X_BYTES))
                        {
                            reader.Read();
                            int x = reader.GetInt32();
                            reader.Read(); // Skip Y property name
                            reader.Read(); // Get Y value
                            int y = reader.GetInt32();
                            
                            ushort pos = GridMath.ToIndex(x, y, state->Width);
                            
                            if (inFood && foodIndex < 200)
                            {
                                state->FoodPositions[foodIndex++] = pos;
                                state->FoodCount = foodIndex;
                            }
                            else if (inHazards && hazardIndex < 255)
                            {
                                state->HazardPositions[hazardIndex++] = pos;
                                state->HazardCount = hazardIndex;
                            }
                        }
                        break;
                        
                    case JsonTokenType.StartObject:
                        depth++;
                        if (inSnakes && depth == 3 && snakeIndex < 4)
                        {
                            currentSnake = &state->Snakes[snakeIndex];
                            currentSnake->Body = state->SnakeBodies + bodyOffset;
                            currentSnake->BodyHash = 0;
                            snakeIndex++;
                            state->SnakeCount = snakeIndex;
                        }
                        break;
                        
                    case JsonTokenType.EndObject:
                        depth--;
                        if (currentSnake != null && depth == 2)
                        {
                            bodyOffset += currentSnake->Length;
                            currentSnake = null;
                        }
                        else if (depth == 1)
                        {
                            inBoard = false;
                            inYou = false;
                        }
                        break;
                        
                    case JsonTokenType.StartArray:
                        // Arrays don't increase depth
                        break;
                        
                    case JsonTokenType.EndArray:
                        if (inBody)
                        {
                            inBody = false;
                        }
                        else if (inSnakes)
                        {
                            inSnakes = false;
                        }
                        else if (inFood)
                        {
                            inFood = false;
                        }
                        else if (inHazards)
                        {
                            inHazards = false;
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Versione con allocazione minima per string input
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ParseIntoGameState(string json, GameState* state)
        {
            // Rent buffer dal pool per evitare allocazioni
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(json);
            byte[] rentedBuffer = _bufferPool.Rent(byteCount);
            
            try
            {
                int written = System.Text.Encoding.UTF8.GetBytes(json, rentedBuffer);
                ParseIntoGameState(rentedBuffer.AsSpan(0, written), state);
            }
            finally
            {
                _bufferPool.Return(rentedBuffer);
            }
        }
    }
    
    /// <summary>
    /// GameManager con parsing generato
    /// </summary>
    public static unsafe partial class GameManager
    {
        /// <summary>
        /// Update ottimizzato che usa il parser generato
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public static void UpdateFromJson(ReadOnlySpan<byte> jsonData)
        {
            // Reset contatori prima del parsing
            var state = State;
            state->SnakeCount = 0;
            state->FoodCount = 0;
            state->HazardCount = 0;
            
            // Usa il parser generato
            BattlesnakeJsonParser.ParseIntoGameState(jsonData, state);
        }
        
        /// <summary>
        /// Versione string per compatibilità
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateFromJson(string json)
        {
            BattlesnakeJsonParser.ParseIntoGameState(json, State);
        }
    }
}
";
        }
    }
}