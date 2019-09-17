using Unity.Collections;
using Unity.Entities;

namespace PaintECS
{

//    public interface ICommandComponent : IComponentData
//    {
//        void 
//    }
    
    public struct Sequence : IComponentData
    {
        public int Index;
        public int Size;
        public bool Created;
        public float ElapsedTime;
    }

    public enum CommandType
    {
        Explode,
        Implode,
        Move,  
        RestoreParent,
        Rotate
    }

    public struct SequenceCommand : IBufferElementData
    {
        public CommandType Type;
        public float Delay;
        public float Duration;
        public ResizableArray64Byte<float> Data;

    }
    
    
//    public struct Stuff : IBufferElementData
//    {
//        public float3 Value;
//    }
}