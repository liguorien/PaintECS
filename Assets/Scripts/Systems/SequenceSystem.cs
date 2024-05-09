using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SequenceSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SergeJob
            {
                deltaTime = Time.deltaTime,
                SequenceCommandBuffers = SystemAPI.GetBufferLookup<SequenceCommand>(),
                EntityCommandBuffer = SystemAPI
                    .GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
            }.Schedule(state.Dependency);
        }
    }
    
    [BurstCompile]
    partial struct SergeJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public BufferLookup <SequenceCommand> SequenceCommandBuffers;
        
        public float deltaTime;

        public EntityCommandBuffer EntityCommandBuffer;
        
        public void Execute(Entity entity, ref Sequence sequence)
        {
            //Debug.Log("command : "+ sequence.Index +"/" + sequence.Size);
            if (sequence.Index >= sequence.Size)
            {
                return;
            }
            
            DynamicBuffer<SequenceCommand> commands = SequenceCommandBuffers[entity];
            SequenceCommand command = commands[sequence.Index];
            
            sequence.ElapsedTime += deltaTime;
            
            if (!sequence.Created)
            {
                if (command.Delay > 0)
                {
                    if (sequence.ElapsedTime >= command.Delay)
                    {
                        createCommandEntity(entity, ref sequence, ref command);
                    }
                }
                else
                {
                    createCommandEntity(entity, ref sequence, ref command);
                }
            }
            
            if (command.Delay > 0 && !sequence.Created && sequence.ElapsedTime >= command.Delay)
            {
                createCommandEntity(entity, ref sequence, ref command);
            }
            

            if (sequence.ElapsedTime >= command.Delay + command.Duration)
            {
                sequence.Index++;
                sequence.ElapsedTime = 0;//(command.Delay + command.Duration);
                sequence.Created = false;
            }
            
            if (sequence.Index >= sequence.Size)
            {
                EntityCommandBuffer.RemoveComponent<Sequence>(entity);
            }
        }
        
        void createCommandEntity(Entity entity, ref Sequence sequence, ref SequenceCommand command){
            switch (command.Type)
            {
                case CommandType.Explode :
                    EntityCommandBuffer.AddComponent(entity, ExplodeView.fromData(command.Duration, command.Data));
                    break;
                case CommandType.Implode :
                    EntityCommandBuffer.AddComponent(entity, ImplodeView.fromData(command.Duration,command.Data));
                    break;
                case CommandType.Move :
                    EntityCommandBuffer.AddComponent(entity, MoveView.fromData(command.Duration, command.Data));
                    break;
                case CommandType.RestoreParent :
                    EntityCommandBuffer.AddComponent(entity, RestoreParentView.fromData(command.Data));
                    break;
            }
        
            sequence.Created = true;
        }
       
    }
    
   
}