using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct Position : IComponentData
    {
        public float3 Value;
    }
}