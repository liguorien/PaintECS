using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS
{
    public class SequenceSystem : JobComponentSystem
    {

        private EntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.Active.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            inputDependencies = new SergeJob
            {
                deltaTime = Time.deltaTime,
                SequenceCommandBuffers = GetBufferFromEntity<SequenceCommand>(),
                EntityCommandBuffer = _commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, inputDependencies);

            _commandBufferSystem.AddJobHandleForProducer(inputDependencies);
           
            return inputDependencies;
        }
    }
    
//    [BurstCompile(CompileSynchronously = false)]
    struct SergeJob : IJobForEachWithEntity<Sequence>
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity <SequenceCommand> SequenceCommandBuffers;
        
        public float deltaTime;

        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        
        public void Execute(Entity entity, int index, ref Sequence sequence)
        {
        //    Debug.Log("command : "+ sequence.Index +"/" + sequence.Size);
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
                        createCommandEntity(index, entity, ref sequence, ref command);
                    }
                }
                else
                {
                    createCommandEntity(index, entity, ref sequence, ref command);
                }
            }
            
            if (command.Delay > 0 && !sequence.Created && sequence.ElapsedTime >= command.Delay)
            {
                createCommandEntity(index, entity, ref sequence, ref command);
            }
            

            if (sequence.ElapsedTime >= command.Delay + command.Duration)
            {
                sequence.Index++;
                sequence.ElapsedTime = 0;//(command.Delay + command.Duration);
                sequence.Created = false;
            }
            
            if (sequence.Index >= sequence.Size)
            {
                EntityCommandBuffer.RemoveComponent<Sequence>(index, entity);
            }
        }
        
        void createCommandEntity(int index, Entity entity, ref Sequence sequence, ref SequenceCommand command){
            switch (command.Type)
            {
                case CommandType.Explode :
                    EntityCommandBuffer.AddComponent(index, entity, ExplodeView.fromData(command.Duration, command.Data));
                    break;
                case CommandType.Implode :
                    EntityCommandBuffer.AddComponent(index, entity, ImplodeView.fromData(command.Duration,command.Data));
                    break;
                case CommandType.Move :
                    EntityCommandBuffer.AddComponent(index, entity, MoveView.fromData(command.Duration, command.Data));
                    break;
                case CommandType.RestoreParent :
                    EntityCommandBuffer.AddComponent(index, entity, RestoreParentView.fromData(command.Data));
                    break;
            }
        
            sequence.Created = true;
        }
       
    }
    
   
}