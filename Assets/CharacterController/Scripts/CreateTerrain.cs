using System.IO;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
 
[GenerateAuthoringComponent]
public struct CreateTerrain : IComponentData
{
 
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class InitTerrainSystem : SystemBase
{
    private Texture2D dogeTexture;
    protected override void OnCreate()
    {
        dogeTexture = LoadPNG("D://doge30.png");
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CreateTerrain)
            }
        }));
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
        var configEntity = GetSingletonEntity<Config>();
        var boxBuff = GetBuffer<BoxBlobAssetRef>(configEntity);
        var random = new Unity.Mathematics.Random((uint) 10);
    
        var tex = dogeTexture;

 
        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity creatorEntity, in CreateTerrain creator) =>
            {
                
                ref BoxBlobAsset boxBlobAsset = ref boxBuff[0].BoxesRef.Value;
                float3 setpos = new float3{x=10.5f,y=0.5f,z=10.5f};
                
                for (int i = 0; i < tex.width; i++)
                {
                    for (int j = 0; j < tex.height; j++)
                    {
                        int box = 0;
                        Color color =  tex.GetPixel(i, j);
                        int r = (int)math.round(color.r * 255);
                        int b = (int)math.round(color.b * 255);
                        if (b == 181)
                        {
                            box = 0;
                        }else if (b < 10)
                        {
                            box = 3;
                        }else if (b == 53)
                        {
                            box = 1;
                        }else if (b == 60)
                        {
                            box = 2;
                        }else if (b == 255)
                        {
                            box = 4;
                        }

                        var e = EntityManager.Instantiate(boxBlobAsset.Boxes[box]); 
                        var pos =   new float3((float)i,(float)j  ,0) + setpos;   
                        
                        EntityManager.SetComponentData(e, new Translation() { Value = pos }); 
//                        EntityManager.AddComponent(e, typeof(MaterialColor));
//                        MaterialColor mcc = new MaterialColor{ Value = new float4(color.r, color.g, color.b, color.a)};
//                        EntityManager.SetComponentData(e, mcc);
                        
                        EntityManager.AddComponentData(e, new URPMaterialPropertyBaseColor {Value = new float4(color.r, color.g, color.b, color.a)});
                    }
                }
  
 
//                ref BoxBlobAsset boxBlobAsset = ref boxBuff[0].BoxesRef.Value;
//                for (int i = 0; i < 50; i++)
//                {
//                    for (int j = 0; j < 50; j++)
//                    {
//                        var n = noiseWithParam(new float2(i/100.0f, j/100.0f),4,0.5f,2);
//                        
//                        //var s = noiseWithParam(new float2(-i/100.0f, -j/100.0f),4,0.5f,2);
//                        n = math.remap(-1, 1, 0, 1, n);
//                       // int height = (int)math.round(20 + 10*n); 
//                       int height = 1;
//                        for (int k = 0; k < height; k++)
//                        {
//                            int box = 2;
//             
//
//                            if (k == height - 1)
//                            {
//                                  box = 0;
//                                  if (height > 23)
//                                  {
//                                      box = 1;
//                                  }
//                            }
//                            
//
//                            float3 pos = new float3(i+0.5f,k+0.5f,j+0.5f);
//               
//                            var e = EntityManager.Instantiate(boxBlobAsset.Boxes[box]);  
//                            Rotation rotation = new Rotation {Value = quaternion.identity}; 
//                            EntityManager.SetComponentData(e, new Translation() { Value = pos }); 
//                        }
//                    }
//                }
// 
               

                EntityManager.DestroyEntity(creatorEntity);
            }).Run();
    }
}
