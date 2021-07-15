using Unity.Entities;

[GenerateAuthoringComponent]
public struct BBody:IComponentData
{
    // The entity that box body represents
    public Entity Entity; 
}