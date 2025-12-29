using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Thanos.War.Structures;

public readonly ref partial struct Bitboard
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        switch (_ulongsCount)
        {
            case 1: // 7x7 (49 bit)
                if (Vector64.IsHardwareAccelerated)
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                else
                    _first = 0;
                return;

            case 2: // 11x11 (121 bit) - Standard
                if (Vector128.IsHardwareAccelerated)
                {
                    Vector128<ulong>.Zero.StoreUnsafe(ref _first);
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                }
                else
                {
                    _first = 0;
                    Unsafe.Add(ref _first, 1) = 0;
                }

                return;

            case 3: // Intermedio (3 ulongs)
                if (Vector128.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 2 + 1
                    Vector128<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else
                {
                    _first = 0;
                    Unsafe.Add(ref _first, 1) = 0;
                    Unsafe.Add(ref _first, 2) = 0;
                }

                return;

            case 4: // (4 ulongs)
                if (Vector256.IsHardwareAccelerated)
                {
                    Vector256<ulong>.Zero.StoreUnsafe(ref _first);
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    // 2 + 2
                    Vector128<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector128<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1 + 1
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 3));
                }
                else
                {
                    _first = 0;
                    Unsafe.Add(ref _first, 1) = 0;
                    Unsafe.Add(ref _first, 2) = 0;
                    Unsafe.Add(ref _first, 3) = 0;
                }

                return;

            case 5: // (5 ulongs)
                if (Vector256.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 4 + 1
                    Vector256<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector128.IsHardwareAccelerated && Vector64.IsHardwareAccelerated)
                {
                    // 2 + 2 + 1
                    Vector128<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector128<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // Fallback solo su Vector64
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 3));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else
                {
                    _first = 0;
                    Unsafe.Add(ref _first, 1) = 0;
                    Unsafe.Add(ref _first, 2) = 0;
                    Unsafe.Add(ref _first, 3) = 0;
                    Unsafe.Add(ref _first, 4) = 0;
                }

                return;

            case 6: // 19x19 (361 bit) - Large
                if (Vector256.IsHardwareAccelerated && Vector128.IsHardwareAccelerated)
                {
                    // 4 + 2
                    Vector256<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector128<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    // 2 + 2 + 2
                    Vector128<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector128<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                    Vector128<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                }
                else if (Vector64.IsHardwareAccelerated)
                {
                    // 1 + 1 + 1 + 1 + 1 + 1
                    Vector64<ulong>.Zero.StoreUnsafe(ref _first);
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 1));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 2));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 3));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 4));
                    Vector64<ulong>.Zero.StoreUnsafe(ref Unsafe.Add(ref _first, 5));
                }
                else
                {
                    _first = 0;
                    Unsafe.Add(ref _first, 1) = 0;
                    Unsafe.Add(ref _first, 2) = 0;
                    Unsafe.Add(ref _first, 3) = 0;
                    Unsafe.Add(ref _first, 4) = 0;
                    Unsafe.Add(ref _first, 5) = 0;
                }

                return;

            default:
                Buffer.Clear();
                break;
        }
    }
}