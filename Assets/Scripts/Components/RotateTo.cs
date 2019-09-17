using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct RotateTo : IComponentData, ITween
    {
        public float Angle;
        public float3 Axis;
        public float Direction;
        
        public float Delay { get; set; }
        public float Duration { get; set; }
        public float Elapsed { get; set; }
        public Easing Easing { get; set; }
        public bool Enabled { get; set; }
    }
}