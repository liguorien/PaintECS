using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PaintECS
{
    public struct ExplodeView : IComponentData
    {
        public float Duration;
        public Easing Easing;

        public static ExplodeView fromData(float duration, FixedList64Bytes<float> data)
        {
            return new ExplodeView
            {
                Duration = duration,
                Easing = data.Length == 0 ? Easing.Linear : (Easing) data[0]
            };
        }
    }
}