using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace PaintECS
{
    public struct RenderData : ISharedComponentData, IEquatable<RenderData>
    {
        public Mesh Mesh;
        public Material Material;

        public bool Equals(RenderData other)
        {
            return
                Mesh == other.Mesh &&
                Material == other.Material ;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(Mesh, null)) hash ^= Mesh.GetHashCode();
            if (!ReferenceEquals(Material, null)) hash ^= Material.GetHashCode();
            return hash;
        }
    }
}