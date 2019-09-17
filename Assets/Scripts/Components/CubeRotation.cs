using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct CubeRotation : IComponentData
    {
        public float3 Axis;
        public float AngleTotal;
        public float AngleRotated;
        public float Speed;
        public float Direction;

        public bool Completed => AngleTotal == AngleRotated;
    }
}
