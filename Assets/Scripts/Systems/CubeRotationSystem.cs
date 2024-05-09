using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS
{
    public partial struct CubeRotationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var hitList = new NativeQueue<Entity>(Allocator.TempJob);
            var job = new RotateGameCubeSystemJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                hitList = hitList.AsParallelWriter()
            };

            job.Schedule(state.Dependency).Complete();
            
            if (hitList.Count > 0)
            {
                // for small batches, there is no noticeable impact on performance
                // for removing the component one by one
                if (hitList.Count < 4096)
                {
                    while (hitList.Count > 0)
                    {
                        state.EntityManager.RemoveComponent<CubeRotation>(hitList.Dequeue());
                    }
                }
                else
                {
                    // for big batches, we will add a tag component and use an EntityQuery to cleanup the mess
                    var toRemove = new NativeArray<Entity>(hitList.Count, Allocator.TempJob);

                    int i = 0;
                    while (hitList.Count > 0)
                    {
                        toRemove[i++] = hitList.Dequeue();
                    }
                    state.EntityManager.RemoveComponent<CubeRotation>(toRemove);
                    toRemove.Dispose();
                }
            }
            
            hitList.Dispose();
        }
        
        [BurstCompile]
        partial struct RotateGameCubeSystemJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;

            [WriteOnly] public NativeQueue<Entity>.ParallelWriter hitList;

            public void Execute(Entity entity, ref Rotation transform, ref CubeRotation rotation)
            {
                if (rotation.Completed)
                {
                    transform.Value = quaternion.Euler(0, 0, 0);
                    return;
                }

                float angleToRotate = rotation.Speed * deltaTime;

                if (rotation.AngleRotated + angleToRotate > rotation.AngleTotal)
                {
                    angleToRotate = rotation.AngleTotal - rotation.AngleRotated;
                }

                transform.Value = math.mul(
                    math.normalize(transform.Value),
                    quaternion.AxisAngle(rotation.Axis, angleToRotate * rotation.Direction)
                );

                rotation.AngleRotated += angleToRotate;

                if (rotation.AngleRotated >= rotation.AngleTotal)
                {
                    transform.Value = quaternion.Euler(0, 0, 0);
                    
                    hitList.Enqueue(entity);
                }
            }
        }
    }
}