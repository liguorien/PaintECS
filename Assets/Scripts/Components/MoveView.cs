using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct MoveView : IComponentData
    {
        public float Duration;
        public Easing Easing;
        public float3 Position;
        public quaternion Rotation;
        public float3 Scale;
        
        
        
        public static MoveView fromData(float duration, FixedList64Bytes<float> data)
        {
            return new MoveView
            {
                Duration = duration,
                Position = new float3(data[0], data[1], data[2]),
                Rotation = new quaternion(data[3],data[4],data[5],data[6]),
                Scale = new float3(data[7],data[8],data[9]),
                Easing = data.Length > 10 ? (Easing)data[10] : Easing.Linear
            };
        }
    }
}