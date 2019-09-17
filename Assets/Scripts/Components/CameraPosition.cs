using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct CameraController : IComponentData
    {
        public bool Active;
        public float3 Offset;
    }
}