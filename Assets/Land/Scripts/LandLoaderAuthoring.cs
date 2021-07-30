 
    using Unity.Entities;
    using UnityEngine;

    public class LandLoaderAuthoring: MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {

            dstManager.AddComponentData(
                entity, new LandLoader());
            dstManager.AddBuffer<DynamicLand>(entity);

        }
    }
 