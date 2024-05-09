using System.Runtime.CompilerServices;
using PaintECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Entities.ComponentType;
using Random = Unity.Mathematics.Random;

public abstract partial class PaintViewSystem<T> : SystemBase
{
    protected EntityQuery _query;
    protected EntityCommandBufferSystem _commandBufferSystem;

    protected abstract EntityQuery InitQuery();

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
        _query = InitQuery();
    }

    protected abstract JobHandle scheduleJobs(int jobsCount);
    
    protected override void OnUpdate()
    {
        int jobsCount = _query.CalculateEntityCount();

        if (jobsCount == 0)
        {
            return;
        }

        Dependency = scheduleJobs(jobsCount);

        _commandBufferSystem.CreateCommandBuffer().RemoveComponent<T>(_query, EntityQueryCaptureMode.AtRecord);
        _commandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ExplodeViewSystem : PaintViewSystem<ExplodeView>
{
    protected override EntityQuery InitQuery()
    {
        return GetEntityQuery(
            ReadOnly<ParentView>(),
            ReadOnly<ExplodeView>()
        );
    }

    protected override JobHandle scheduleJobs(int jobsCount)
    {
        var data = new NativeParallelHashMap<int, ExplodeViewJobData>(jobsCount, Allocator.TempJob);
        var prepareJob = new PrepareExplodeJob { Data = data.AsParallelWriter() };
        var executeJob = new ExplodeJob
        {
            Data = data, 
            Rng = new Random((uint) UnityEngine.Random.Range(1, uint.MaxValue))
        };

        var handle = prepareJob.Schedule(_query, Dependency);
        handle = executeJob.Schedule(handle);
        return data.Dispose(handle);
    }
    
    struct ExplodeViewJobData
    {
        public Easing Easing;
        public float Duration;
        public float Min;
        public float Max;
    }

    [BurstCompile]
    partial struct PrepareExplodeJob : IJobEntity
    {
        [WriteOnly] public NativeParallelHashMap<int, ExplodeViewJobData>.ParallelWriter Data;

        public void Execute(in ParentView view, in ExplodeView explode)
        {
            Data.TryAdd(view.Id, new ExplodeViewJobData
            {
                Min = -(view.Width / 1),
                Max = view.Width / 1,
                Duration = explode.Duration,
                Easing = explode.Easing
            });
        }
    }

    [BurstCompile]
    partial struct ExplodeJob : IJobEntity
    {
        [ReadOnly] public Random Rng;
        [ReadOnly] public NativeParallelHashMap<int, ExplodeViewJobData> Data;

        public void Execute(in ParentId parentId, in Position position, ref MoveTo moveTo)
        {
            if (!Data.ContainsKey(parentId.Value))
            {
                return;
            }

            ExplodeViewJobData data = Data[parentId.Value];

            moveTo = new MoveTo
            {
                Origin = position.Value,
                Destination = new float3(position.Value.x, position.Value.y, Rng.NextFloat(data.Min, data.Max)),
                Delay = Rng.NextFloat(0.0f, data.Duration / 20),
                Duration = data.Duration * 0.95f,
                Elapsed = 0,
                Easing = data.Easing,
                Enabled = true
            };
        }
    }
}


[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ImplodeViewSystem : PaintViewSystem<ImplodeView>
{
    protected override EntityQuery InitQuery()
    {
        return GetEntityQuery(
            ReadOnly<ParentView>(),
            ReadOnly<ImplodeView>()
        );
    }

    protected override JobHandle scheduleJobs(int jobsCount)
    {
        var data = new NativeParallelHashMap<int, ImplodeViewJobData>(jobsCount, Allocator.TempJob);
        var prepareJob = new PrepareImplodeViewJob { Data = data.AsParallelWriter() };
        var executeJob = new ImplodeViewJob { Data = data };

        var handle = prepareJob.Schedule(_query, Dependency);
        handle = executeJob.Schedule(handle);
        return data.Dispose(handle);
    }
    
    public struct ImplodeViewJobData
    {
        public Easing Easing;
        public float Duration;
    }

    
    #region Jobs

    [BurstCompile]
    partial struct PrepareImplodeViewJob : IJobEntity
    {
        [WriteOnly] public NativeParallelHashMap<int, ImplodeViewJobData>.ParallelWriter Data;

        public void Execute(in ParentView view, in ImplodeView explode)
        {
            Data.TryAdd(view.Id, new ImplodeViewJobData
            {
                Duration = explode.Duration,
                Easing = explode.Easing
            });
        }
    }

    [BurstCompile]
    partial struct ImplodeViewJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, ImplodeViewJobData> Data;

        public void Execute(in ParentId parentId, in Position position, ref MoveTo moveTo)
        {
            if (!Data.ContainsKey(parentId.Value))
            {
                return;
            }

            ImplodeViewJobData data = Data[parentId.Value];

            moveTo = new MoveTo
            {
                Origin = position.Value,
                Destination = new float3(position.Value.x, position.Value.y, 0),
                Delay = 0,
                Duration = data.Duration,
                Elapsed = 0,
                Easing = data.Easing,
                Enabled = true
            };
        }
    }
    #endregion
}



[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class MoveViewSystem : PaintViewSystem<MoveView>
{
    protected override EntityQuery InitQuery()
    {
        return GetEntityQuery(
            ReadOnly<ParentView>(),
            ReadOnly<MoveView>(),
            ReadWrite<Position>(),
            ReadOnly<Rotation>(),
            ReadOnly<Scale>()
        );
    }

    protected override JobHandle scheduleJobs(int jobsCount)
    {
        var data = new NativeParallelHashMap<int, MoveViewJobData>(jobsCount, Allocator.TempJob);
        var prepareJob = new PrepareMoveViewJob { Data = data.AsParallelWriter() };
        var executeJob = new MovePaintViewJob { Data = data, Rng = new Random((uint)UnityEngine.Random.Range(1, uint.MaxValue))};
        var handle = prepareJob.Schedule(_query, Dependency);
        handle = executeJob.Schedule(handle);
        return data.Dispose(handle);
    }
    
    struct MoveViewJobData
    {
        public float4x4 currentTransform;
        public float4x4 targetTransform;
        public float Duration;
        public Easing Easing;
    }

    [BurstCompile]
    partial struct PrepareMoveViewJob : IJobEntity
    {
        [WriteOnly] public NativeParallelHashMap<int, MoveViewJobData>.ParallelWriter Data;

        public void Execute(
            in ParentView view,
            in MoveView command,
            ref Position position,
            ref Rotation rotation,
            ref Scale scale)
        {
            Data.TryAdd(view.Id, new MoveViewJobData
            {
                Duration = command.Duration,
                Easing = command.Easing,
                currentTransform = float4x4.TRS(position.Value, rotation.Value, scale.Value),
                targetTransform = float4x4.TRS(command.Position, command.Rotation, command.Scale)
            });

            position.Value = command.Position;
            rotation.Value = command.Rotation;
            scale.Value = command.Scale;
        }
    }


    [BurstCompile]
    partial struct MovePaintViewJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, MoveViewJobData> Data;

        [ReadOnly] public Random Rng;


        public void Execute(
            in PixelCube cube,
            ref Position position,
            ref Rotation rotation,
            ref Scale scale,
            ref MoveTo moveTo,
            ref ParentId parentId)
        {
            if (!Data.ContainsKey(parentId.Value))
            {
                return;
            }

            MoveViewJobData data = Data[parentId.Value];


            // TODO: there is probably a simpler way to calculate the target world position

            float4x4 targetWorldTransform;

            if (parentId.Active)
            {
                float4x4 pixelLocalTransform = float4x4.TRS(position.Value, rotation.Value, scale.Value);
                float4x4 currentWorldTransform = math.mul(data.currentTransform, pixelLocalTransform);
                float4x4 targetLocalTransform = float4x4.TRS(
                    position.Value, //new float3(cube.Position.x - data.Width / 2, cube.Position.y - Height / 2, position.Value.z/*;Rng.NextFloat(-Width/2, Width/2)*/),
                    quaternion.identity,
                    new float3(1, 1, 1)
                );
               // Debug.Log("pos = " + position.Value) ;
                targetWorldTransform = math.mul(data.targetTransform, targetLocalTransform);

                position.Value = ExtractPosition(ref currentWorldTransform);
                //TODO: support rotation
                //     rotation.Value = ExtractPosition(ref currentWorldTransform);
                scale.Value = ExtractScale(ref currentWorldTransform);


                parentId.Active = false;
            }
            else
            {
                targetWorldTransform = math.mul(
                    data.targetTransform,
                    math.mul(
                        math.inverse(data.currentTransform),
                        float4x4.TRS(position.Value, rotation.Value, scale.Value)
                    )
                );
            }

//            position.Value = targetWorldTransform.c3.xyz;

            moveTo = new MoveTo
            {
                Origin = position.Value,
                Destination = ExtractPosition(ref targetWorldTransform),
                Delay = Rng.NextFloat(0.0f, data.Duration * 0.25f),
                Duration = data.Duration * 0.75f, //Random.Range(0.5f, 0.7f),
                Elapsed = 0,
                Enabled = true,
                Easing = data.Easing
            };
//            rotateTo = new RotateTo
//            {
//                Axis = new float3(Rng.NextFloat(0, 1), Rng.NextFloat(0, 1), Rng.NextFloat(0, 1)),
//                Angle = Mathf.Deg2Rad * 360,
//                Duration = 5f,
//                Delay = Duration / 4,
//                Easing = Easing.StrongOut,
//                Enabled = true,
//                Direction = Rng.NextFloat(-1, 1)
//            };
        }

        public static float3 ExtractPosition(ref float4x4 m)
        {
            return m.c3.xyz;
        }

        public static float3 ExtractScale(ref float4x4 m)
        {
//            return new float3(1,1,1);
            return new float3(
                math.length(m.c0),
                math.length(m.c1),
                math.length(m.c2)
            );
        }

//        public static Quaternion GetRotation(Matrix4x4 m)
//        {
//            Vector3 s = GetScale(m);
// 
//            // Normalize Scale from Matrix4x4
//            float m00 = m[0, 0] / s.x;
//            float m01 = m[0, 1] / s.y;
//            float m02 = m[0, 2] / s.z;
//            float m10 = m[1, 0] / s.x;
//            float m11 = m[1, 1] / s.y;
//            float m12 = m[1, 2] / s.z;
//            float m20 = m[2, 0] / s.x;
//            float m21 = m[2, 1] / s.y;
//            float m22 = m[2, 2] / s.z;
// 
//            Quaternion q = new Quaternion();
//            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m00 + m11 + m22)) / 2;
//            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m00 - m11 - m22)) / 2;
//            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m00 + m11 - m22)) / 2;
//            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m00 - m11 + m22)) / 2;
//            q.x *= Mathf.Sign(q.x * (m21 - m12));
//            q.y *= Mathf.Sign(q.y * (m02 - m20));
//            q.z *= Mathf.Sign(q.z * (m10 - m01));
// 
//            // q.Normalize()
//            float qMagnitude = Mathf.Sqrt(q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z);
//            q.w /= qMagnitude;
//            q.x /= qMagnitude;
//            q.y /= qMagnitude;
//            q.z /= qMagnitude;
// 
//            return q;
//        }
    }
}


[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class RestoreViewSystem : PaintViewSystem<RestoreParentView>
{
    protected override EntityQuery InitQuery()
    {
        return GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ReadOnly<Parent>(),
                ReadOnly<ParentView>(),
                ReadOnly<RestoreParentView>(),
                ReadWrite<Position>(),
                ReadWrite<Rotation>(),
                ReadWrite<Scale>()
            }
        });
    }

    protected override JobHandle scheduleJobs(int jobsCount)
    {
        var data = new NativeParallelHashMap<int, RestoreParentJobData>(jobsCount, Allocator.TempJob);
        var prepareJob = new PrepareRestoreParentJob { Data = data };
        var executeJob = new RestoreParentViewJob { Data = data };
        var handle = prepareJob.Schedule(_query, Dependency);
        handle = executeJob.Schedule(handle);
        return data.Dispose(handle);
    }
    struct RestoreParentJobData
    {
        public float4x4 InvertedParentTransform;
    }

    [BurstCompile]
    partial struct PrepareRestoreParentJob : IJobEntity
    {
        [WriteOnly] public NativeParallelHashMap<int, RestoreParentJobData> Data;

        public void Execute(
            in Parent parent,
            in ParentView parentView,
            in RestoreParentView restoreParent,
            in Position position,
            in Rotation rotation,
            in Scale scale)
        {
            Data.TryAdd(parent.Id, new RestoreParentJobData
            {
                InvertedParentTransform = math.inverse(float4x4.TRS(position.Value, rotation.Value, scale.Value))
            });
        }
    }

    [BurstCompile]
    partial struct RestoreParentViewJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<int, RestoreParentJobData> Data;

        public void Execute(in PixelCube cube, ref ParentId parentId, ref Position position,
            ref Rotation rotation, ref Scale scale)
        {
            // TODO: get rid of ParentViewId and use ParentId.Active(?) instead

            if (!Data.ContainsKey(parentId.Value))
            {
                // if (position.Value.x == 0)
                // {
                //   //  Debug.LogWarning("Parent not found");
                // }

                return;
            }

            RestoreParentJobData data = Data[parentId.Value];


             float4x4 LocalTransform = math.mul(data.InvertedParentTransform,
                 float4x4.TRS(position.Value, rotation.Value, scale.Value));
          //  Debug.Log("Restoring PixelCube : " + cube.Position + " ");
          
            //
            // // TODO: calculate world.z to local.z
            position.Value = LocalTransform.c3.xyz;// new float3(cube.Position.x, cube.Position.y, 0);//serge.c3.xyz;
                 //serge.c3.xyz; //new float3(cube.Position.x - data.Width/2, cube.Position.y - data.Height/2, serge.c3.z);//serge.c3.xyz;
            scale.Value = new float3(1, 1, 1);
            rotation.Value = quaternion.identity;
            parentId.Active = true;
        }
    }
}

