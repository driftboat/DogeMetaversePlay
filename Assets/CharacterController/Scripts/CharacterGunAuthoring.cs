using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public struct CharacterGun : IComponentData
{
    public Entity Bullet;
    public Entity Hand;
    public Entity ShowBullet;
    public float Strength;
    public float Rate;
    public float Duration;

    public int WasFiring;
    public int IsFiring;
    public int WasPlacing;
    public int IsPlacing;

    public int CurrentBoxIndex;
    public int CurrentBoxSubIndex;
}

public struct CharacterGunInput : IComponentData
{
    public float2 Looking;
    public float Firing;
    public float Placing;
    public Entity Target;
    public float3 TargetPos;
    public int3 TargetGrid;
    public int3 GenGrid;
    
    public float BoxSelect;
}

public struct GunShowBox : IComponentData
{
    public Entity Gun;
    
}

public struct BoxBlobAsset
{
    public BlobArray<Entity> Boxes;
}

public struct BoxBlobAssetRef : IBufferElementData
{
    public BlobAssetReference<BoxBlobAsset> BoxesRef;
}

public class CharacterGunAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject[] Bullets;
    public GameObject hand;
    public float Strength;
    public float Rate;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.AddRange(Bullets);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var boxBlobAssetRefBuf = dstManager.AddBuffer<BoxBlobAssetRef>(entity);
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            ref BoxBlobAsset boxBlobAsset = ref blobBuilder.ConstructRoot<BoxBlobAsset>();
            BlobBuilderArray<Entity> entityArr = blobBuilder.Allocate(ref boxBlobAsset.Boxes, Bullets.Length);
            for (int i = 0; i < Bullets.Length; i++)
            {
                var bullet = Bullets[i];
                entityArr[i] = conversionSystem.GetPrimaryEntity(bullet);
            } 

            BlobAssetReference<BoxBlobAsset> boxBlobAssetRef =
                blobBuilder.CreateBlobAssetReference<BoxBlobAsset>(Allocator.Persistent);
            boxBlobAssetRefBuf.Add(new BoxBlobAssetRef
            {
                BoxesRef = boxBlobAssetRef
            });
        }

        dstManager.AddComponentData(
            entity,
            new CharacterGun
            {
                Bullet = conversionSystem.GetPrimaryEntity(Bullets[0]),
                Hand = conversionSystem.GetPrimaryEntity(hand),
                Strength = Strength,
                Rate = Rate,
                WasFiring = 0,
                IsFiring = 0,
                WasPlacing = 0,
                IsPlacing = 0,
            });
    }
}

#region System

// Update before physics gets going so that we don't have hazard warnings.
// This assumes that all gun are being controlled from the same single input system
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public class CharacterGunOneToManyInputSystem : SystemBase
{
    EntityCommandBufferSystem m_EntityCommandBufferSystem;
    BuildBPhysicsWorld m_BuildPhysicsWorld;
    protected override void OnCreate()
    {
        base.OnCreate();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_BuildPhysicsWorld = World.GetExistingSystem<BuildBPhysicsWorld>();
    }
  

    static float3 toEuler(quaternion q)
    {
        const float epsilon = 1e-6f;

        //prepare the data
        var qv = q.value;
        var d1 = qv * qv.wwww * new float4(2.0f); //xw, yw, zw, ww
        var d2 = qv * qv.yzxw * new float4(2.0f); //xy, yz, zx, ww
        var d3 = qv * qv;
        var euler = new float3(0.0f);

        const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);


        var y1 = d2.y - d1.x;
        if (y1 * y1 < CUTOFF)
        {
            var x1 = d2.x + d1.z;
            var x2 = d3.y + d3.w - d3.x - d3.z;
            var z1 = d2.z + d1.y;
            var z2 = d3.z + d3.w - d3.x - d3.y;
            euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
        }
        else //zxz
        {
            y1 = math.clamp(y1, -1.0f, 1.0f);
            var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
            var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
            var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
            euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
        }

        return euler.yzx;
        ;
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var input = GetSingleton<CharacterGunInput>();
        float dt = Time.DeltaTime; 
        NativeList<int3> removePosArr = new NativeList<int3>(Allocator.TempJob);
        var removePosWriter = removePosArr.AsParallelWriter();
        Entities
            .WithName("CharacterControllerGunToManyInputJob")
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, ref Rotation gunRotation, ref CharacterGun gun,
                in LocalToWorld gunTransform, in DynamicBuffer<BoxBlobAssetRef> boxBlobAssetRef) =>
            {
                // Handle input
                {
                    gun.IsFiring = input.Firing > 0f ? 1 : 0;
                    gun.IsPlacing = input.Placing > 0f ? 1 : 0;
                }
                var oldBullet = gun.Bullet;

                if (gun.IsFiring == 0)
                {
                    gun.Duration = 0;
                    gun.WasFiring = 0;
                }

                if (gun.IsPlacing == 0)
                {
                    gun.WasPlacing = 0;
                }

                if (gun.IsPlacing > 0)
                {
                    gun.Duration += dt;
                    if (gun.WasPlacing == 0)
                    {
                        if (gun.Bullet != null &&  !math.all(input.GenGrid == BMath.nagtiveUnit) )
                        {
                            var e = commandBuffer.Instantiate(entityInQueryIndex, gun.Bullet);
                            var pos = input.GenGrid + new float3(0.5f,0.5f,0.5f);  
                            Translation position = new Translation {Value = pos};
                            Rotation rotation = new Rotation {Value = quaternion.identity};
                      
                            commandBuffer.SetComponent(entityInQueryIndex, e, position);
                            commandBuffer.SetComponent(entityInQueryIndex, e, rotation);
                        }

                        gun.Duration = 0;
                    }

                    gun.WasPlacing = 1;
                }
                else if (gun.IsFiring > 0)
                {
                    if (gun.WasFiring == 0)
                    {
                        if (input.Target != Entity.Null && !HasComponent<DontDetroy>(input.Target))
                        {
                            commandBuffer.DestroyEntity(entityInQueryIndex, input.Target);
                            removePosWriter.AddNoResize( input.TargetGrid);
                        }

                        gun.Duration = 0;
                    }

                    gun.WasFiring = 1;
                }
                else if (input.BoxSelect > 0)
                {
                    ref BoxBlobAsset boxBlobAsset = ref boxBlobAssetRef[0].BoxesRef.Value;
                    if (gun.CurrentBoxSubIndex < boxBlobAsset.Boxes.Length - 1)
                    {
                        gun.CurrentBoxSubIndex++;
                    }

                    gun.Bullet = boxBlobAsset.Boxes[gun.CurrentBoxSubIndex];
                }
                else if (input.BoxSelect < 0)
                {
                    ref BoxBlobAsset boxBlobAsset = ref boxBlobAssetRef[0].BoxesRef.Value;
                    if (gun.CurrentBoxSubIndex > 0)
                    {
                        gun.CurrentBoxSubIndex--;
                    }

                    gun.Bullet = boxBlobAsset.Boxes[gun.CurrentBoxSubIndex];
                }

                if (oldBullet != gun.Bullet || gun.ShowBullet == Entity.Null)
                {
                    if (gun.ShowBullet != Entity.Null)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, gun.ShowBullet);
                    }

                    var e = commandBuffer.Instantiate(entityInQueryIndex, gun.Bullet);
                    commandBuffer.RemoveComponent<BoxNeedInit>(entityInQueryIndex, e);
                    commandBuffer.AddComponent(entityInQueryIndex, e, new Parent { Value = gun.Hand } );
                    commandBuffer.AddComponent(entityInQueryIndex, e, new LocalToParent());
                    commandBuffer.SetComponent(entityInQueryIndex, e, new Translation{Value = new float3(0, 0, 0.808f)});
                    commandBuffer.AddComponent(entityInQueryIndex, e, new Scale{Value = 0.3f});
                    commandBuffer.AddComponent(entityInQueryIndex, e, new GunShowBox{ Gun = entity}); 
                }
            }).ScheduleParallel();

        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();
        foreach (var pos in removePosArr)
        {
            m_BuildPhysicsWorld.PhysicsWorld.m_posBBodyMap.Remove(pos);
        }

        removePosArr.Dispose();
    }
}

#endregion