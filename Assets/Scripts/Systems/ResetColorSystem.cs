using Unity.Entities;
using UnityEngine;

namespace PaintECS
{
    public partial class ResetColorSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.RightControl))
            {
                Dependency = new ResetColorJob().Schedule(Dependency);
            }
        }

        partial struct ResetColorJob : IJobEntity
        {
            public void Execute(ref RenderColor c0)
            {
                c0.Value = Color.blue;
            }
        }
    }
}