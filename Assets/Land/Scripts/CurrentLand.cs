using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
 
public struct LandLoader : IComponentData
{
    public Land CurrentLand; 
    
}



[UpdateInGroup(typeof(InitializationSystemGroup))]
public class UpdateLandSystem : SystemBase
{

    protected override void OnUpdate()
    {
        var landLoaderEntity = GetSingletonEntity<LandLoader>();
        var landLoader = GetComponent<LandLoader>(landLoaderEntity);
        var landBuffer = GetBuffer<DynamicLand>(landLoaderEntity);
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var loadedLandBuffer = landBuffer.Reinterpret<Land>();
        Entities.WithName("characterLandCheckJob").WithoutBurst().ForEach((Entity playerEntity, in Translation translation,
            in CharacterControllerComponentData characterControllerComponentData) =>
        {
            float3 playerPos = translation.Value;
            int3 land = BMath.WorldPosToLand(playerPos);
            int worldId = land.x;
            int landx = land.y;
            int landy = land.z;
        
            if (math.any(landLoader.CurrentLand.LandPos != land))
            {
                var showLands = new NativeList<int3>(9, Allocator.Temp);
                //--add create current land entity if the land is not exist
                showLands.Add(land);
                bool exist = false;
                Debug.Log("land change:"+worldId + "," + landx + "," + landy);
                exist = false;
                for (int j = 0; j < loadedLandBuffer.Length; j++)
                {
                    var lb = loadedLandBuffer[j];
                    if (math.all(lb.LandPos == land))
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    var entity = ecb.CreateEntity(); 
                    ecb.AddComponent(entity, new CreateLand{Land = land});
                    loadedLandBuffer.Add(new Land{LandPos = land});
                }
                //search nearby and add create  land entity if the land is not exist
                for (int i = 1; i <= 8; i++)
                {
                   int3 nearBy = BMath.GetLandNearBy(worldId, landx, landy, i);
                   Debug.Log("land nearBy:"+i + ","+nearBy );
                  
                   if (math.any(nearBy != int3.zero))
                   {
                       showLands.Add(nearBy);
                       exist = false;
                       for (int j = 0; j < loadedLandBuffer.Length; j++)
                       {
                           var lb = loadedLandBuffer[j];
                           if (math.all(lb.LandPos == nearBy))
                           {
                               exist = true;
                               break;
                           }
                       }

                       if (!exist)
                       { 
                           var entity = ecb.CreateEntity(); 
                           ecb.AddComponent(entity, new CreateLand{ Land = new int3(nearBy.x, nearBy.y, nearBy.z)});
                           loadedLandBuffer.Add(new Land{LandPos = nearBy});
                       } 
                   }
                }
                //--remove invisible lands
                for (int j = 0; j < loadedLandBuffer.Length; j++)
                {
                    var lb = loadedLandBuffer[j];
                    if (!showLands.Contains(lb.LandPos))
                    {
                        Debug.Log("remove land:"+lb.LandPos);
                        loadedLandBuffer.RemoveAt(j);
                        j--;
                    }
                }

                landLoader.CurrentLand.LandPos = land; 
                SetSingleton<LandLoader>(landLoader);
                DestoyLandSystem.needDstroy = true;
                showLands.Dispose();
            }
            
            
        }).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
