using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace PaintECS
{

    
    
    public class PaintViewBuilder
    {
        public static PaintViewBuilder Create()
        {
            return new PaintViewBuilder();
        }

        private float3 _position = float3.zero;
        private quaternion _rotation = quaternion.identity;
        private float3 _scale = new float3(1,1,1);

        private IColorSource _colorSource;

        private int _width = 64;
        private int _height = 64;

        private Mesh _mesh;
        private Material _material;
        
        private Texture2D _sourceTexture;


        public PaintViewBuilder SetSize(int width, int height)
        {
            _width = width;
            _height = height;
            return this;
        }
        
        public PaintViewBuilder SetRenderData(Mesh mesh, Material material)
        {
            _mesh = mesh;
            _material = material;
            return this;
        }
        
        public PaintViewBuilder FromTransform(Transform transform)
        {
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
            return this;
        }
        
        
        public PaintViewBuilder SetPosition(float3 value)
        {
            _position = value;
            return this;
        }
        
        public PaintViewBuilder SetRotation(quaternion value)
        {
            _rotation = value;
            return this;
        }

        
        public PaintViewBuilder SetScale(float3 value)
        {
            _scale = value;
            return this;
        }


        public PaintViewBuilder SetTexture(Texture2D texture)
        {
            _colorSource = new TextureColorSource(texture.GetPixels());
            return this;
        }
        
        public PaintViewBuilder SetColorSource(IColorSource source)
        {
            _colorSource = source;
            return this;
        }

        public Entity Build()
        {

            if (_colorSource == null)
            {
                _colorSource = new PlainColorSource(1,1,1);
            }
            

            var EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
          //  Color[] textureColors = _sourceTexture.GetPixels();
            
            
            
        //    _cubes = new Entity[width, height];
            Parent parent = Parent.Create();
            ParentId parentId = parent.ParentId();
            ParentView parentView = new ParentView
            {
                Id = parent.Id,
                Width = _width,
                Height = _height
            };

            var renderData = new RenderData
            {
                Mesh = _mesh,
                Material = _material
            };


            EntityArchetype archetype = EntityManager.CreateArchetype(
                typeof(ParentId),
                typeof(RenderData),
                typeof(WorldTransform),
                typeof(ParentViewId),
                typeof(PixelCube),
                typeof(Position),
                typeof(Rotation),
                typeof(Scale),
                typeof(RenderColor),
                typeof(MoveTo),
                typeof(RotateTo),
                typeof(MouseInteractive),
                typeof(Prefab)
            );


            var prefab = EntityManager.CreateEntity(archetype);
            EntityManager.SetSharedComponentData(prefab, renderData);
            EntityManager.SetSharedComponentData(prefab, parentView.ParentViewId());

            Profiler.BeginSample("PaintViewBuilder.CreateEntities");
            var count = _width * _height;
            var entities = new NativeArray<Entity>(count, Allocator.Temp);
            EntityManager.Instantiate(prefab, entities);
            Profiler.EndSample();
            
            
            Profiler.BeginSample("PaintViewBuilder.PrepareData");


            var colors = new NativeArray<RenderColor>(count, Allocator.TempJob);
            var pixelCubes = new NativeArray<PixelCube>(count, Allocator.TempJob);
            var positions = new NativeArray<Position>(count, Allocator.TempJob);
            var rotations = new NativeArray<Rotation>(count, Allocator.TempJob);
            var scales = new NativeArray<Scale>(count, Allocator.TempJob);
            var parentIds = new NativeArray<ParentId>(count, Allocator.TempJob);

            for (int x = 0; x < _width; ++x)
            {
                for (int y = 0; y < _height; ++y)
                {
                    var index = y * _width + x;

                    parentIds[index] = parentId;
                    colors[index] = new RenderColor {Value = _colorSource.getColor(index)};
                    pixelCubes[index] = new PixelCube {Position = new int2(x, y)};
                    positions[index] = new Position {Value = new float3(x, y, 0)};
                    rotations[index] = new Rotation {Value = quaternion.identity};
                    scales[index] = new Scale {Value = new float3(1, 1, 1)};
                }
            }

            Profiler.EndSample();

            
            
            Profiler.BeginSample("PaintViewBuilder.SetData");

            var query = EntityManager.CreateEntityQuery(
                typeof(ParentViewId),
                typeof(PixelCube),
                typeof(Position),
                typeof(Rotation),
                typeof(Scale),
                typeof(RenderColor),
                typeof(ParentId));

            query.SetSharedComponentFilter(parentView.ParentViewId());
            query.CopyFromComponentDataArray(parentIds);
            query.CopyFromComponentDataArray(pixelCubes);
            query.CopyFromComponentDataArray(colors);
            query.CopyFromComponentDataArray(positions);
            query.CopyFromComponentDataArray(rotations);
            query.CopyFromComponentDataArray(scales);


            entities.Dispose();
            colors.Dispose();
            pixelCubes.Dispose();
            positions.Dispose();
            rotations.Dispose();
            scales.Dispose();
            parentIds.Dispose();

            Profiler.EndSample();
            
            
            
            Profiler.BeginSample("PaintEditor.CreateParent");
            var entity = EntityBuilder.Create()
                .Add(parent)
                .Add(new Position {Value = _position})
                .Add(new Rotation {Value = _rotation})
                .Add(new Scale {Value = _scale})
                .Add(new ParentView {Width = _width, Height = _height, Id = parentId.Value})
                .Build();
           
            Profiler.EndSample();

            return entity;
        }
        
    }

     public interface IColorSource
    {
        Color getColor(int index);
    }

    class TextureColorSource : IColorSource
    {
        private Color[] _colors;

        public TextureColorSource(Color[] colors)
        {
            _colors = colors;
        }

        public Color getColor(int index)
        {
         //   Debug.Log("index/"+ _colors.Length);
            if (index < _colors.Length)
            {
                return _colors[index];    
            }
            else
            {
                return Color.white;
            }
            
        }
    }

    class PlainColorSource : IColorSource
    {
        private Color _color;

        public PlainColorSource(Color color)
        {
            _color = color;
        }
        public PlainColorSource(float r, float g, float b)
        {
            _color = new Color(r,g,b);
        }
        public PlainColorSource(float r, float g, float b, float a)
        {
            _color = new Color(r,g,b,a);
        }

        public Color getColor(int index)
        {
            return _color;
        }
    }
}