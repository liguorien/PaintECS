using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TransformSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var parentMatrices = new NativeParallelHashMap<int, float4x4>(16, Allocator.TempJob);

            var collectParentsHandle = new CollectParentMatricesJob
            {
                output = parentMatrices.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new UpdateTransformsJob
            {
                parentMatrices = parentMatrices
            }.ScheduleParallel(collectParentsHandle);

            parentMatrices.Dispose(state.Dependency);
        }
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
           
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
           
        }
        
        #region Jobs
      
        [BurstCompile]
        partial struct CollectParentMatricesJob : IJobEntity
        {
            [WriteOnly] public NativeParallelHashMap<int, float4x4>.ParallelWriter output;
            
            void Execute(
                in Parent parent,
                in Position position,
                in Rotation rotation,
                in Scale scale)
            {
                
                output.TryAdd(parent.Id, float4x4.TRS(position.Value, rotation.Value, scale.Value));
            }
        }
        

        [BurstCompile]
        partial struct UpdateTransformsJob : IJobEntity
        {
            [ReadOnly] public NativeParallelHashMap<int, float4x4> parentMatrices;

            void Execute(
                ref WorldTransform transform,
                in ParentId parent,
                in Rotation rotation,
                in Position position,
                in Scale scale)
            {
                // it's a lot cheaper to set parentId to zero instead of removing the ParentId component
                if (parent.Active && parent.Value != 0)
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