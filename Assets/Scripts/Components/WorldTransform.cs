using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct WorldTransform : IComponentData
    {
        public float4x4 Value;
    }
}