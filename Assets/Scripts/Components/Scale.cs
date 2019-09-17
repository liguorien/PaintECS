using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct Scale : IComponentData
    {
        public float3 Value;
    }
}