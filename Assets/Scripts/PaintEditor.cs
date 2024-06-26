using System;
using PaintECS.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace PaintECS
{
    public class PaintEditor : MonoBehaviour
    {
        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public int width;

        public int height;

        public Vector2Int ImageOffset;

        //public GameObject cubePrefab;
        public Mesh mesh;
        public Material material;

        //private Entity[,] _cubes;
        private Parent _parent;
        private ParentId _parentId;
        private ParentView _parentView;
        private Entity _entity;
        private bool _paintEditorEnabled;
        public Texture2D sourceTexture;

        [Header("Explode")]
        public float ExplodeDuration;
        public Easing ExplodeEasing;
        
        [Header("Implode")]
        public float ImplodeDuration;
        public Easing ImplodeEasing;
        
        [Header("Move")]
        public float MoveDuration;
        public Easing MoveEasing;
        
        private readonly float3[] _positions =
        {
            new float3(0, 0, 0),
            new float3(200, 0, 0),
            new float3(-200, 0, 0),
            //new float3(300, -300, 130)
        }; 

        private EntityQuery _cubeQuery;
        public int viewIndex;

        private void OnDrawGizmos()
        {
            Gizmos.DrawCube(transform.position + new Vector3(width*0.5f, height*0.5f, 0), new Vector3(width, height, 1));
        }

        void Update()
        {
            if (!_paintEditorEnabled)
            {
                Debug.Log("Paint Editor Disabled");
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                EntityManager.AddComponentData(_entity, new ExplodeView {Duration = ExplodeDuration, Easing = ExplodeEasing});
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                EntityManager.AddComponentData(_entity, new ImplodeView {Duration = ImplodeDuration, Easing = ImplodeEasing});
            }
            else if (Input.GetKeyDown(KeyCode.LeftControl))
            {

                viewIndex++;
                if (viewIndex >= _positions.Length)
                {
                    viewIndex = 0;
                }

                SequenceBuilder
                    .FromEntity(_entity)
                    .Explode(0, ExplodeDuration, Easing.StrongIn)
                    .MoveView(0f, MoveDuration, MoveEasing,
                        _positions[viewIndex],
                        quaternion.identity,
                        new float3(1, 1, 1)
                    )
                    .RestoreParent()
                    .Implode(0, ImplodeDuration, Easing.StrongOut)
                    .Build();
            }
        }


        void Start()
        {
          _entity = PaintViewBuilder.Create()
                .SetSize(width, height)
                .SetTexture(sourceTexture)
                .SetImageOffset(ImageOffset)
                .FromTransform(transform)
                .SetRenderData(mesh, material)
                .Build();
            
            _paintEditorEnabled = true;
        }
    }
}