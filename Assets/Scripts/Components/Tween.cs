using Unity.Entities;

namespace PaintECS
{
    public interface ITween
    {
        float Delay { get; set; }
        float Duration { get; set; }
        float Elapsed { get; set; }
        Easing Easing { get; set; }
        bool Enabled { get; set; }
    }
}