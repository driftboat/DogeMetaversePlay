using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[AlwaysUpdateSystem]
public class BuildBPhysicsWorld:SystemBase
{
    public BPhysicsWorld PhysicsWorld = new BPhysicsWorld();
    EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        m_CommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        PhysicsWorld.m_posBBodyMap = new NativeHashMap<int3, BBody>(0, Allocator.Persistent);
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery( typeof(Translation), typeof(BoxNeedInit));
        var entityCount = entityQuery.CalculateEntityCount();
        PhysicsWorld.AddBBodyCapacity(entityCount);
        var commandBufferParallel = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var bodyMap = PhysicsWorld.m_posBBodyMap;
        var bodyMapPWriter = PhysicsWorld.m_posBBodyMap.AsParallelWriter();
        Entities
            .WithName("BoxMapCollectionJob")
            .WithBurst()
            .ForEach(
                (Entity entity, int entityInQueryIndex, in Translation translation, in BoxNeedInit boxNeedInit) =>
                {
                    var coord = BMath.PositionToGridCoord(translation.Value);
                    bodyMapPWriter.TryAdd(coord, new BBody {Entity = entity});
                    commandBufferParallel.RemoveComponent<BoxNeedInit>(entityInQueryIndex, entity); 
                }
            ).ScheduleParallel();
        
         Entities
            .WithName("NearbyCollectionJob")
            .WithBurst()
            .WithReadOnly(bodyMap)
            .ForEach(
                (Entity entity, int entityInQueryIndex, in Translation translation,
                    in BoxNeedCollectNearby boxNeedInit) =>
                {
                    var crood = BMath.PositionToGridCoord(translation.Value);
                    BoxNearby boxNearby = new BoxNearby();
                    BBody body;
                    boxNearby.self = entity;
                    bool isInBox = true;
                    //right;
                    int3 right = new int3(crood.x + 1, crood.y, crood.z);
                 
                    if (bodyMap.TryGetValue(right, out body))
                    {
                        boxNearby.right = body.Entity;
                    }
                    else
                    {
                        isInBox = false;
                    }

                    int3 left = new int3(crood.x - 1, crood.y, crood.z);
                    if (bodyMap.TryGetValue(left, out body))
                    {
                        boxNearby.left = body.Entity;
                    } else
                    {
                        isInBox = false;
                    }

                    int3 front = new int3(crood.x, crood.y, crood.z + 1);
                    if (bodyMap.TryGetValue(front, out body))
                    {
                        boxNearby.front = body.Entity;
                    } else
                    {
                        isInBox = false;
                    }

                    int3 back = new int3(crood.x, crood.y, crood.z - 1);
                    if (bodyMap.TryGetValue(back, out body))
                    {
                        boxNearby.back = body.Entity;
                    } else
                    {
                        isInBox = false;
                    }

                    int3 top = new int3(crood.x, crood.y+1, crood.z );
                    if (bodyMap.TryGetValue(top, out body))
                    {
                        boxNearby.top = body.Entity;
                    } else
                    {
                        isInBox = false;
                    }
                    int3 bottom = new int3(crood.x, crood.y-1, crood.z );
                    if (bodyMap.TryGetValue(bottom, out body))
                    {
                        boxNearby.bottom = body.Entity;
                    } else if(crood.y != 0)
                    {
                        isInBox = false;
                    }

                    if (isInBox)
                    {
                        commandBufferParallel.AddComponent(entityInQueryIndex,entity,new DisableRendering());  
                    } 
 
                    commandBufferParallel.AddComponent(entityInQueryIndex,entity,boxNearby);
                }
            ).ScheduleParallel() ;
    }
}