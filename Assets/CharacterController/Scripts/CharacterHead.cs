 using Unity.Entities;
 using Unity.Mathematics;
 using Unity.Transforms;
 using UnityEngine;

 [GenerateAuthoringComponent]
 public struct CharacterHead : IComponentData
 {
     
 }
 
 
#region System
// Update before physics gets going so that we don't have hazard warnings.
// This assumes that all gun are being controlled from the same single input system
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public class CharacterHeadOneToManyInputSystem : SystemBase
{ 
 

    
    protected override void OnUpdate()
    { 
        var input = GetSingleton<CharacterGunInput>();
        float dt = Time.DeltaTime;

        Entities
            .WithName("CharacterHeadOneToManyInputJob")
            .WithBurst()
            .ForEach((Entity entity,  ref Rotation headRotation, ref CharacterHead head, in LocalToWorld headTransform) =>
            {
                // Handle input
               
                    float a = -input.Looking.y;
                    if (a != 0)
                    {
                        var euler = BMath.ToEuler(headRotation.Value);
                       
                        euler.x += math.radians(a);    
                        var halfPI = math.PI/2 - 0.01f; 
                        euler.x = math.clamp(euler.x, -halfPI, halfPI);
                        euler.y = 0;
                        euler.z = 0;  
                        headRotation.Value = quaternion.Euler(euler);
                        BMath.ToEuler(headRotation.Value);  
                    } 
            }).ScheduleParallel();
 
    }
}
#endregion