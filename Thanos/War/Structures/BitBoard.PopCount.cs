using System.Numerics;
using System.Runtime.CompilerServices;

namespace Thanos.War.Structures;

public readonly ref partial struct Bitboard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PopCount()
    {
        switch (_ulongsCount)
        {
            case 1:
                return BitOperations.PopCount(_root);
            case 2:
                return BitOperations.PopCount(_root) + BitOperations.PopCount(Unsafe.Add(ref _root, 1));
            case 3:
                return BitOperations.PopCount(_root) + BitOperations.PopCount(Unsafe.Add(ref _root, 1)) + BitOperations.PopCount(Unsafe.Add(ref _root, 2));
            case 4:
                return BitOperations.PopCount(_root) + BitOperations.PopCount(Unsafe.Add(ref _root, 1)) + BitOperations.PopCount(Unsafe.Add(ref _root, 2)) + BitOperations.PopCount(Unsafe.Add(ref _root, 3));
            case 5:
                return BitOperations.PopCount(_root) + BitOperations.PopCount(Unsafe.Add(ref _root, 1)) + BitOperations.PopCount(Unsafe.Add(ref _root, 2)) + BitOperations.PopCount(Unsafe.Add(ref _root, 3)) + BitOperations.PopCount(Unsafe.Add(ref _root, 4));
            case 6:
                return BitOperations.PopCount(_root) + BitOperations.PopCount(Unsafe.Add(ref _root, 1)) + BitOperations.PopCount(Unsafe.Add(ref _root, 2)) + BitOperations.PopCount(Unsafe.Add(ref _root, 3)) + BitOperations.PopCount(Unsafe.Add(ref _root, 4)) + BitOperations.PopCount(Unsafe.Add(ref _root, 5));
        }

        var count = 0;
        for (var i = 0; i < _ulongsCount; i++) count += BitOperations.PopCount(Unsafe.Add(ref _root, i));
        return count;
    }
}