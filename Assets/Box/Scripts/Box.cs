using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public struct ColorBox:IComponentData
{
    public float3 Color;
    public int3 Land;
    public int3 Pos;
}

public struct CommonBox:IComponentData
{
    public short BoxType;
    public int3 Land;
    public int3 Pos;
}

public struct ColorBoxInLand
{
    public float3 Color;
    public int3 Pos;
}

public struct CommonBoxInLand
{
    public short BoxType;
    public int3 Pos;
}


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public class CreateBoxSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var configEntity = GetSingletonEntity<Config>();

        var boxBuff = GetBuffer<BoxBlobAssetRef>(configEntity);
        Entities
            .WithName("CreateColorBoxJob") 
            .WithNone<Translation>()
            .WithReadOnly(boxBuff) 
            .ForEach((Entity colorBoxEntity, int entityInQueryIndex, in ColorBox colorBox) =>
            {
                ref BoxBlobAsset boxBlobAsset = ref boxBuff[0].BoxesRef.Value;
                var e = commandBuffer.Instantiate(entityInQueryIndex, boxBlobAsset.Boxes[0]); 
                commandBuffer.SetComponent(entityInQueryIndex, e, new Translation() {Value = BMath.LandToWorldPos(colorBox.Land) + colorBox.Pos + new float3(0.5f,0.5f,0.5f)});
                commandBuffer.AddComponent(entityInQueryIndex, e,
                    new URPMaterialPropertyBaseColor {Value = new float4(colorBox.Color.x, colorBox.Color.y, colorBox.Color.z, 1)});
                commandBuffer.AddComponent(entityInQueryIndex,e, colorBox);
                commandBuffer.DestroyEntity(entityInQueryIndex, colorBoxEntity);
            }).ScheduleParallel();
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}