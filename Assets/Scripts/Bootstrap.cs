using PaintECS;
using Unity.Entities;
using UnityEngine;

public class Bootstrap
{
    [RuntimeInitializeOnLoadMethod]
    public static void init()
    {
        PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown);

        var world = new World("PaintECS");
        World.Active = world;

        // INITIALIZATION
        var initialization = world.GetOrCreateSystem<InitializationSystemGroup>();
       
        initialization.AddSystemToUpdateList(world.GetOrCreateSystem<InputSystem>());
        initialization.AddSystemToUpdateList(world.GetOrCreateSystem<SequenceSystem>());
        initialization.AddSystemToUpdateList(world.GetOrCreateSystem<PaintViewSystem>());
        initialization.SortSystemUpdateList();
        
        // SIMULATION
        var simulation = world.GetOrCreateSystem<SimulationSystemGroup>();
        simulation.AddSystemToUpdateList(world.GetOrCreateSystem<CameraPositionSystem>());
        simulation.AddSystemToUpdateList(world.GetOrCreateSystem<TweenSystem>());
        simulation.AddSystemToUpdateList(world.GetOrCreateSystem<TransformSystem>());
        simulation.SortSystemUpdateList();
        
        // PRESENTATION
        var presentation = world.GetOrCreateSystem<PresentationSystemGroup>();
        presentation.AddSystemToUpdateList(world.GetOrCreateSystem<RenderSystem>());
        presentation.SortSystemUpdateList();
        
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
    }

    static void DomainUnloadShutdown()
    {
        World.DisposeAllWorlds();

        WordStorage.Instance.Dispose();
        WordStorage.Instance = null;
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);
    }
}