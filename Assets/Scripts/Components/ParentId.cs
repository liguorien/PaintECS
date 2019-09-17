using Unity.Entities;

namespace PaintECS
{
    public struct ParentId : IComponentData
    {
        public int Value;
        public bool Active;
    }
}