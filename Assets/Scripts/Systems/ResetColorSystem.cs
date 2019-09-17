using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace PaintECS
{
    public class ResetColorSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (Input.GetKeyDown(KeyCode.RightControl))
            {
                return new ResetColorJob().Schedule(this, inputDeps);
            }
            
            return inputDeps;
        }

        struct ResetColorJob : IJobForEach<RenderColor>
        {
            public void Execute(ref RenderColor c0)
            {
                c0.Value = Color.blue;
            }
        }
    }
}