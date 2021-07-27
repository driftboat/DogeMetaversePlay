using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
 
[GenerateAuthoringComponent]
public struct CreateTerrain : IComponentData
{
    public int worldId;
    public int landPosX;
    public int landPosY;
}



[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateLandSystem))]
public class InitTerrainSystem : SystemBase
{
    private Texture2D dogeTexture;
    private EndInitializationEntityCommandBufferSystem m_CommandBufferSystem;
    private NativeArray<float3> dogeTextureColors;
    private int dogeTextureWidth;
    private int dogeTextureHeight;
    protected override void OnCreate()
    {
        #if UNITY_EDITOR
        dogeTexture = LoadPNG(Application.dataPath + "/../Images/doge30.png");
        #else
        dogeTexture = LoadPNG("doge30.png");
        #endif
        dogeTextureWidth = dogeTexture.width;
        dogeTextureHeight = dogeTexture.height;
        dogeTextureColors = new NativeArray<float3>(dogeTextureWidth*dogeTextureHeight, Allocator.Persistent);


        for (int i = 0; i < dogeTextureWidth; i++)
        {
            for (int j = 0; j < dogeTextureHeight; j++)
            {
                int box = 0;
                Color color = dogeTexture.GetPixel(i, j);
             
                dogeTextureColors[j * dogeTextureWidth + i] = new float3(color.r, color.g, color.b);
            }
        }

        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CreateTerrain)
            }
        }));
        m_CommandBufferSystem =  World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    static float noiseWithParam(float2 pos, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float totalAmplitude = 0;  // Used for normalizing result to 0.0 - 1.0
        for(int i=0;i<octaves;i++) {
            total += noise.cnoise(pos*frequency) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        float result = total/totalAmplitude;
        return result;
    }
    public static Texture2D LoadPNG(string filePath) {
 
        Texture2D tex = null;
        byte[] fileData;
 
        if (File.Exists(filePath))     {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var configEntity = GetSingletonEntity<Config>();
 
        var boxBuff = GetBuffer<BoxBlobAssetRef>(configEntity);
        var colorW = dogeTextureWidth;
        var colorH = dogeTextureHeight;
        var colors = dogeTextureColors;
        Entities
            .WithName("CreateTerrainJob")
            .WithoutBurst() 
            .WithReadOnly(boxBuff)
            .WithReadOnly(colors)
            .ForEach((Entity creatorEntity, int entityInQueryIndex, in CreateTerrain creator) =>
            {
                float3 landPos  = BMath.GetWorldStartPos(creator.worldId) + BMath.GetLandOffsetPos(creator.landPosX,creator.landPosY);
                float3 setpos = landPos + new float3{x=10.5f,y=0.5f,z=10.5f};
                ref BoxBlobAsset boxBlobAsset = ref boxBuff[0].BoxesRef.Value;
                for (int i = 0; i < colorW; i++)
                {
                    for (int j = 0; j < colorH; j++)
                    {
                        int box = 0;
                        float3 color =  colors[j*colorW + i];
                        
                        var e =  commandBuffer.Instantiate(entityInQueryIndex,boxBlobAsset.Boxes[box]);  
                        var pos =   new float3((float)i,(float)j  ,0) + setpos;
                        commandBuffer.SetComponent(entityInQueryIndex,e, new Translation() { Value = pos });
                        commandBuffer.AddComponent(entityInQueryIndex,e, new URPMaterialPropertyBaseColor {Value = new float4(color.x, color.y, color.z, 1)});
                    }
                }
                
                commandBuffer.DestroyEntity(entityInQueryIndex,creatorEntity);
            }).ScheduleParallel();
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
