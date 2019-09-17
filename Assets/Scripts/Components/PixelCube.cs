using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct PixelCube : IComponentData
    {
        public int2 Position;
    }
}