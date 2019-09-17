using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct Rotation : IComponentData
    {
        public quaternion Value;
    }
}