 
using Unity.Entities;
using Unity.Mathematics;

public struct BRaycastInput
{
    public float3 Start { get; set; }
    public float3 End { get; set; }
}

public struct BRaycastHit
{
    public Entity Entity { get; set; }
    public float3 Position { get; set; }
    /// <summary>
    /// The gird where the hit occurred.
    /// </summary>
    public int3 EntityGrid { get; set; }
    /// <summary>
    /// grid where to generate new box
    /// </summary>
    public int3 GenGrid { get; set; } 
    
    public float3 SurfaceNormal { get; set; }
}
