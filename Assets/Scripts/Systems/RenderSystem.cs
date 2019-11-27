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
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class RenderSystem : JobComponentSystem
    {
        private const int BATCH_SIZE = 1023;

        private EntityQuery _renderQuery;
        private Matrix4x4[] _matrices = new Matrix4x4[BATCH_SIZE];
        private Vector4[] _colors = new Vector4[BATCH_SIZE];
        private MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private List<RenderData> _renderDatas = new List<RenderData>(2);
        

        protected override void OnCreate()
        {
            _renderQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[]{
                    ComponentType.ReadOnly<RenderData>(),
                    ComponentType.ReadOnly<RenderColor>(),
                    ComponentType.ReadOnly<WorldTransform>()
                }
            });
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            
            EntityManager.GetAllUniqueSharedComponentData(_renderDatas);

            if (_renderDatas.Count == 0)
            {
                return inputDependencies;
            }
            
            for (int i = 0; i < _renderDatas.Count; i++)
            {
                if (_renderDatas[i].Mesh != null)
                {
                    RenderInstancedMesh(_renderDatas[i], inputDependencies);
                }
            }

            _renderDatas.Clear();
            
            return inputDependencies;
        }

        private void RenderInstancedMesh(RenderData renderData, JobHandle jobHandle)
        {
           
            _renderQuery.SetSharedComponentFilter(renderData);
            int count = _renderQuery.CalculateEntityCount();
            if (count == 0)
            {
                return;
            }

            var matricesBuffer = new NativeArray<float4x4>(count, Allocator.TempJob);
            var colorsBuffer = new NativeArray<Vector4>(count, Allocator.TempJob);

            CollectMatricesJob job = new CollectMatricesJob
            {
                output = matricesBuffer,
                colors = colorsBuffer
            };

            JobForEachExtensions.Schedule(job, _renderQuery, jobHandle).Complete();

            
            for (int i = 0; i < count; i += BATCH_SIZE)
            {
                int len = Math.Min(count - i, BATCH_SIZE);
                
                Utils.CopyNativeToManaged(ref _matrices, matricesBuffer, i, len);
                Utils.CopyNativeToManaged<Vector4>(ref _colors, colorsBuffer, i, len);
                _propertyBlock.SetVectorArray("_BaseColor", _colors);
                Graphics.DrawMeshInstanced(renderData.Mesh, 0, renderData.Material, _matrices, len, _propertyBlock);
            }
                
            matricesBuffer.Dispose();
            colorsBuffer.Dispose();
        }
       
   

        [BurstCompile]
        struct CollectMatricesJob : IJobForEachWithEntity<WorldTransform, RenderColor>
        {
            [WriteOnly] public NativeArray<float4x4> output;

            [WriteOnly] public NativeArray<Vector4> colors;

            public void Execute(
                Entity entity,
                int index,
                [ReadOnly] ref WorldTransform transform,
                [ReadOnly] ref RenderColor color)
            {
                output[index] = transform.Value;
                colors[index] = color.Value;
            }
        }
    }
}