using Unity.Entities;

namespace PaintECS
{
    public struct Parent : IComponentData
    {
        private static int m_idSerial = 0;

        public static Parent Create()
        {
            return new Parent
            {
                Id = System.Threading.Interlocked.Increment(ref m_idSerial)
            };
        }
        
        
        public int Id;

        public ParentId ParentId()
        {
            return new ParentId{Value = Id, Active = true};
        }
    }
}