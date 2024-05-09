using Unity.Entities;

namespace PaintECS
{
    public class EntityBuilder
    {
        private static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
        
        public static EntityBuilder Create()
        {
            return new EntityBuilder();
        }
        
        public static EntityBuilder FromEntity(Entity entity)
        {
            return new EntityBuilder(entity);
        }
        
        private Entity _entity;

        public EntityBuilder()
        {
            _entity = EntityManager.CreateEntity();
        }
        
        public EntityBuilder(Entity entity)
        {
            _entity = entity;
          //  Debug.Log("Entity : " + entity.Index + " ? " + EntityManager.Exists(entity));
            
        }

        public EntityBuilder Add<T>(T component) where T : unmanaged, IComponentData
        {
            EntityManager.AddComponentData(_entity, component);
            return this;
        }
        
        public EntityBuilder Set<T>(ref T component) where T : unmanaged, IComponentData
        {
            EntityManager.SetComponentData(_entity, component);
            return this;
        }

        public EntityBuilder AddShared<T>(T component) where T : struct, ISharedComponentData
        {
            EntityManager.AddSharedComponentManaged(_entity, component);
            return this;
        }
        
        public EntityBuilder SetShared<T>(T component) where T : struct, ISharedComponentData
        {
            EntityManager.SetSharedComponentManaged(_entity, component);
            return this;
        }
        

        public Entity Build()
        {
            return _entity;
        }
    }
}