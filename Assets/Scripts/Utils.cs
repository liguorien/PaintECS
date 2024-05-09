
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace PaintECS
{
    public class Utils
    {
        public static unsafe void CopyNativeToManaged<T>(
            ref T[] destination,
            NativeArray<T> source,
            int offset,
            int count
        ) where T : struct
        {
            MemCpy(
                AddressOf(ref destination[0]),
                (byte*) source.GetUnsafeReadOnlyPtr() + SizeOf<T>() * offset,
                SizeOf<T>() * count
            );
        }
        
        public static unsafe void CopyNativeToManaged<T,B>(
            ref T[] destination,
            NativeArray<B> source,
            int offset,
            int count
        ) where T : struct 
          where B : struct
        {
            if (SizeOf<T>() == SizeOf<B>())
            {
                MemCpy(
                    AddressOf(ref destination[0]),
                    (byte*) source.GetUnsafeReadOnlyPtr() + SizeOf<T>() * offset,
                    SizeOf<T>() * count
                );
            }
        }
    }
}