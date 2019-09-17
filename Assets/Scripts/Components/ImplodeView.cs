using Unity.Collections;
using Unity.Entities;

namespace PaintECS
{
    public struct ImplodeView : IComponentData
    {
        public float Duration;
        public Easing Easing;

        
        public static ImplodeView fromData(float duration, ResizableArray64Byte<float> data)
        {
            return new ImplodeView
            {
                Duration = duration,
                Easing = data.Length == 0 ? Easing.Linear : (Easing)data[0]
            };
        }
    }
}