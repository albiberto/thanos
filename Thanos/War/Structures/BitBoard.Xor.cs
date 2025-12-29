using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Thanos.War.Structures;

public readonly ref partial struct Bitboard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Xor(Bitboard other)
    {
        switch (_ulongsCount)
        {
            case 1: // 7x7 (49 bit)
                if (Vector64.IsHardwareAccelerated)
                {
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);
                }
                else
                {
                    _first ^= other._first;
                }
                return;

            case 2: // 11x11 (121 bit)
                if (Vector128.IsHardwareAccelerated)
                {
                    var v1 = Vector128.LoadUnsafe(ref _first);
                    var v2 = Vector128.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 1));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 1));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                }
                else
                {
                    _first ^= other._first;
                    Unsafe.Add(ref _first, 1) ^= Unsafe.Add(ref other._first, 1);
                }
                return;

            case 3: // Intermedio
                if (Vector128.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 2 + 1
                    var v1 = Vector128.LoadUnsafe(ref _first);
                    var v2 = Vector128.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 1));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 1));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 1));

                    var v5 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v6 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else
                {
                    _first ^= other._first;
                    Unsafe.Add(ref _first, 1) ^= Unsafe.Add(ref other._first, 1);
                    Unsafe.Add(ref _first, 2) ^= Unsafe.Add(ref other._first, 2);
                }
                return;

            case 4:
                if (Vector256.IsHardwareAccelerated)
                {
                    var v1 = Vector256.LoadUnsafe(ref _first);
                    var v2 = Vector256.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    // 2 + 2
                    var v1 = Vector128.LoadUnsafe(ref _first);
                    var v2 = Vector128.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1 + 1
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 1));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 1));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 1));

                    var v5 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v6 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 2));

                    var v7 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 3));
                    var v8 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 3));
                    (v7 ^ v8).StoreUnsafe(ref Unsafe.Add(ref _first, 3));
                }
                else
                {
                    _first ^= other._first;
                    Unsafe.Add(ref _first, 1) ^= Unsafe.Add(ref other._first, 1);
                    Unsafe.Add(ref _first, 2) ^= Unsafe.Add(ref other._first, 2);
                    Unsafe.Add(ref _first, 3) ^= Unsafe.Add(ref other._first, 3);
                }
                return;

            case 5:
                if (Vector256.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 4 + 1
                    var v1 = Vector256.LoadUnsafe(ref _first);
                    var v2 = Vector256.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector128.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 2 + 2 + 1
                    var v1 = Vector128.LoadUnsafe(ref _first);
                    var v2 = Vector128.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 2));

                    var v5 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v6 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 1));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 1));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 1));

                    var v5 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v6 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 2));

                    var v7 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 3));
                    var v8 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 3));
                    (v7 ^ v8).StoreUnsafe(ref Unsafe.Add(ref _first, 3));

                    var v9 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v10 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v9 ^ v10).StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else
                {
                    _first ^= other._first;
                    Unsafe.Add(ref _first, 1) ^= Unsafe.Add(ref other._first, 1);
                    Unsafe.Add(ref _first, 2) ^= Unsafe.Add(ref other._first, 2);
                    Unsafe.Add(ref _first, 3) ^= Unsafe.Add(ref other._first, 3);
                    Unsafe.Add(ref _first, 4) ^= Unsafe.Add(ref other._first, 4);
                }
                return;

            case 6: // 19x19
                if (Vector256.IsHardwareAccelerated && Vector128.IsHardwareAccelerated)
                {
                    // 4 + 2
                    var v1 = Vector256.LoadUnsafe(ref _first);
                    var v2 = Vector256.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    // 2 + 2 + 2
                    var v1 = Vector128.LoadUnsafe(ref _first);
                    var v2 = Vector128.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector128.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v4 = Vector128.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 2));

                    var v5 = Vector128.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v6 = Vector128.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1 + 1 + 1 + 1
                    var v1 = Vector64.LoadUnsafe(ref _first);
                    var v2 = Vector64.LoadUnsafe(ref other._first);
                    (v1 ^ v2).StoreUnsafe(ref _first);

                    var v3 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 1));
                    var v4 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 1));
                    (v3 ^ v4).StoreUnsafe(ref Unsafe.Add(ref _first, 1));

                    var v5 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 2));
                    var v6 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 2));
                    (v5 ^ v6).StoreUnsafe(ref Unsafe.Add(ref _first, 2));

                    var v7 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 3));
                    var v8 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 3));
                    (v7 ^ v8).StoreUnsafe(ref Unsafe.Add(ref _first, 3));

                    var v9 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 4));
                    var v10 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 4));
                    (v9 ^ v10).StoreUnsafe(ref Unsafe.Add(ref _first, 4));

                    var v11 = Vector64.LoadUnsafe(ref Unsafe.Add(ref _first, 5));
                    var v12 = Vector64.LoadUnsafe(ref Unsafe.Add(ref other._first, 5));
                    (v11 ^ v12).StoreUnsafe(ref Unsafe.Add(ref _first, 5));
                }
                else
                {
                    _first ^= other._first;
                    Unsafe.Add(ref _first, 1) ^= Unsafe.Add(ref other._first, 1);
                    Unsafe.Add(ref _first, 2) ^= Unsafe.Add(ref other._first, 2);
                    Unsafe.Add(ref _first, 3) ^= Unsafe.Add(ref other._first, 3);
                    Unsafe.Add(ref _first, 4) ^= Unsafe.Add(ref other._first, 4);
                    Unsafe.Add(ref _first, 5) ^= Unsafe.Add(ref other._first, 5);
                }
                return;

            default:
                XorScalar(other, _ulongsCount);
                return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XorScalar(Bitboard other, int count)
    {
        for (var i = 0; i < count; i++)
            Unsafe.Add(ref _first, i) ^= Unsafe.Add(ref other._first, i);
    }
}