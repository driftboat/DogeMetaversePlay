using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class SaveLandSystem : SystemBase
{
    private bool m_existLandToSave;
    private Land m_landToSave;

    protected override void OnCreate()
    {
        base.OnCreate();
        EventManager.instance.saveLand += OnSaveLand;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventManager.instance.saveLand -= OnSaveLand;
    }

    private void OnSaveLand(Land land)
    {
        m_existLandToSave = true;
        m_landToSave = land;
    }


    protected override void OnUpdate()
    {
        if (m_existLandToSave)
        {
            String filePath = Application.persistentDataPath +
                              $"{m_landToSave.LandPos.x}_{m_landToSave.LandPos.y}_{m_landToSave.LandPos.z}";
            Debug.Log("===========================save start:" + filePath);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    EntityQuery entityQuery = GetEntityQuery(ComponentType.ReadOnly<URPMaterialPropertyBaseColor>(),
                        ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<BoxType>());
                    var entityCount = entityQuery.CalculateEntityCount();
                    if (entityCount > 0)
                    {
                        var colorBoxes = new NativeList<ColorBoxInLand>(entityCount, Allocator.TempJob);
                        var boxWriter = colorBoxes.AsParallelWriter();
                        var landToSave = m_landToSave; 
                        Entities
                            .WithName("GetLandColorBoxJob")
                            .WithBurst()
                            .ForEach(
                                (Entity entity, int entityInQueryIndex, in URPMaterialPropertyBaseColor matColor,
                                    in Translation translation, in BoxType boxType) =>
                                {
                                    var landPos = BMath.WorldPosToLand(translation.Value);
                                    if (math.all(landPos == landToSave.LandPos))
                                    {
                                        if (boxType.boxType == 1)
                                        {
                                            var cb = new ColorBoxInLand
                                            {
                                                Color = matColor.Value.xyz,
                                                Pos = BMath.WorldPosToLandPos(translation.Value)
                                            };
                                            boxWriter.AddNoResize(cb);
                                        }
                                    }
                                }
                            ).ScheduleParallel();
                        Dependency.Complete();
                        var data = colorBoxes.ToArray(Allocator.Temp).ToRawBytes();
                        binaryWriter.Write(colorBoxes.Length);
                        binaryWriter.Write(data.Length);
                        binaryWriter.Write(data);  
                    }
                    else
                    {
                        binaryWriter.Write(0);
                    }



                    var queryDescription = new EntityQueryDesc
                    {
                        None = new ComponentType[]
                            {ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<BoxType>()},
                        All = new ComponentType[] {ComponentType.ReadOnly<URPMaterialPropertyBaseColor>()}
                    };
                    entityQuery = GetEntityQuery(queryDescription);
                    entityCount = entityQuery.CalculateEntityCount();

                    if (entityCount > 0)
                    {
                        var commonBoxes = new NativeList<CommonBoxInLand>(entityCount, Allocator.TempJob);
                        var boxWriter = commonBoxes.AsParallelWriter();
                        var landToSave = m_landToSave;
                        Entities
                            .WithName("GetLandCommonBoxJob")
                            .WithNone<URPMaterialPropertyBaseColor>()
                            .WithBurst()
                            .ForEach(
                                (Entity entity, int entityInQueryIndex, in Translation translation, in BoxType boxType) =>
                                {
                                    var landPos = BMath.WorldPosToLand(translation.Value);
                                    if (math.all(landPos == landToSave.LandPos))
                                    {
                                        if (boxType.boxType != 1)
                                        {
                                            var cb = new CommonBoxInLand
                                            {
                                                BoxType = boxType.boxType, Pos = BMath.WorldPosToLandPos(translation.Value)
                                            };
                                            boxWriter.AddNoResize(cb);
                                        }
                                    }
                                }
                            ).ScheduleParallel();

                  
                        var boxCount = commonBoxes.Length;
                        Dependency.Complete();
                        var data = commonBoxes.ToArray(Allocator.Temp).ToRawBytes();
                        binaryWriter.Write(commonBoxes.Length);
                        binaryWriter.Write(data.Length);
                        binaryWriter.Write(data); 
                    } else
                    {
                        binaryWriter.Write(0);
                    }

                   
                }
            }

            Debug.Log("===========================save finish:" + filePath);
            m_existLandToSave = false;
        }
    }
}