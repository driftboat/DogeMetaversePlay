using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct CreateLand : IComponentData
{
    public int3 Land;
}


[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateLandSystem))]
public class CreateLandSystem : SystemBase
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
        dogeTextureColors = new NativeArray<float3>(dogeTextureWidth * dogeTextureHeight, Allocator.Persistent);


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
                typeof(CreateLand)
            }
        }));
        m_CommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    static float noiseWithParam(float2 pos, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float totalAmplitude = 0; // Used for normalizing result to 0.0 - 1.0
        for (int i = 0; i < octaves; i++)
        {
            total += noise.cnoise(pos * frequency) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float result = total / totalAmplitude;
        return result;
    }

    public static Texture2D LoadPNG(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        return tex;
    }

    struct ReadColorBoxes : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter CommandBufferWriter;
        [ReadOnly] public NativeArray<ColorBoxInLand> Boxes;

        public int3 BoxLand;

        public void Execute(int index)
        {
            Debug.Log("==============read" + index);
            var box = Boxes[index];
            var e = CommandBufferWriter.CreateEntity(index);
            CommandBufferWriter.AddComponent(index, e, new ColorBox
            {
                Land = BoxLand,
                Pos = box.Pos,
                Color = box.Color
            });
        }
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        var colorW = dogeTextureWidth;
        var colorH = dogeTextureHeight;
        var colors = dogeTextureColors;
        var dataPath = Application.persistentDataPath;
        Entities
            .WithStructuralChanges()
            .ForEach((Entity creatorEntity, in CreateLand creator) =>
            {
                String filePath = dataPath +
                                  $"{creator.Land.x}_{creator.Land.y}_{creator.Land.z}";
                if (File.Exists(filePath))
                {
                    Debug.Log("exist:" + creator.Land);
                    EntityManager.DestroyEntity(creatorEntity);
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fileStream))
                        {
                            int boxCnt = binaryReader.ReadInt32();
                            int dataSize = binaryReader.ReadInt32();
                            var data = binaryReader.ReadBytes(dataSize);
                            NativeArray<ColorBoxInLand> boxes =
                                new NativeArray<ColorBoxInLand>(boxCnt, Allocator.TempJob);
                            boxes.CopyFromRawBytes(data);
                            var readBoxes = new ReadColorBoxes
                                {CommandBufferWriter = commandBuffer, Boxes = boxes, BoxLand = creator.Land};
                            var jobHanlder = readBoxes.Schedule(boxCnt, 32, Dependency);
                            Dependency = JobHandle.CombineDependencies(Dependency,jobHanlder);
                        }
                    }
                }
            }).Run();

        Entities
            .WithName("CreateLandJob")
            .WithoutBurst()
            .WithReadOnly(colors)
            .ForEach((Entity creatorEntity, int entityInQueryIndex, in CreateLand creator) =>
            {
                for (int i = 0; i < colorW; i++)
                {
                    for (int j = 0; j < colorH; j++)
                    {
                        int box = 0;
                        float3 color = colors[j * colorW + i];
                        var e = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, e, new ColorBox
                        {
                            Land = creator.Land,
                            Pos = new int3(10 + i, j, 10),
                            Color = color
                        });
                    }
                }

                commandBuffer.DestroyEntity(entityInQueryIndex, creatorEntity);
            }).ScheduleParallel();
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}