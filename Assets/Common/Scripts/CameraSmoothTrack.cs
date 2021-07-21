using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

// Camera Utility to smoothly track a specified target from a specified location
// Camera location and target are interpolated each frame to remove overly sharp transitions
public class CameraSmoothTrack : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649
    public GameObject Target;
    public GameObject LookTo;
    [Range(0, 1)] public float LookToInterpolateFactor = 0.9f;

    public GameObject LookFrom;
    [Range(0, 1)] public float LookFromInterpolateFactor = 0.9f;
#pragma warning restore 649

    void OnValidate()
    {
        LookToInterpolateFactor = math.clamp(LookToInterpolateFactor, 0f, 1f);
        LookFromInterpolateFactor = math.clamp(LookFromInterpolateFactor, 0f, 1f);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CameraSmoothTrackSettings
        {
            Target = conversionSystem.GetPrimaryEntity(Target),
            LookTo = conversionSystem.GetPrimaryEntity(LookTo),
            LookToInteroplateFactor = LookToInterpolateFactor,
            LookFrom = conversionSystem.GetPrimaryEntity(LookFrom),
            LookFromInterpolateFactor = LookFromInterpolateFactor
        });
    }
}

struct CameraSmoothTrackSettings : IComponentData
{
    public Entity Target;
    public Entity LookTo;
    public float LookToInteroplateFactor;
    public Entity LookFrom;
    public float LookFromInterpolateFactor;
    public float3 OldPositionTo;
}

[UpdateAfter(typeof(TransformSystemGroup))]
class SmoothlyTrackCameraTarget : SystemBase
{
    struct Initialized : ISystemStateComponentData
    {
    }

    BuildBPhysicsWorld m_BuildPhysicsWorld;
    RecordMostRecentFixedTime m_RecordMostRecentFixedTime;
    EntityQuery m_CharacterGunInputQuery;
    private CollisionFilter _collisionFilter; 
    
    protected override void OnCreate()
    {
        base.OnCreate();
        m_BuildPhysicsWorld = World.GetExistingSystem<BuildBPhysicsWorld>();
        m_RecordMostRecentFixedTime = World.GetExistingSystem<RecordMostRecentFixedTime>();
        m_CharacterGunInputQuery = GetEntityQuery(typeof(CharacterGunInput));
        _collisionFilter = new CollisionFilter
        {
            BelongsTo = 0xffffffff,
            CollidesWith = 1,
            GroupIndex = 0
        };
        
    }

    protected override void OnUpdate()
    { 
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("InitializeCameraOldPositionsJob")
            .WithBurst()
            .WithNone<Initialized>()
            .ForEach((Entity entity, ref CameraSmoothTrackSettings cameraSmoothTrack, in LocalToWorld localToWorld) =>
            {
                commandBuffer.AddComponent<Initialized>(entity);
                cameraSmoothTrack.OldPositionTo = HasComponent<LocalToWorld>(cameraSmoothTrack.LookTo)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position
                    : localToWorld.Position + new float3(0f, 0f, 1f);
            }).Run();
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();

        BPhysicsWorld world = m_BuildPhysicsWorld.PhysicsWorld;

        var timeAhead = (float) (Time.ElapsedTime - m_RecordMostRecentFixedTime.MostRecentElapsedTime);
        var input = GetSingleton<CharacterGunInput>();

        Entities
            .WithName("SmoothlyTrackCameraTargetsJob")
            .WithoutBurst()
            .WithAll<Initialized>()
            .WithReadOnly(world)
            .ForEach((CameraSmoothTrack monoBehaviour, ref CameraSmoothTrackSettings cameraSmoothTrack,
                in LocalToWorld localToWorld) =>
            {
                var worldPosition = (float3) monoBehaviour.transform.position;

                float3 newPositionFrom = HasComponent<LocalToWorld>(cameraSmoothTrack.LookFrom)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookFrom).Position
                    : worldPosition;

                float3 newPositionTo = HasComponent<LocalToWorld>(cameraSmoothTrack.LookTo)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position
                    : worldPosition + localToWorld.Forward;
                

                float3 newForward = newPositionTo - newPositionFrom;
                newForward = math.normalizesafe(newForward);
                quaternion newRotation = quaternion.LookRotation(newForward, math.up());




                var rayInput = new BRaycastInput
                {
                    Start = newPositionTo,
                    End = newPositionTo + newForward * 20,
                };

                float3 castPos = float3.zero;
                int3 castGrid = BMath.nagtiveUnit;
                Entity castEntity = Entity.Null;
                int3 genGrid = BMath.nagtiveUnit;
                
                if (world.CastRay(rayInput,  out BRaycastHit rayResult))
                {
                    castGrid = rayResult.EntityGrid;
                    castPos = rayResult.Position;
                    castEntity = rayResult.Entity;
                    genGrid = rayResult.GenGrid;
                }

                input.TargetPos = castPos;
                input.Target = castEntity;
                input.TargetGrid = castGrid;
                input.GenGrid = genGrid;

                m_CharacterGunInputQuery.SetSingleton(input);
                monoBehaviour.transform.SetPositionAndRotation(newPositionFrom, newRotation);
                cameraSmoothTrack.OldPositionTo = newPositionTo;
            }).Run();
    }
 
}