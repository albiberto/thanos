namespace Thanos.LightSpeed;

public static class PrecomputedBoards
{
    public static readonly Bitboard256 Border11x11 = BuildBorder(11, 11);
    public static readonly Bitboard256 Border19x19 = BuildBorder(19, 19);

    private static Bitboard256 BuildBorder(int w, int h)
    {
        var b = new Bitboard256();
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