namespace Thanos.LightSpeed;

public static class PrecomputedBoards
{
    public static readonly LSBitboard Border11x11 = BuildBorder(11, 11);
    public static readonly LSBitboard Border19x19 = BuildBorder(19, 19);

    private static LSBitboard BuildBorder(int w, int h)
    {
        var b = new LSBitboard();
        b.Clear();
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                if (x == 0 || x > w || y == 0 || y > h)
                {
                    b.Set((byte)((y << 4) | x));
                }
            }
        }
        return b;
    }
}