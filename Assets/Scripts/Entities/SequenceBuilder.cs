using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PaintECS.Entities
{
    public class SequenceBuilder
    {
        private static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private static FixedList64Bytes<float> Wrap(params float[] values)
        {
            var wrapper = new FixedList64Bytes<float>();
            foreach (var value in values)
            {
                wrapper.Add(value);
            }
            return wrapper;
        }
        
        public static SequenceBuilder Create()
        {
            return new SequenceBuilder();
        }
        
        public static SequenceBuilder FromEntity(Entity entity)
        {
            return new SequenceBuilder(entity);
        }

        private List<SequenceCommand> _commands = new List<SequenceCommand>();

        private Entity _entity;
        
        public SequenceBuilder(Entity entity)
        {
            _entity = entity;
        }
        
        public SequenceBuilder()
        {
            
        }

        public SequenceBuilder MoveView(float delay, float duration, Easing easing, float3 position, quaternion rotation, float3 scale){
            
            var data = new FixedList64Bytes<float>();
            
            // position
            data.Add(position.x);
            data.Add(position.y);
            data.Add(position.z);
            
            //rotation
            data.Add(rotation.value.x);
            data.Add(rotation.value.y);
            data.Add(rotation.value.z);
            data.Add(rotation.value.w);
            
            //scale
            data.Add(scale.x);
            data.Add(scale.y);
            data.Add(scale.z);
            
            // easing
            data.Add((float)easing);
            
            _commands.Add(new SequenceCommand
            {
                Type = CommandType.Move,
                Delay = delay,
                Duration = duration,
                Data = data
            });
            return this;
        }
        
        public SequenceBuilder Explode(float delay, float duration, Easing easing=Easing.Linear) {
            
            _commands.Add(new SequenceCommand
            {
                Type = CommandType.Explode,
                Delay = delay,
                Duration = duration,
                Data = Wrap((float)easing)
            });
            return this;
        }
        
        public SequenceBuilder Implode(float delay, float duration, Easing easing=Easing.Linear) {
            
            _commands.Add(new SequenceCommand
            {
                Type = CommandType.Implode,
                Delay = delay,
                Duration = duration,
                Data = Wrap((float)easing)
            });
            return this;
        }
        
        public SequenceBuilder RestoreParent(float delay, float duration, float3 position) {
            
            _commands.Add(new SequenceCommand
            {
                Type = CommandType.RestoreParent,
                Delay = delay,
                Duration = duration,
                Data = Wrap(position.x, position.y, position.z)
            });
            return this;
        }
        
        public SequenceBuilder RestoreParent()
        {
            return RestoreParent(0.5f, 0.5f, float3.zero);
        }

        public Entity Build()
        {
            if (!EntityManager.Exists(_entity))
            {
                _entity = EntityManager.CreateEntity();
            }

            if (EntityManager.HasComponent<Sequence>(_entity))
            {
                EntityManager.SetComponentData(_entity, new Sequence
                {
                    Index = 0,
                    Size = _commands.Count
                });
            }
            else
            {
                EntityManager.AddComponentData(_entity, new Sequence
                {
                    Index = 0,
                    Size = _commands.Count
                });
            }

//            if(EntityManager.GetBuffer<>())
            DynamicBuffer<SequenceCommand> buffer = EntityManager.AddBuffer<SequenceCommand>(_entity);
            buffer.Clear();
            foreach (SequenceCommand cmd in _commands)
            {
                buffer.Add(cmd);
            }
            return _entity;
        }
    }
}