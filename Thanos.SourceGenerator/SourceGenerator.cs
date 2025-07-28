using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Thanos.SourceGenerator;

[Generator]
public class BattlesnakeJsonGenerator : IIncrementalGenerator 
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var sourceBuilder = new StringBuilder();
            
        // Headers
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
        sourceBuilder.AppendLine("using System.Runtime.InteropServices;");
        sourceBuilder.AppendLine("using System.Text.Json;");
        sourceBuilder.AppendLine("using System.IO;");
        sourceBuilder.AppendLine("using System.Buffers;");
        sourceBuilder.AppendLine("using System.Threading.Tasks;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace BattlesnakeOptimized");
        sourceBuilder.AppendLine("{");
            
        // Generate parser class
        GenerateParserClass(sourceBuilder);
            
        sourceBuilder.AppendLine("}");
            
        context.AddSource("BattlesnakeStreamParser.g.cs", 
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private void GenerateParserClass(StringBuilder sb)
    {
        sb.AppendLine("    public static unsafe class BattlesnakeStreamParser");
        sb.AppendLine("    {");
            
        // Constants
        GenerateConstants(sb);
            
        // Main parsing methods
        GenerateStreamParseMethods(sb);
            
        // Core parser
        GenerateCoreParser(sb);
            
        // Helper methods
        GenerateHelperMethods(sb);
            
        sb.AppendLine("    }");
    }

    private void GenerateConstants(StringBuilder sb)
    {
        sb.AppendLine("        // Pre-computed tokens for fast comparison");
        sb.AppendLine("        private static ReadOnlySpan<byte> TurnToken => \"turn\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> BoardToken => \"board\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> WidthToken => \"width\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> HeightToken => \"height\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> SnakesToken => \"snakes\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> FoodToken => \"food\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> HazardsToken => \"hazards\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> IdToken => \"id\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> HeadToken => \"head\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> BodyToken => \"body\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> HealthToken => \"health\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> XToken => \"x\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> YToken => \"y\"u8;");
        sb.AppendLine("        private static ReadOnlySpan<byte> YouToken => \"you\"u8;");
        sb.AppendLine();
    }

    private void GenerateStreamParseMethods(StringBuilder sb)
    {
        // Async version
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        sb.AppendLine("        public static async ValueTask ParseDirectFromStreamAsync(Stream stream, GameState* state, CancellationToken ct = default)");
        sb.AppendLine("        {");
        sb.AppendLine("            using var rentedBuffer = MemoryPool<byte>.Shared.Rent(8192);");
        sb.AppendLine("            var memory = rentedBuffer.Memory;");
        sb.AppendLine("            ");
        sb.AppendLine("            int totalRead = 0;");
        sb.AppendLine("            int read;");
        sb.AppendLine("            while ((read = await stream.ReadAsync(memory.Slice(totalRead), ct)) > 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                totalRead += read;");
        sb.AppendLine("                if (totalRead >= memory.Length - 100) break;");
        sb.AppendLine("            }");
        sb.AppendLine("            ");
        sb.AppendLine("            var span = memory.Span.Slice(0, totalRead);");
        sb.AppendLine("            ParseFromSpan(span, state);");
        sb.AppendLine("        }");
        sb.AppendLine();
            
        // Sync version
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        sb.AppendLine("        public static void ParseDirectFromStream(Stream stream, GameState* state)");
        sb.AppendLine("        {");
        sb.AppendLine("            Span<byte> buffer = stackalloc byte[8192];");
        sb.AppendLine("            int bytesRead = stream.Read(buffer);");
        sb.AppendLine("            ParseFromSpan(buffer.Slice(0, bytesRead), state);");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateCoreParser(StringBuilder sb)
    {
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        sb.AppendLine("        private static void ParseFromSpan(ReadOnlySpan<byte> data, GameState* state)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Reset state");
        sb.AppendLine("            state->SnakeCount = 0;");
        sb.AppendLine("            state->FoodCount = 0;");
        sb.AppendLine("            state->HazardCount = 0;");
        sb.AppendLine("            ");
        sb.AppendLine("            var reader = new Utf8JsonReader(data, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });");
        sb.AppendLine("            ");
        sb.AppendLine("            // Parser state");
        sb.AppendLine("            Span<byte> youId = stackalloc byte[64];");
        sb.AppendLine("            int youIdLength = 0;");
        sb.AppendLine("            ushort bodyOffset = 0;");
        sb.AppendLine("            byte snakeIndex = 0;");
        sb.AppendLine("            Snake* currentSnake = null;");
        sb.AppendLine("            bool inBoard = false;");
        sb.AppendLine("            bool inSnakes = false;");
        sb.AppendLine("            bool inFood = false;");
        sb.AppendLine("            bool inHazards = false;");
        sb.AppendLine("            bool inYou = false;");
        sb.AppendLine("            bool inBody = false;");
        sb.AppendLine("            int depth = 0;");
        sb.AppendLine("            ");
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                switch (reader.TokenType)");
        sb.AppendLine("                {");
        sb.AppendLine("                    case JsonTokenType.PropertyName:");
        sb.AppendLine("                        ProcessPropertyName(ref reader, state, ref youId, ref youIdLength,");
        sb.AppendLine("                                          ref bodyOffset, ref snakeIndex, ref currentSnake,");
        sb.AppendLine("                                          ref inBoard, ref inSnakes, ref inFood, ref inHazards,");
        sb.AppendLine("                                          ref inYou, ref inBody, depth);");
        sb.AppendLine("                        break;");
        sb.AppendLine("                        ");
        sb.AppendLine("                    case JsonTokenType.StartObject:");
        sb.AppendLine("                        depth++;");
        sb.AppendLine("                        if (inSnakes && depth == 3 && snakeIndex < 4)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            currentSnake = &state->Snakes[snakeIndex];");
        sb.AppendLine("                            currentSnake->Body = state->SnakeBodies + bodyOffset;");
        sb.AppendLine("                            currentSnake->BodyHash = 0;");
        sb.AppendLine("                            currentSnake->Length = 0;");
        sb.AppendLine("                            snakeIndex++;");
        sb.AppendLine("                            state->SnakeCount = snakeIndex;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        break;");
        sb.AppendLine("                        ");
        sb.AppendLine("                    case JsonTokenType.EndObject:");
        sb.AppendLine("                        depth--;");
        sb.AppendLine("                        if (currentSnake != null && depth == 2)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            bodyOffset += currentSnake->Length;");
        sb.AppendLine("                            currentSnake = null;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        break;");
        sb.AppendLine("                        ");
        sb.AppendLine("                    case JsonTokenType.StartArray:");
        sb.AppendLine("                        break;");
        sb.AppendLine("                        ");
        sb.AppendLine("                    case JsonTokenType.EndArray:");
        sb.AppendLine("                        if (inBody) inBody = false;");
        sb.AppendLine("                        else if (inSnakes) inSnakes = false;");
        sb.AppendLine("                        else if (inFood) inFood = false;");
        sb.AppendLine("                        else if (inHazards) inHazards = false;");
        sb.AppendLine("                        break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateHelperMethods(StringBuilder sb)
    {
        // ProcessPropertyName method
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        private static void ProcessPropertyName(ref Utf8JsonReader reader, GameState* state,");
        sb.AppendLine("                                               ref Span<byte> youId, ref int youIdLength,");
        sb.AppendLine("                                               ref ushort bodyOffset, ref byte snakeIndex,");
        sb.AppendLine("                                               ref Snake* currentSnake, ref bool inBoard,");
        sb.AppendLine("                                               ref bool inSnakes, ref bool inFood,");
        sb.AppendLine("                                               ref bool inHazards, ref bool inYou,");
        sb.AppendLine("                                               ref bool inBody, int depth)");
        sb.AppendLine("        {");
        sb.AppendLine("            ReadOnlySpan<byte> propertyName = reader.ValueSpan;");
        sb.AppendLine("            ");
        sb.AppendLine("            if (propertyName.SequenceEqual(TurnToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine("                state->Turn = (ushort)reader.GetInt32();");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(BoardToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inBoard = true;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(YouToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inYou = true;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (inBoard)");
        sb.AppendLine("            {");
        sb.AppendLine("                ProcessBoardProperty(ref reader, state, propertyName, ref inSnakes, ref inFood, ref inHazards);");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (inYou && propertyName.SequenceEqual(IdToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine("                var idSpan = reader.ValueSpan;");
        sb.AppendLine("                idSpan.CopyTo(youId);");
        sb.AppendLine("                youIdLength = idSpan.Length;");
        sb.AppendLine("                ");
        sb.AppendLine("                // Find matching snake");
        sb.AppendLine("                for (byte i = 0; i < state->SnakeCount; i++)");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Compare IDs (implement proper comparison)");
        sb.AppendLine("                    state->YouIndex = i; // Simplified");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (currentSnake != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                ProcessSnakeProperty(ref reader, currentSnake, propertyName, ref inBody, state->Width);");
        sb.AppendLine("            }");
        sb.AppendLine("            else if ((inFood || inHazards) && propertyName.SequenceEqual(XToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                ProcessCoordinate(ref reader, state, inFood, inHazards);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
            
        // Additional helper methods
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        private static void ProcessBoardProperty(ref Utf8JsonReader reader, GameState* state,");
        sb.AppendLine("                                                ReadOnlySpan<byte> propertyName,");
        sb.AppendLine("                                                ref bool inSnakes, ref bool inFood, ref bool inHazards)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (propertyName.SequenceEqual(WidthToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine("                if (state->Width == 0)");
        sb.AppendLine("                {");
        sb.AppendLine("                    byte width = (byte)reader.GetInt32();");
        sb.AppendLine("                    // Initialize if needed");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(SnakesToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inSnakes = true;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(FoodToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inFood = true;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(HazardsToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inHazards = true;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
            
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        private static void ProcessSnakeProperty(ref Utf8JsonReader reader, Snake* snake,");
        sb.AppendLine("                                               ReadOnlySpan<byte> propertyName,");
        sb.AppendLine("                                               ref bool inBody, byte width)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (propertyName.SequenceEqual(HeadToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                // Parse head coordinate");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(HealthToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine("                snake->Health = (byte)reader.GetInt32();");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (propertyName.SequenceEqual(BodyToken))");
        sb.AppendLine("            {");
        sb.AppendLine("                inBody = true;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
            
        sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine("        private static void ProcessCoordinate(ref Utf8JsonReader reader, GameState* state,");
        sb.AppendLine("                                            bool inFood, bool inHazards)");
        sb.AppendLine("        {");
        sb.AppendLine("            reader.Read();");
        sb.AppendLine("            int x = reader.GetInt32();");
        sb.AppendLine("            ");
        sb.AppendLine("            // Skip to Y");
        sb.AppendLine("            reader.Read(); // property name");
        sb.AppendLine("            reader.Read(); // value");
        sb.AppendLine("            int y = reader.GetInt32();");
        sb.AppendLine("            ");
        sb.AppendLine("            ushort pos = GridMath.ToIndex(x, y, state->Width);");
        sb.AppendLine("            ");
        sb.AppendLine("            if (inFood && state->FoodCount < 200)");
        sb.AppendLine("            {");
        sb.AppendLine("                state->FoodPositions[state->FoodCount++] = pos;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (inHazards && state->HazardCount < 255)");
        sb.AppendLine("            {");
        sb.AppendLine("                state->HazardPositions[state->HazardCount++] = pos;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }
}