using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
 
[GenerateAuthoringComponent]
public struct CreateTerrain : IComponentData
{
 
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class InitTerrainSystem : SystemBase
{
    protected override void OnCreate()
    {
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


    protected override void OnUpdate()
    {
        var configEntity = GetSingletonEntity<Config>(); 
        var boxBuff = GetBuffer<BoxBlobAssetRef>(configEntity);
        var random = new Unity.Mathematics.Random((uint)10);
        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity creatorEntity, in CreateTerrain creator) =>
            {
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
