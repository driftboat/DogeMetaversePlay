using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;


[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateLandSystem))]
public class DestoyLandSystem : SystemBase
{
    public static bool needDstroy;
    private EndInitializationEntityCommandBufferSystem m_CommandBufferSystem; 
    private BuildPhysicsWorld m_BuildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        m_BuildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    } 
    
    protected override void OnUpdate()
    {
        if (!needDstroy) return;
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var landLoaderEntity = GetSingletonEntity<LandLoader>();
        var landBuffer = GetBuffer<DynamicLand>(landLoaderEntity);
        var loadedLandBuffer = landBuffer.Reinterpret<Land>();
        NativeList<int3> removePosArr = new NativeList<int3>(960000,Allocator.TempJob);
        var removePosWriter = removePosArr.AsParallelWriter();
        Entities
            .WithName("DestroyLandJob") 
            .WithNone<GunShowBox>()
            .WithReadOnly(loadedLandBuffer)
            .ForEach((Entity entity, int entityInQueryIndex, in ColorBox  box,in LocalToWorld localToWorld) =>
            {
     
                var shouldDestroy = true;
                for (int j = 0; j < loadedLandBuffer.Length; j++)
                {
                    var lb = loadedLandBuffer[j];
                    if (math.all(lb.LandPos == box.Land))
                    {
                        shouldDestroy = false;
                        break;
                    }
                }

                if (shouldDestroy)
                {
                    removePosWriter.AddNoResize(BMath.PositionToGridCoord(localToWorld.Position));
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
 
            }).ScheduleParallel();
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();
        foreach (var pos in removePosArr)
        {
            m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld.m_posBBodyMap.Remove(pos);
        }
        needDstroy = false;
        removePosArr.Dispose();
    }
}