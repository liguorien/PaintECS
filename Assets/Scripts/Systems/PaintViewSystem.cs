
using PaintECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public class PaintViewSystem : JobComponentSystem
{
    #region Main System
  
    private EntityCommandBufferSystem _commandBufferSystem;
    
    protected override void OnCreate()
    {
        _commandBufferSystem = World.Active.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        InitExplodeQueries();
        InitImplodeQueries();
        InitMoveQueries();
        InitRestoreQueries();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var handles = new NativeArray<JobHandle>(4, Allocator.Temp);
        handles[0] = processViewCommand<ExplodeView, ExplodeViewJobData, PrepareExplodeJob, ExplodeJob>(
            inputDependencies, _explodeJobsQuery, ref _explodeData
        );
        handles[1] = processViewCommand<ImplodeView, ImplodeViewJobData, PrepareImplodeViewJob, ImplodeViewJob>(
            inputDependencies, _implodeJobsQuery, ref _implodeData
        );
        handles[2] = processViewCommand<MoveView, MoveViewJobData, PrepareMoveViewJob, MovePaintViewJob>(
            inputDependencies, _moveJobsQuery, ref _moveData
        );
        handles[3] = processViewCommand<RestoreParentView, RestoreParentJobData, PrepareRestoreParentJob, RestoreParentViewJob>(
            inputDependencies, _restoreJobsQuery, ref _restoreData
        );
        
        inputDependencies = JobHandle.CombineDependencies(handles);
        
        handles.Dispose();
        
        return inputDependencies;
    }
    
    
    private JobHandle processViewCommand<T,D,P,J>(JobHandle inputDependencies, EntityQuery query, ref NativeHashMap<int, D> data)  
        where T : struct, IComponentData // component
        where D : struct                 // job data
        where P : struct, IPrepareJob<D> // prepare job
        where J : struct, IExecuteJob<D> // main job
    {
        if (data.IsCreated)
        {
            data.Dispose();
        }
        
        int jobsCount = query.CalculateEntityCount();
        
        if (jobsCount > 0)
        {
            data = new NativeHashMap<int, D>(jobsCount, Allocator.TempJob);

            var prepareJob = new P();
            prepareJob.setData(data.AsParallelWriter());

            var executeJob = new J();
            executeJob.setData(data);

            var purgeJob = new PurgeComponent<T>
            {
                Buffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            
            inputDependencies = JobForEachExtensions.Schedule(prepareJob, query, inputDependencies);
            inputDependencies = JobHandle.CombineDependencies(
                executeJob.Schedule(this, inputDependencies),
                purgeJob.Schedule(this, inputDependencies)
            );
            
            _commandBufferSystem.AddJobHandleForProducer(inputDependencies);
        }

        return inputDependencies;
    }
    
    #endregion

    #region Explode
    
    struct ExplodeViewJobData
    {
        public Easing Easing;
        public float Duration;
        public float Min;
        public float Max;
    }
    

    #region System
    
    private EntityQuery _explodeJobsQuery;
    private NativeHashMap<int, ExplodeViewJobData> _explodeData;

    private void InitExplodeQueries()
    {
        _explodeJobsQuery = GetEntityQuery(
            ComponentType.ReadOnly<ParentView>(),
            ComponentType.ReadOnly<ExplodeView>()
        );
    }

    #endregion

  
    #region Jobs
    [BurstCompile]
    struct PrepareExplodeJob : IJobForEachWithEntity<ParentView, ExplodeView>, IPrepareJob<ExplodeViewJobData>
    {
        [WriteOnly] public NativeHashMap<int, ExplodeViewJobData>.ParallelWriter Data;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref ParentView view,  [ReadOnly] ref ExplodeView explode)
        {
            Data.TryAdd(view.Id, new ExplodeViewJobData
            {
                Min = -(view.Width / 1),
                Max = view.Width / 1,
                Duration = explode.Duration,
                Easing = explode.Easing
            });
        }
        
        public void setData(NativeHashMap<int, ExplodeViewJobData>.ParallelWriter data)
        {
            Data = data;
        }
    }

    [BurstCompile]
    struct ExplodeJob : IJobForEach<ParentId, Position, MoveTo>, IExecuteJob<ExplodeViewJobData>
    {
        [ReadOnly] public Random Rng;
        [ReadOnly] public NativeHashMap<int, ExplodeViewJobData> Data;

        public void Execute([ReadOnly] ref ParentId parentId, [ReadOnly] ref Position position, ref MoveTo moveTo)
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
                Delay = Rng.NextFloat(0.0f, data.Duration/20),
                Duration = data.Duration * 0.95f,
                Elapsed = 0,
                Easing = data.Easing,
                Enabled = true
            };
        }

        public void setData(NativeHashMap<int, ExplodeViewJobData> data)
        {
            Data = data;
            Rng = new Random((uint) UnityEngine.Random.Range(1, uint.MaxValue));
        }
    }
    #endregion
    #endregion
    
    
    #region Implode

    struct ImplodeViewJobData
    {
        public Easing Easing;
        public float Duration;
    }
    
    #region System
    private EntityQuery _implodeJobsQuery;
    private NativeHashMap<int, ImplodeViewJobData> _implodeData;

    private void InitImplodeQueries()
    {
        _implodeJobsQuery = GetEntityQuery(
            ComponentType.ReadOnly<ParentView>(),
            ComponentType.ReadOnly<ImplodeView>()
        );
    }
    #endregion

    #region Jobs
    [BurstCompile]
    struct PrepareImplodeViewJob : IJobForEach<ParentView, ImplodeView>, IPrepareJob<ImplodeViewJobData>
    {
        [WriteOnly] public NativeHashMap<int, ImplodeViewJobData>.ParallelWriter Data;

        public void Execute([ReadOnly] ref ParentView view,  [ReadOnly] ref ImplodeView explode)
        {
            Data.TryAdd(view.Id, new ImplodeViewJobData
            {
                Duration = explode.Duration,
                Easing = explode.Easing
            });
        }

        public void setData(NativeHashMap<int, ImplodeViewJobData>.ParallelWriter data)
        {
            Data = data;
        }
    }
    
    [BurstCompile]
    struct ImplodeViewJob : IJobForEach<ParentId, Position, MoveTo>, IExecuteJob<ImplodeViewJobData>
    {
        [ReadOnly] public NativeHashMap<int, ImplodeViewJobData> Data;

        public void Execute([ReadOnly] ref ParentId parentId, [ReadOnly] ref Position position, ref MoveTo moveTo)
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

        public void setData(NativeHashMap<int, ImplodeViewJobData> data)
        {
            Data = data;
        }
    }
    #endregion
    #endregion
    
    

    #region Move

    #region System
    private EntityQuery _moveJobsQuery;
    private NativeHashMap<int, MoveViewJobData> _moveData;
    
    private void InitMoveQueries()
    {
        _moveJobsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<ParentView>(),
                ComponentType.ReadOnly<MoveView>(),
                ComponentType.ReadWrite<Position>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<Scale>()
            }
        });
    }

    #endregion
    struct MoveViewJobData 
    {
//        public Entity Entity;
//        public ParentView PaintView;
        public float4x4 currentTransform;
        public float4x4 targetTransform;
        public float Duration;
        public Easing Easing;
    }

    [BurstCompile]
    struct PrepareMoveViewJob : IJobForEach<ParentView, MoveView, Position, Rotation, Scale>, IPrepareJob<MoveViewJobData>
    {
        [WriteOnly] NativeHashMap<int, MoveViewJobData>.ParallelWriter Data;
        public void Execute( 
            [ReadOnly] ref ParentView view,
            [ReadOnly] ref MoveView command,
            ref Position position,
            ref Rotation rotation,
            ref Scale scale)
        {
//            Debug.Log("View    Position : " + position.Value + " Rotation=" + rotation.Value + " Scale=" + scale.Value);
//            Debug.Log("command Position : " + command.Position + " Rotation=" + command.Rotation + " Scale=" + command.Scale);
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

        public void setData(NativeHashMap<int, MoveViewJobData>.ParallelWriter data)
        {
            Data = data;
        }
    }


    [BurstCompile]
    struct MovePaintViewJob : IJobForEach<PixelCube, Position, Rotation, Scale, MoveTo, ParentId>, IExecuteJob<MoveViewJobData>
    {
        [ReadOnly] public NativeHashMap<int, MoveViewJobData> Data;
    
        
        [ReadOnly] public Random Rng;
        

        public void Execute(
            [ReadOnly] ref PixelCube cube, 
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
                
//                if (cube.Position.x == 10 && cube.Position.y == 10)
//                {
//                    Debug.Log("data.currentTransform=" + data.currentTransform.c3.xyz);
//                    Debug.Log("cube.position=" + position.Value);
//                    Debug.Log("cube.target=" + targetWorldTransform.c3.xyz);
//                }
            }

//            position.Value = targetWorldTransform.c3.xyz;

            moveTo = new MoveTo
            {
                Origin = position.Value,
                Destination = ExtractPosition(ref targetWorldTransform),
                Delay = Rng.NextFloat(0.0f, data.Duration*0.25f),
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

        public void setData(NativeHashMap<int, MoveViewJobData> data)
        {
            Data = data;
            Rng = new Random((uint) UnityEngine.Random.Range(1, uint.MaxValue));
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

    #endregion

    #region Restore

    private EntityQuery _restoreJobsQuery;
    private NativeHashMap<int, RestoreParentJobData> _restoreData;
//    private EntityQuery restoreViewQuery;

    private void InitRestoreQueries()
    {
        _restoreJobsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Parent>(),
                ComponentType.ReadOnly<ParentView>(),
                ComponentType.ReadOnly<RestoreParentView>(),
                ComponentType.ReadWrite<Position>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<Scale>()
            }
        });
    }

    struct RestoreParentJobData
    {
        public float4x4 InvertedParentTransform;
    }
   
    [BurstCompile]
    struct PrepareRestoreParentJob : IJobForEach<Parent, ParentView, RestoreParentView, Position, Rotation,Scale>, IPrepareJob<RestoreParentJobData>
    {
        [WriteOnly] public NativeHashMap<int,RestoreParentJobData>.ParallelWriter Data;

        public void Execute(
            [ReadOnly] ref Parent parent,
            [ReadOnly] ref ParentView parentView, 
            [ReadOnly] ref RestoreParentView restoreParent, 
            ref Position position,
            ref Rotation rotation, 
            ref Scale scale)
        {
            Data.TryAdd(parent.Id, new RestoreParentJobData
            {
                InvertedParentTransform = math.inverse(float4x4.TRS(position.Value, rotation.Value, scale.Value))
            });
        }

        public void setData(NativeHashMap<int, RestoreParentJobData>.ParallelWriter data)
        {
            Data = data;
        }
    }
    
    [BurstCompile]
    struct RestoreParentViewJob : IJobForEach<PixelCube, ParentId, Position, Rotation, Scale>, IExecuteJob<RestoreParentJobData>
    {
 
        [ReadOnly] public NativeHashMap<int, RestoreParentJobData> Data;
        
        public void Execute([ReadOnly] ref PixelCube cube, ref ParentId parentId, ref Position position,
            ref Rotation rotation, ref Scale scale)
        {
            // TODO: get rid of ParentViewId and use ParentId.Active(?) instead
               
            if (!Data.ContainsKey(parentId.Value))
            {
                return;
            }
            
            RestoreParentJobData data = Data[parentId.Value];
            
            
            float4x4 serge = math.mul(data.InvertedParentTransform, float4x4.TRS(position.Value, rotation.Value, scale.Value));
           // Debug.Log("Restoring PixelCube : " + ParentId + " ");
            parentId.Active = true;
            
            // TODO: calculate world.z to local.z
            position.Value = serge.c3.xyz;//new float3(cube.Position.x - data.Width/2, cube.Position.y - data.Height/2, serge.c3.z);//serge.c3.xyz;
            scale.Value = new float3(1, 1, 1);
            rotation.Value = quaternion.identity;
        }

        public void setData(NativeHashMap<int, RestoreParentJobData> data)
        {
            Data = data;
        }
    }


    #endregion
    
    
    #region Interfaces
    interface IPrepareJob<T> : JobForEachExtensions.IBaseJobForEach where T : struct
    {
        void setData(NativeHashMap<int, T>.ParallelWriter data);
    }
    
    interface IExecuteJob<T> : JobForEachExtensions.IBaseJobForEach where T : struct
    {
//        NativeHashMap<int, T> Data { get; set; }
        void setData(NativeHashMap<int, T> data);
    }
    #endregion
}