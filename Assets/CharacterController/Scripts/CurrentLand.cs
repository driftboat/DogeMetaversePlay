using System.IO;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct CurrentLand : IComponentData
{
    public int worldId;
    public int landPosX;
    public int landPosY;
}



[UpdateInGroup(typeof(InitializationSystemGroup))]
public class UpdateLandSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var curLand = GetSingleton<CurrentLand>();
 
        Entities.WithName("characterLandCheckJob").WithStructuralChanges().ForEach((Entity playerEntity, in Translation translation,
            in CharacterControllerComponentData characterControllerComponentData) =>
        {
            float3 playerPos = translation.Value;
            int3 land = BMath.WorldPosToLand(playerPos);
            int worldId = land.x;
            int landx = land.y;
            int landy = land.z;
 
            if (curLand.worldId != worldId || curLand.landPosX != landx || curLand.landPosY != landy)
            {
                Debug.Log("land change:"+worldId + "," + landx + "," + landy);
                Entity entity = EntityManager.CreateEntity(typeof(CreateTerrain));
                EntityManager.SetComponentData(entity, new CreateTerrain{worldId = worldId, landPosX = landx, landPosY = landy});
                for (int i = 1; i < 7; i++)
                {
                   int3 nearBy = BMath.GetLandNearBy(worldId, landx, landy, i);
                   if (math.any(nearBy != int3.zero))
                   {
                       entity = EntityManager.CreateEntity(typeof(CreateTerrain));
                       EntityManager.SetComponentData(entity, new CreateTerrain{worldId = nearBy.x, landPosX = nearBy.y, landPosY = nearBy.z});
                   }
                }


                curLand.worldId = worldId;
                curLand.landPosX = landx;
                curLand.landPosY = landy;
                SetSingleton<CurrentLand>(curLand);
            }
            
        }).Run();
    }
}
