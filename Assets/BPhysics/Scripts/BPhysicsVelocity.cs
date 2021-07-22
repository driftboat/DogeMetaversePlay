using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BPhysicsVelocity : IComponentData
{
    /// <summary>
    /// The body's world-space linear velocity in units per second.
    /// </summary>
    public float3 Linear;
    /// <summary>
    /// The body's angular velocity in radians per second about each principal axis specified by <see cref="Transform"/>.
    /// In order to get or set world-space values, use <see cref="ComponentExtensions.GetAngularVelocityWorldSpace"/> and <see cref="ComponentExtensions.SetAngularVelocityWorldSpace"/>, respectively.
    /// </summary>
    public float3 Angular;    
}
     