using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace PaintECS
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RenderSystem : SystemBase
    {
        private const int BATCH_SIZE = 1023;

        private EntityQuery _renderQuery;
        private Matrix4x4[] _matrices = new Matrix4x4[BATCH_SIZE];
        private Vector4[] _colors = new Vector4[BATCH_SIZE];
        private MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private List<RenderData> _renderDatas = new List<RenderData>(2);
     
        protected override  void OnCreate()
        {
            _renderQuery = GetEntityQuery(new EntityQueryDesc {
                All = new[]{
                    ComponentType.ReadOnly<RenderData>(),
                    ComponentType.ReadOnly<RenderColor>(),
                    ComponentType.ReadOnly<WorldTransform>()
                }
            });
        }
        
        protected override void OnUpdate()
        {
            EntityManager.GetAllUniqueSharedComponentsManaged(_renderDatas);

            if (_renderDatas.Count == 0)
            {
                return;
            }
            
            for (int i = 0; i < _renderDatas.Count; i++)
            {
                if (_renderDatas[i].Mesh != null)
                {
                    RenderInstancedMesh(_renderDatas[i], Dependency);
                }
            }

            _renderDatas.Clear();
        }

        private void RenderInstancedMesh(RenderData renderData, JobHandle jobHandle)
        {
            _renderQuery.SetSharedComponentFilterManaged(renderData);
            int count = _renderQuery.CalculateEntityCount();
            if (count == 0)
            {
                return;
            }

            Profiler.BeginSample("RenderSystem");
            
            
            var matricesBuffer = new NativeArray<float4x4>(count, Allocator.TempJob);
            var colorsBuffer = new NativeArray<Vector4>(count, Allocator.TempJob);

            CollectMatricesJob job = new CollectMatricesJob
            {
                output = matricesBuffer,
                colors = colorsBuffer
            };

            Profiler.BeginSample("PopulateBuffers");
            job.ScheduleParallel(_renderQuery, jobHandle).Complete();
            Profiler.EndSample(); // PopulateBuffers
            
            for (int i = 0; i < count; i += BATCH_SIZE)
            {
                int len = Math.Min(count - i, BATCH_SIZE);
                
                Profiler.BeginSample("CopyMatrices");
                Utils.CopyNativeToManaged(ref _matrices, matricesBuffer, i, len);
                Profiler.EndSample(); // CopyMatrices
                
                Profiler.BeginSample("CopyColors");
                Utils.CopyNativeToManaged<Vector4>(ref _colors, colorsBuffer, i, len);
                Profiler.EndSample(); // CopyColors
                _propertyBlock.SetVectorArray("_BaseColor", _colors);
                
                Profiler.BeginSample("DrawMeshInstanced");
                Graphics.DrawMeshInstanced(renderData.Mesh, 0, renderData.Material, _matrices, len, _propertyBlock);
                Profiler.EndSample(); // CopyCo
            }
                
            Profiler.BeginSample("Dispose");
            matricesBuffer.Dispose();
            colorsBuffer.Dispose();
            Profiler.EndSample(); // Dispose
            
            Profiler.EndSample(); // RenderSystem
        }
       
   

        [BurstCompile]
        partial struct CollectMatricesJob : IJobEntity
        {
            [WriteOnly] public NativeArray<float4x4> output;

            [WriteOnly] public NativeArray<Vector4> colors;

            public void Execute(
                [EntityIndexInQuery] int index,
                in WorldTransform transform,
                in RenderColor color)
            {
                output[index] = transform.Value;
                colors[index] = color.Value;
            }
        }
    }
}