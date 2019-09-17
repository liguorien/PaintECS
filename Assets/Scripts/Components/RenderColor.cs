using Unity.Entities;
using UnityEngine;

namespace PaintECS
{
    public struct RenderColor : IComponentData
    {
        public Vector4 Value;
    }
}