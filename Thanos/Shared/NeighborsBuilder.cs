namespace Thanos.Shared;

public static class NeighborsBuilder
{
    public static void Populate(int width, int height, Span<ushort> memory)
    {
        if (memory.Length != width * height * 4) throw new ArgumentException("Buffer size mismatch for Neighbors.");

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var currentPos = y * width + x;
                var baseOffset = currentPos * 4;

                memory[baseOffset + 0] = y >= height - 1 
                    ? ushort.MaxValue 
                    : (ushort)(currentPos + width);
                
                memory[baseOffset + 1] = y == 0 
                    ? ushort.MaxValue 
                    : (ushort)(currentPos - width);
                
                memory[baseOffset + 2] = x == 0 
                    ? ushort.MaxValue 
                    : (ushort)(currentPos - 1);
                
                memory[baseOffset + 3] = x == width - 1 
                    ? ushort.MaxValue 
                    : (ushort)(currentPos + 1);
            }
        }
    }
}