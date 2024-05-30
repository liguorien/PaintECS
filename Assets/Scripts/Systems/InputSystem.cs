using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InputSystem : ISystem
    {
        private Vector3 _lastMousePosition;

        public void OnUpdate(ref SystemState state)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _lastMousePosition = GetMouseWorldPosition();
            }
            else if (Input.GetMouseButton(0))
            {
                Vector3 mousePosition = GetMouseWorldPosition();

                var commandBufferSystem = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
                var commandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged);
                var hitList = new NativeQueue<Hit>(Allocator.TempJob);

                var captureInputJob = new CaptureInputJob
                {
                    hitList = hitList.AsParallelWriter(),
                    mousePosition = mousePosition,
                    lastMousePosition = _lastMousePosition
                };
                
                var processInputJob = new ProcessInputJob
                {
                    HitList = hitList,
                    Buffer = commandBuffer
                };

                state.Dependency = processInputJob.Schedule(
                    captureInputJob.ScheduleParallel(state.Dependency)
                );

                hitList.Dispose(state.Dependency);
                
                _lastMousePosition = mousePosition;
            }
        }
        

        Vector3 GetMouseWorldPosition()
        {
            return Camera.main.ScreenToWorldPoint(
                new Vector3(
                    Input.mousePosition.x - 0.5f,
                    Input.mousePosition.y - 0.5f,
                    -Camera.main.transform.position.z
                )
            );
        }
        
        struct Hit
        {
            public Entity entity;
            public RotateTo rotateTo;
        }

        [BurstCompile]
        struct ProcessInputJob : IJob
        {
            public NativeQueue<Hit> HitList;

            public EntityCommandBuffer Buffer;
            
            public void Execute()
            {
                while (HitList.Count > 0)
                {
                    Hit hit = HitList.Dequeue();
                    Buffer.SetComponent(hit.entity, new RenderColor{Value=Color.red});
                    Buffer.SetComponent(hit.entity, hit.rotateTo);
                }
            }
        }

        
        [BurstCompile]
        partial struct CaptureInputJob : IJobEntity
        {

            [WriteOnly] public NativeQueue<Hit>.ParallelWriter hitList;

            [ReadOnly] public Vector3 lastMousePosition; // current=xy last=zw
            [ReadOnly] public Vector3 mousePosition; // current=xy last=zw

            public void Execute(in Entity entity, ref MouseInteractive cube,
                [ReadOnly] ref WorldTransform position)
            {
                float x = position.Value.c3.x;
                float y = position.Value.c3.y;

                
                // TODO: test if current mouse position is inside bounds
                
                bool hitTest = lineIntersects(x - 0.5f, y - 0.5f, x + 0.5f, y - 0.5f)
                               || lineIntersects(x - 0.5f, y - 0.5f, x - 0.5f, y + 0.5f)
                               || lineIntersects(x + 0.5f, y - 0.5f, x + 0.5f, y + 0.5f)
                               || lineIntersects(x - 0.5f, y + 0.5f, x + 0.5f, y + 0.5f);

                if (hitTest)
                {
                    if (cube.MouseOver)
                    {
                        return;
                    }

                    cube.MouseOver = true;


                    float3 axis;
                    float direction = 1;
                    if (math.abs(lastMousePosition.x - mousePosition.x) >
                        math.abs(lastMousePosition.y - mousePosition.y))
                    {
                        axis = new float3(0.0f, 1.0f, 0.0f);
                        if (lastMousePosition.x - mousePosition.x < 0)
                        {
                            direction = -1;
                        }
                    }
                    else
                    {
                        axis = new float3(1.0f, 0.0f, 0.0f);
                        if (lastMousePosition.y - mousePosition.y > 0)
                        {
                            direction = -1;
                        }
                    }

                    hitList.Enqueue(new Hit
                    {
                        entity = entity,
                        rotateTo = new RotateTo
                        {
                            Axis = axis,
                            Angle =  Mathf.Deg2Rad * 90,
                            Duration = 1.5f,
                            Easing = Easing.StrongOut,
                            Enabled = true,
                            Direction = direction
                        }
//                        rotateTo = new CubeRotation
//                        {
//                        Axis = axis,
//                        AngleTotal = Mathf.Deg2Rad * 90,
//                        AngleRotated = 0,
//                        Speed = 3.5f,
//                        Direction = direction
//                    }
                    });
                    
                }
                else
                {
                    cube.MouseOver = false;
                }
            }

            bool lineIntersects(float x3, float y3, float x4, float y4)
            {
                


                float x1 = lastMousePosition.x;
                float y1 = lastMousePosition.y;
                float x2 = mousePosition.x;
                float y2 = mousePosition.y;

                // calculate the distance to intersection point
                float uA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / 
                           ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
                
                if (uA < 0 || uA > 1)
                {
                    return false;
                }

                float uB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / 
                           ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

                return uB >= 0 && uB <= 1;
            }
        }
    
    }
    
    
   
}