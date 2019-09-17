using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS
{
    public class TransformSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var parentMatrices = new NativeHashMap<int, float4x4>(16, Allocator.TempJob);
            return parentMatrices.Dispose(
                new CollectMatricesJob
                {
                    parentMatrices = parentMatrices
                }.Schedule(this, 
                new CollectParentMatricesJob
                {
                    output = parentMatrices.AsParallelWriter()
                }.Schedule(this, inputDependencies))
            );
        }

        
        #region Jobs
      
        [BurstCompile]
        struct CollectParentMatricesJob : IJobForEach<Parent, Position, Rotation, Scale>
        {
            [WriteOnly] public NativeHashMap<int, float4x4>.ParallelWriter output;
            
            public void Execute(
                [ReadOnly] ref Parent parent,
                [ReadOnly] ref Position position,
                [ReadOnly] ref Rotation rotation,
                [ReadOnly] ref Scale scale)
            {
                
                output.TryAdd(parent.Id, float4x4.TRS(position.Value, rotation.Value, scale.Value));
            }
        }
        

        [BurstCompile]
        struct CollectMatricesJob : IJobForEachWithEntity<ParentId, WorldTransform, Rotation, Position, Scale>
        {
            [ReadOnly] public NativeHashMap<int, float4x4> parentMatrices;

            public void Execute(
                Entity entity,
                int index,
                [ReadOnly] ref ParentId parent,
                ref WorldTransform transform,
                [ReadOnly] ref Rotation rotation,
                [ReadOnly] ref Position position,
                [ReadOnly] ref Scale scale)
            {
                // it's a lot cheaper to set parentId to zero instead of removing the ParentId component
                if (parent.Value != 0 && parent.Active)
                {
                    parentMatrices.TryGetValue(parent.Value, out float4x4 parentMatrix);
            
                    transform.Value = math.mul(parentMatrix, float4x4.TRS(position.Value, rotation.Value, scale.Value));
                }
                else
                {
                    transform.Value = float4x4.TRS(position.Value, rotation.Value, scale.Value);
                }
            }
        }
        #endregion

    }
}