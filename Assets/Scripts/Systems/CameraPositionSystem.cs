using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace PaintECS
{
    public partial struct CameraPositionSystem : ISystem
    {
      
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (controller, position) in
                     SystemAPI.Query<RefRO<CameraController>, RefRO<Position>>())
            {
                if (controller.ValueRO.Active)
                {
                    Camera.main.transform.position = position.ValueRO.Value + controller.ValueRO.Offset;
                }
            };
        }
    }
}