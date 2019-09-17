using Unity.Entities;
using UnityEngine;

namespace PaintECS
{
    public class CameraPositionSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity id, ref CameraController controller, ref Position position) =>
            {
                if (controller.Active)
                {
                    Debug.Log("position : " + position.Value + " offset=" + controller.Offset);
                    Camera.main.transform.position = position.Value + controller.Offset;
                }
            });
        }
    }
}