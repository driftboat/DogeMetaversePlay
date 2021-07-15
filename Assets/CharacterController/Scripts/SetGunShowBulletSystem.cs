 

using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))] 
public class SetGunShowBulletSystem : SystemBase
{
    private EndInitializationEntityCommandBufferSystem m_CommandBufferSystem;
    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        var commandBufferParallel = m_CommandBufferSystem.CreateCommandBuffer();
        Entities
            .WithName("SetGunShowBulletJob")
            .WithoutBurst()
            .ForEach(
                (Entity entity, int entityInQueryIndex, in GunShowBox gunShowBox ) =>
                {
                    var characterGun =  EntityManager.GetComponentData<CharacterGun>(gunShowBox.Gun);
                    characterGun.ShowBullet = entity;
                    commandBufferParallel.SetComponent<CharacterGun>( gunShowBox.Gun, characterGun);
                    commandBufferParallel.RemoveComponent<GunShowBox>(  entity); 
                }
            ).Run();
    }
}