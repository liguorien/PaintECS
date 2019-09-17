using Unity.Entities;

namespace PaintECS
{

    public struct ParentViewId : ISharedComponentData
    {
        public int Value;
    }
    
    public struct ParentView : IComponentData
    {
        public int Id;
        public int Width;
        public int Height;
        
        public ParentViewId ParentViewId()
        {
            return new ParentViewId{Value = Id};
        }

    }
}