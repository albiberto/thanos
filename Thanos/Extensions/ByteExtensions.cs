using Thanos.Common;

namespace Thanos.Extensions;

public static class ByteExtensions
{
    public static string ToApiMove(this byte move) =>
        move switch
        {
            Moves.Up => "up",
            Moves.Down => "down",
            Moves.Left => "left",
            Moves.Right => "right",
            _ => "none"
        };
}