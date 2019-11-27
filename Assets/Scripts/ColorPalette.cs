using PaintECS;
using PaintECS.Entities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DefaultNamespace
{
    public class ColorPalette : MonoBehaviour
    {
      

        public int width;

        public int height;

        //public GameObject cubePrefab;
        public Mesh mesh;
        public Material material;

        //private Entity[,] _cubes;
        private Parent _parent;
        private ParentId _parentId;
        private ParentView _parentView;
        private Entity _entity;
        private bool _paintEditorEnabled;
    
      
        private EntityQuery _cubeQuery;
      
        private void OnDrawGizmos()
        {
            Gizmos.DrawCube(transform.position + new Vector3(width*0.5f, height*0.5f, 0), new Vector3(width, height, 1));
        }

        void Update()
        {
           
        }


        void Start()
        {
          _entity = PaintViewBuilder.Create()
                .SetSize(width, height)
                .SetColorSource(new PaletteColorSource(width, height))
                .FromTransform(transform)
                .SetRenderData(mesh, material)
                .Build();
            
            _paintEditorEnabled = true;
        }
    }

    struct PaletteColorSource : IColorSource
    {
        private float _width;
        private float _height;
        private float _diag;
        
        public PaletteColorSource(int width, int height)
        {
            _width = width;
            _height = height;
            _diag = math.distance(float2.zero, new float2(_width, _height));
        }

        public Color getColor(int index)
        {
            float x = index % _width;
            float y = math.floor(index / _width);
          
            var result = new Color(x/_width, y/_width, 1 - math.distance(new float2(x,y), new float2(_width,_height)) / _diag, 1f);
            
            
            //Debug.Log(index + "   x=" + x + " y=" + y + " color=" + result);

            return result;
        }
    }
}