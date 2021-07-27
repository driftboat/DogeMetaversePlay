using Unity.Entities;
using Unity.Mathematics;

public struct Land : IComponentData
{
    /// <summary>
    /// x worldId
    /// y landX
    /// z landY
    /// </summary>
    public int3 LandPos;
}

public struct DynamicLand : IBufferElementData
{
    public Land Value;
}