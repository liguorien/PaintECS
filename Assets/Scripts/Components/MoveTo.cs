using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct MoveTo : IComponentData, ITween
    {
        public float3 Origin;
        public float3 Destination;

        public float Delay { get; set; }
        public float Duration { get; set; }
        public float Elapsed { get; set; }
        public Easing Easing { get; set; }
        public bool Enabled { get; set; }
    }
}