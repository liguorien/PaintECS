using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct RestoreParentView : IComponentData
    {
        public float3 Position;

        public static RestoreParentView fromData(FixedList64Bytes<float> data)
        {
            return new RestoreParentView
            {
                Position = new float3(data[0], data[1], data[2])
            };
        }
    }
}