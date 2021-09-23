using Unity.Entities;
    
public struct BoxNearby:IComponentData
{
    public Entity self;
    public Entity left;
    public Entity right;
    public Entity front;
    public Entity back;
    public Entity top;
    public Entity bottom;
}