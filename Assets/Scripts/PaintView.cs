using PaintECS.Entities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace PaintECS
{
    public class PaintView : MonoBehaviour
    {
        private EntityManager EntityManager => World.Active.EntityManager;

        public int width;

        public int height;
        //public GameObject cubePrefab;


        public Entity[,] _cubes;
        public Mesh mesh;
        public Material material;

        private Entity _entity;
        private float rotationSpeed;
        private bool serged = false;
        private ParentId _parentId;
        private ParentView _parentView;
        void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                transform.Rotate(math.up(), 500f * Time.deltaTime);
                EntityManager.SetComponentData(_entity, new Rotation{Value = transform.rotation});
            }
            if (serged)
            {
//                if (!Input.GetKey(KeyCode.LeftShift))
//                {
//                    serged = false;
////                    EntityManager.SetComponentData(_parentId);
//                    EntityManager.AddComponentData(_entity, new RestoreParentView
//                    {
//                        Position = new float3(32,32,0)
//                    });
////                    for (int x = 0; x < width; x++)
////                    {
////                        for (int y = 0; y < height; y++)
////                        {
////                            EntityManager.SetComponentData(_cubes[x,y], _parentId);
////                        }
////                    }
//                }
                
            }else if (Input.GetKeyDown(KeyCode.LeftShift))
            {
//                serged = true;
                
//                Debug.Log("Lorne : " + EntityManager.GetComponentData<ParentId>(_cubes[0,0]).Value);
                
//                for (int x = 0; x < width; x++)
//                {
//                    for (int y = 0; y < height; y++)
//                    {
//                        EntityManager.SetComponentData(_cubes[x,y], new ParentId{Value = 0});
//                    }
//                }
                _newPosition = new Vector3(
                    Random.Range(-100, 100),
                    0,//Random.Range(-100, 100),
                    Random.Range(50, 200));

//Explode();

                var sequence = SequenceBuilder.FromEntity(_entity);

//                sequence.Explode(0, 1);

                for (int i = 0; i < 100; i++)
                {
                    _newPosition = new Vector3(
                        Random.Range(-50, 100),
                        Random.Range(-50, 100),
                        Random.Range(50, 100));

                    sequence
//                        .Explode(0f, 1.55f)
                        .MoveView(0, 1.0f, Easing.Linear,
                            _newPosition,
                            quaternion.Euler(0, 0, 0),
                            new float3(1, 1, 1)
                        )
                        .RestoreParent(0.05f, 0.05f, _newPosition);
//                        .Implode(0f, 0.15f);

//                    sequence
//                        .Explode(0, 0.3f)
//                        .Implode(0, 0.3f)
//                        .Explode(0, 0.3f);
                }
                    
                sequence.Explode(0, 2.3f)
                    .Implode(0, 1.0f)
                    .Explode(0, 1.0f)
                    .Implode(0, 1.0f)
                    .Explode(0, 1.0f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f).Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 0.5f)
                    .Explode(0, 0.5f)
                    .Implode(0, 2.3f)
                    .Explode(0, 2.3f)
                    .Implode(0, 2.3f)
                    .Explode(0, 2.3f)
                    .Implode(0, 2.3f)
                    .Explode(0, 2.3f)
                    .Implode(0, 2.3f)
                    .Explode(0, 2.3f)
                    .Implode(0, 2.3f)//    .Build();
                    .MoveView(5.3f, 1.0f, Easing.StrongOut,
                        _newPosition,
                        quaternion.Euler(0, 0, 0),
                        new float3(1, 1, 1)
                    )
                    .RestoreParent(0.1f, 0.1f, _newPosition)
                    .Implode(0.3f, 1.0f)
                    .Build();

//  Explode();
//                Invoke("Explode", Random.Range(0, 3));
//                Explode();
            }
            
//            if (Input.GetKeyDown(KeyCode.LeftShift))
//            {
//                rotationSpeed = Random.Range(-500, 500);
//            }
//            transform.Rotate(math.up(), rotationSpeed * Time.deltaTime);
//            EntityManager.SetComponentData(_entity, new Rotation
//            {
//                Value = transform.rotation 
//            });
//            EntityManager.SetComponentData(_entity, new Position
//            {
//                Value = transform.position 
//            });
//            EntityManager.SetComponentData(_entity, new Scale
//            {
//                Value = transform.localScale
//            });
        }

        void Explode(float duration)
        {
            serged = true;
            Debug.Log("Explode");

            EntityManager.AddComponentData(_entity, new ExplodeView{Duration = duration});
                
            Invoke("MoveToNewLocation", 2.0f);
        }
        
        void Implode(float duration)
        {
//            serged = false;
            Debug.Log("Implode");
            EntityManager.AddComponentData(_entity, new ImplodeView{Duration = duration});
                
            Invoke("Explode", 3.0f);
        }

        private Vector3 _newPosition;
        
        void MoveToNewLocation()
        {
            Debug.Log("MoveView");
            _newPosition = new Vector3(
                Random.Range(-100, 100),
                0,//Random.Range(-100, 100),
                Random.Range(50, 200));


            EntityManager.AddComponentData(_entity, new MoveView
            {
                Position = _newPosition,
                Rotation = transform.rotation,
                Scale = transform.localScale
            });

            Invoke("RestoreParent", 2.8f);
        }

        void RestoreParent()
        {
            Debug.Log("RestoreParent");
          //  serged = false;
            EntityManager.AddComponentData(_entity, new RestoreParentView
            {
                Position = _newPosition
            });
//            EntityManager.SetComponentData(_entity, new Position {Value = _newPosition});
//            transform.position = _newPosition;
//            
//            for (int x = 0; x < width; x++)
//            {
//                for (int y = 0; y < height; y++)
//                {
//                    EntityManager.SetComponentData(_cubes[x,y], _parentId);
//                    EntityManager.SetComponentData(_cubes[x,y], new Position {Value = new float3(x-width/2, y-height/2, 0)});
//                    EntityManager.SetComponentData(_cubes[x,y], new Rotation{Value = quaternion.Euler(0,0,0)});
//                    EntityManager.SetComponentData(_cubes[x,y], new Scale{Value = new float3(1,1,1)});
//                }
//            }
            
            Invoke("Explode", 3.5f);
        }
        
        public Vector3 GetPosition(Matrix4x4 m)
        {
            return new Vector3(m[0, 3], m[1, 3], m[2, 3]);
        }

        public Vector3 GetScale(Matrix4x4 m)
        {
            return new Vector3
                (m.GetColumn(0).magnitude, m.GetColumn(1).magnitude, m.GetColumn(2).magnitude);
        }
 
        public Quaternion GetRotation(Matrix4x4 m)
        {
            Vector3 s = GetScale(m);
 
            // Normalize Scale from Matrix4x4
            float m00 = m[0, 0] / s.x;
            float m01 = m[0, 1] / s.y;
            float m02 = m[0, 2] / s.z;
            float m10 = m[1, 0] / s.x;
            float m11 = m[1, 1] / s.y;
            float m12 = m[1, 2] / s.z;
            float m20 = m[2, 0] / s.x;
            float m21 = m[2, 1] / s.y;
            float m22 = m[2, 2] / s.z;
 
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m00 + m11 + m22)) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m00 - m11 - m22)) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m00 + m11 - m22)) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m00 - m11 + m22)) / 2;
            q.x *= Mathf.Sign(q.x * (m21 - m12));
            q.y *= Mathf.Sign(q.y * (m02 - m20));
            q.z *= Mathf.Sign(q.z * (m10 - m01));
 
            // q.Normalize()
            float qMagnitude = Mathf.Sqrt(q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z);
            q.w /= qMagnitude;
            q.x /= qMagnitude;
            q.y /= qMagnitude;
            q.z /= qMagnitude;
 
            return q;
        }
        
        void Start()
        {
            Profiler.BeginSample("PaintView.Start");
            rotationSpeed = Random.Range(-50, 50);
            
            _cubes = new Entity[width, height];

            var parent = Parent.Create();

            _parentId = parent.ParentId();
            _parentView = new ParentView
            {
                Id = parent.Id,
                Width = width,
                Height = height
            };
            
            var renderData = new RenderData
            {
                Mesh = mesh,
                Material = material
            };
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var color =Color.magenta;
                    if (Random.Range(1, 100) > 50)
                    {
                        if (Random.Range(1, 100) > 50)
                        {
                            color = Color.green;
                        }
                        else
                        {
                            color = Color.yellow;
                        }
                    }
                    else
                    {
                        if (Random.Range(1, 100) > 50)
                        {
                            color = Color.red;
                        }
                        else
                        {
                            color = Color.blue;
                        }
                    }

                    
                    _cubes[i, j] = EntityBuilder.Create()
                        .Add(_parentId)
                        .AddShared(renderData)
                        .AddShared(_parentView.ParentViewId())
                        .Add(new PixelCube{Position = new int2(i, j)})
                        .Add(new Position {Value = new float3(i-width/2, j-height/2, 0)})
                        .Add(new Rotation {Value = quaternion.Euler(0, 0, 0)})
                        .Add(new Scale{Value = new float3(1,1,1)})
                        .Add(new RenderColor {Value = color})
                        .Add(new MoveTo())
                        .Build();
                    
                    
                }
            }
            
            //creates parent entity
            _entity = EntityBuilder.Create()
                .Add(parent)
                .Add(new Position {Value = transform.position})
                .Add(new Rotation {Value = transform.rotation})
                .Add(new Scale{Value = transform.localScale})
                .Add(new ParentView{Width = width, Height = height, Id = _parentId.Value})
                .Build();

            Profiler.EndSample();
            
//            World.Active.GetOrCreateSystem<RenderSystem>().initialize(new RenderSystem.RenderConfig
//            {
//                material = material, //cubePrefab.GetComponent<MeshRenderer>().sharedMaterial,
//                mesh = mesh //cubePrefab.GetComponent<MeshFilter>().sharedMesh
//            });
        }
    }
}