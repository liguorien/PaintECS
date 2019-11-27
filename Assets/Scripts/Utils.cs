using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace PaintECS
{
    public class Utils
    {
        unsafe public static void CopyNativeToManaged<T>(
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
        
        unsafe public static void CopyNativeToManaged<T,B>(
            ref T[] destination,
            NativeArray<B> source,
            int offset,
            int count
        ) where T : struct where B : struct
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

//        public static NativeHashMap<int, int> Copy(this NativeHashMap<int, int>)
//        {
//            
//        }
        
//          unsafe public static void ReallocateHashMap<TKey, TValue>(NativeHashMapData* data, int newCapacity,
//            int newBucketCapacity, Allocator label)
//            where TKey : struct
//            where TValue : struct
//        {
//            newBucketCapacity = CollectionHelper.CeilPow2(newBucketCapacity);
//
//            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
//                return;
//
//            if (data->keyCapacity > newCapacity)
//                throw new Exception("Shrinking a hash map is not supported");
//
//            int keyOffset, nextOffset, bucketOffset;
//            int totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out keyOffset,
//                out nextOffset, out bucketOffset);
//
//            byte* newData = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
//            byte* newKeys = newData + keyOffset;
//            byte* newNext = newData + nextOffset;
//            byte* newBuckets = newData + bucketOffset;
//
//            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
//            UnsafeUtility.MemCpy(newData, data->values, data->keyCapacity * UnsafeUtility.SizeOf<TValue>());
//            UnsafeUtility.MemCpy(newKeys, data->keys, data->keyCapacity * UnsafeUtility.SizeOf<TKey>());
//            UnsafeUtility.MemCpy(newNext, data->next, data->keyCapacity * UnsafeUtility.SizeOf<int>());
//            for (int emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
//                ((int*)newNext)[emptyNext] = -1;
//
//            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
//            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
//                ((int*)newBuckets)[bucket] = -1;
//            for (int bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
//            {
//                int* buckets = (int*)data->buckets;
//                int* nextPtrs = (int*)newNext;
//                while (buckets[bucket] >= 0)
//                {
//                    int curEntry = buckets[bucket];
//                    buckets[bucket] = nextPtrs[curEntry];
//                    int newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() &
//                                    (newBucketCapacity - 1);
//                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
//                    ((int*)newBuckets)[newBucket] = curEntry;
//                }
//            }
//
//            UnsafeUtility.Free(data->values, label);
//            if (data->allocatedIndexLength > data->keyCapacity)
//                data->allocatedIndexLength = data->keyCapacity;
//            data->values = newData;
//            data->keys = newKeys;
//            data->next = newNext;
//            data->buckets = newBuckets;
//            data->keyCapacity = newCapacity;
//            data->bucketCapacityMask = newBucketCapacity - 1;
//        }

    }
    
    
    struct PurgeComponent<T> : IJobForEachWithEntity<T> where T : struct, IComponentData
    {
        public EntityCommandBuffer.Concurrent Buffer;
        
        public void Execute(Entity entity, int index, ref T c1)
        {
     //       Debug.Log("Removing component : " + c1);
            Buffer.RemoveComponent<T>(index, entity);
        }
    }
    
    
    
}