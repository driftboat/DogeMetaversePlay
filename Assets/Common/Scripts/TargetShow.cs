using Unity.Entities;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct TargetShow : IComponentData
{
     
}

#region System
// Update before physics gets going so that we don't have hazard warnings.
// This assumes that all gun are being controlled from the same single input system
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public class TargetShowSystem : SystemBase
{  
    protected override void OnUpdate()
    {       var input = GetSingleton<CharacterGunInput>();

        Entities
            .WithName("TargetShowSystem")
            .WithBurst()
            .ForEach((Entity entity,  ref Translation trans, in TargetShow head) => { trans.Value = input.TargetPos; }).Run();
 
    }
}
#endregion