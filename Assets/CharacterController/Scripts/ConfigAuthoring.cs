 

using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

struct Config : IComponentData
{
}


public class ConfigAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject[] Bullets;
    
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

            BlobAssetReference<BoxBlobAsset> boxBlobAssetRef = blobBuilder.CreateBlobAssetReference<BoxBlobAsset>(Allocator.Persistent);
            boxBlobAssetRefBuf.Add(new BoxBlobAssetRef{
                BoxesRef = boxBlobAssetRef
            });
        }

        dstManager.AddComponentData(entity, new Config{});
        
    }
}

