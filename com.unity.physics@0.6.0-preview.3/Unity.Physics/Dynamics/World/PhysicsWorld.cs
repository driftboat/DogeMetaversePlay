using System;
using Unity.Entities;
using System.ComponentModel;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Physics
{
    // A collection of rigid bodies and joints.
    [NoAlias]
    public struct PhysicsWorld : ICollidable, IDisposable
    {
        [NoAlias] public CollisionWorld CollisionWorld;   // stores rigid bodies and broadphase
        [NoAlias] public DynamicsWorld DynamicsWorld;     // stores motions and joints

        public int NumAllBodies  => CollisionWorld.NumAllBodies;
        public int NumBodies => CollisionWorld.NumBodies;
        public int NumStaticBodies => CollisionWorld.NumStaticBodies;
        public int NumDynamicBodies => CollisionWorld.NumDynamicBodies;
        public int NumJoints => DynamicsWorld.NumJoints;

        public NativeArray<RigidBody> AllBodies => CollisionWorld.AllBodies;
        public NativeArray<RigidBody> Bodies => CollisionWorld.Bodies;
        public NativeArray<RigidBody> StaticBodies => CollisionWorld.StaticBodies;
        public NativeArray<RigidBody> DynamicBodies => CollisionWorld.DynamicBodies;
        public NativeArray<MotionData> MotionDatas => DynamicsWorld.MotionDatas;
        public NativeArray<MotionVelocity> MotionVelocities => DynamicsWorld.MotionVelocities;
        public NativeArray<Joint> Joints => DynamicsWorld.Joints;

        // Construct a world with the given number of uninitialized bodies and joints
        public PhysicsWorld(int numStaticBodies, int numDynamicBodies, int numJoints)
        {
            CollisionWorld = new CollisionWorld(numStaticBodies, numDynamicBodies);
            DynamicsWorld = new DynamicsWorld(numDynamicBodies, numJoints);
        }

        // Reset the number of bodies and joints in the world
        public void Reset(int numStaticBodies, int numDynamicBodies, int numJoints, int numBoxes)
        {
            CollisionWorld.Reset(numStaticBodies, numDynamicBodies, numBoxes);
            DynamicsWorld.Reset(numDynamicBodies, numJoints);
        }

        // Free internal memory
        public void Dispose()
        {
            CollisionWorld.Dispose();
            DynamicsWorld.Dispose();
        }

        // Clone the world
        public PhysicsWorld Clone() => new PhysicsWorld
        {
            CollisionWorld = (CollisionWorld)CollisionWorld.Clone(),
            DynamicsWorld = (DynamicsWorld)DynamicsWorld.Clone()
        };

        public int GetRigidBodyIndex(Entity entity) => CollisionWorld.GetRigidBodyIndex(entity);
        public int GetJointIndex(Entity entity) => DynamicsWorld.GetJointIndex(entity);

        // NOTE:
        // The BuildPhysicsWorld system updates the Body and Joint Index Maps.
        // If the PhysicsWorld is being setup and updated directly then
        // this UpdateIndexMaps function should be called manually as well.
        public void UpdateIndexMaps()
        {
            CollisionWorld.UpdateBodyIndexMap();
            DynamicsWorld.UpdateJointIndexMap();
        }

        #region ICollidable implementation

        public Aabb CalculateAabb()
        {
            return CollisionWorld.CalculateAabb();
        }

        public Aabb CalculateAabb(RigidTransform transform)
        {
            return CollisionWorld.CalculateAabb(transform);
        }

        // Cast ray
        public bool CastRay(RaycastInput input) => QueryWrappers.RayCast(ref this, input);
        public bool CastRay(RaycastInput input, out RaycastHit closestHit) => QueryWrappers.RayCast(ref this, input, out closestHit);
        public bool CastRay(RaycastInput input, ref NativeList<RaycastHit> allHits) => QueryWrappers.RayCast(ref this, input, ref allHits);
        public bool CastRay<T>(RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
        {
            return CollisionWorld.CastRay(input, ref collector);
        }

        public bool CastRay(BRaycastInput input,   out BRaycastHit hit)
        {
            float3 src = input.Start;
            float3 target = input.End;
            hit = new BRaycastHit();
            float3 dir = target - src;

            float3 now = src;
            int3 nowgrid = (int3)math.floor(now);
            int3 targetGrid = (int3)math.floor(target);
            bool3 isOnEdge;
            int3 nextgrid;
            float3 nextgridt;
            float totalT = 0;
            int i = 0;
            float3 nowfraction = now - nowgrid;
            nextgrid = nowgrid;
            if (math.abs(nowfraction.x) < math.EPSILON)
            {
                isOnEdge.x = true;
            }
            else
            {
                isOnEdge.x = false;
            }
            if (math.abs(nowfraction.y) < math.EPSILON)
            {
                isOnEdge.y = true;
            }
            else
            {
                isOnEdge.y = false;
            }
            if (math.abs(nowfraction.z) < math.EPSILON)
            {
                isOnEdge.z = true;
            }
            else
            {
                isOnEdge.z = false;
            }

            while (nowgrid.x != targetGrid.x || nowgrid.y != targetGrid.y || nowgrid.z != targetGrid.z)
            {
                nextgridt.x = nextgridt.y = nextgridt.z = float.MaxValue;
                if (dir.x > float.Epsilon)
                {
                    nextgrid.x = nowgrid.x + 1;
                    nextgridt.x = (nextgrid.x - now.x) / dir.x;
                }
                else if (dir.x < -float.Epsilon)
                {
                    nextgrid.x = nowgrid.x;
                    if (isOnEdge.x)
                    {
                        nextgrid.x  = nowgrid.x - 1;
                    }
                    nextgridt.x  = (nextgrid.x  - now.x) / dir.x;
                }

                if (dir.y > 0)
                {
                    nextgrid.y = nowgrid.y + 1;
                    nextgridt.y = (nextgrid.y - now.y) / dir.y;
                }
                else if (dir.y < 0)
                {
                    nextgrid.y = nowgrid.y;
                    if (isOnEdge.y)
                    {
                        nextgrid.y = nowgrid.y - 1;
                    }

                    nextgridt.y = (nextgrid.y - now.y) / dir.y;
                }

                if (dir.z >  0)
                {
                    nextgrid.z = nowgrid.z + 1;
                    nextgridt.z = (nextgrid.z - now.z) / dir.z;
                }
                else if (dir.z < 0)
                {
                    nextgrid.z = nowgrid.z;
                    if (isOnEdge.z)
                    {
                        nextgrid.z = nowgrid.z - 1;
                    }
                    nextgridt.z = (nextgrid.z - now.z) / dir.z;
                }

                float t = math.min(nextgridt.x , math.min(nextgridt.y, nextgridt.z));
                totalT += t;
                if (totalT > 1)
                {
                    return false;
                }

                now = now + dir * t;
                if (math.abs(t - nextgridt.x) < math.EPSILON)
                {
                    now.x = nextgrid.x;
                }
                if (math.abs(t - nextgridt.y) < math.EPSILON)
                {
                    now.y = nextgrid.y;
                }
                if (math.abs(t - nextgridt.z) < math.EPSILON)
                {
                    now.z = nextgrid.z;
                }

                nowgrid = (int3)math.floor(now);

                nowfraction = now - nowgrid;
                nextgrid = nowgrid;
                if (math.abs(nowfraction.x) < math.EPSILON)
                {
                    isOnEdge.x = true;
                }
                else
                {
                    isOnEdge.x = false;
                }
                if (math.abs(nowfraction.y) < math.EPSILON)
                {
                    isOnEdge.y = true;
                }
                else
                {
                    isOnEdge.y = false;
                }
                if (math.abs(nowfraction.z) < math.EPSILON)
                {
                    isOnEdge.z = true;
                }
                else
                {
                    isOnEdge.z = false;
                }
                BBody body;
                if (CollisionWorld.m_posBBodyMap.TryGetValue(nowgrid, out body))
                {
                    hit.Position = now;
                    hit.EntityGrid = nowgrid;
                    hit.Entity = body.Entity;
                    if (isOnEdge.x)
                    {
                        hit.GenGrid = new int3(nowgrid.x - 1, nowgrid.y, nowgrid.z);
                    }
                    else if (isOnEdge.y)
                    {
                        hit.GenGrid = new int3(nowgrid.x , nowgrid.y - 1, nowgrid.z);
                    }
                    else if (isOnEdge.z)
                    {
                        hit.GenGrid = new int3(nowgrid.x, nowgrid.y, nowgrid.z  - 1);
                    }


                    return true;
                }

                int3 checkg = nowgrid;

                if (isOnEdge.x && dir.x < -float.Epsilon)
                {
                    checkg.x -= 1;
                    if (CollisionWorld.m_posBBodyMap.TryGetValue(checkg, out body))
                    {
                        hit.Position = now;
                        hit.EntityGrid = checkg;
                        hit.Entity = body.Entity;
                        hit.GenGrid = nowgrid;
                        return true;
                    }
                }

                checkg = nowgrid;
                if (isOnEdge.y && dir.y < -float.Epsilon)
                {
                    checkg.y -= 1;
                    if (CollisionWorld.m_posBBodyMap.TryGetValue(checkg, out body))
                    {
                        hit.Position = now;
                        hit.EntityGrid = checkg;
                        hit.Entity = body.Entity;
                        hit.GenGrid = new int3(checkg.x , checkg.y + 1, checkg.z);
                        return true;
                    }
                }

                checkg = nowgrid;
                if (isOnEdge.z && dir.z < -float.Epsilon)
                {
                    checkg.z -= 1;
                    if (CollisionWorld.m_posBBodyMap.TryGetValue(checkg, out body))
                    {
                        hit.Position = now;
                        hit.EntityGrid = checkg;
                        hit.Entity = body.Entity;
                        hit.GenGrid = nowgrid;
                        return true;
                    }
                }

                if (now.y <= 0 && nowgrid.x >= 0 && nowgrid.z >= 0)
                {
                    hit.Position = now;
                    hit.EntityGrid = checkg;
                    hit.Entity = Entity.Null;
                    hit.GenGrid = checkg;
                    return true;
                }

                if (i > 20)
                {
                    break;
                }

                i++;
            }

            return false;
        }

        // Cast collider
        public bool CastCollider(ColliderCastInput input) => QueryWrappers.ColliderCast(ref this, input);
        public bool CastCollider(ColliderCastInput input, out ColliderCastHit closestHit) => QueryWrappers.ColliderCast(ref this, input, out closestHit);
        public bool CastCollider(ColliderCastInput input, ref NativeList<ColliderCastHit> allHits) => QueryWrappers.ColliderCast(ref this, input, ref allHits);
        public bool CastCollider<T>(ColliderCastInput input, ref T collector) where T : struct, ICollector<ColliderCastHit>
        {
            return CollisionWorld.CastCollider(input, ref collector);
        }

        // Point distance
        public bool CalculateDistance(PointDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(PointDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(PointDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public bool CalculateDistance<T>(PointDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            return CollisionWorld.CalculateDistance(input, ref collector);
        }

        // Collider distance
        public bool CalculateDistance(ColliderDistanceInput input) => QueryWrappers.CalculateDistance(ref this, input);
        public bool CalculateDistance(ColliderDistanceInput input, out DistanceHit closestHit) => QueryWrappers.CalculateDistance(ref this, input, out closestHit);
        public bool CalculateDistance(ColliderDistanceInput input, ref NativeList<DistanceHit> allHits) => QueryWrappers.CalculateDistance(ref this, input, ref allHits);
        public bool CalculateDistance<T>(ColliderDistanceInput input, ref T collector) where T : struct, ICollector<DistanceHit>
        {
            return CollisionWorld.CalculateDistance(input, ref collector);
        }

        #region GO API Queries

        // Interfaces that represent queries that exist in the GameObjects world.

        public bool CheckSphere(float3 position, float radius, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CheckSphere(ref this, position, radius, filter, queryInteraction);
        public bool OverlapSphere(float3 position, float radius, ref NativeList<DistanceHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.OverlapSphere(ref this, position, radius, ref outHits, filter, queryInteraction);
        public bool OverlapSphereCustom<T>(float3 position, float radius, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<DistanceHit>
            => QueryWrappers.OverlapSphereCustom(ref this, position, radius, ref collector, filter, queryInteraction);

        public bool CheckCapsule(float3 point1, float3 point2, float radius, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CheckCapsule(ref this, point1, point2, radius, filter, queryInteraction);
        public bool OverlapCapsule(float3 point1, float3 point2, float radius, ref NativeList<DistanceHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.OverlapCapsule(ref this, point1, point2, radius, ref outHits, filter, queryInteraction);
        public bool OverlapCapsuleCustom<T>(float3 point1, float3 point2, float radius, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<DistanceHit>
            => QueryWrappers.OverlapCapsuleCustom(ref this, point1, point2, radius, ref collector, filter, queryInteraction);

        public bool CheckBox(float3 center, quaternion orientation, float3 halfExtents, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CheckBox(ref this, center, orientation, halfExtents, filter, queryInteraction);
        public bool OverlapBox(float3 center, quaternion orientation, float3 halfExtents, ref NativeList<DistanceHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.OverlapBox(ref this, center, orientation, halfExtents, ref outHits, filter, queryInteraction);
        public bool OverlapBoxCustom<T>(float3 center, quaternion orientation, float3 halfExtents, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<DistanceHit>
            => QueryWrappers.OverlapBoxCustom(ref this, center, orientation, halfExtents, ref collector, filter, queryInteraction);

        public bool SphereCast(float3 origin, float radius, float3 direction, float maxDistance, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.SphereCast(ref this, origin, radius, direction, maxDistance, filter, queryInteraction);
        public bool SphereCast(float3 origin, float radius, float3 direction, float maxDistance, out ColliderCastHit hitInfo, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.SphereCast(ref this, origin, radius, direction, maxDistance, out hitInfo, filter, queryInteraction);
        public bool SphereCastAll(float3 origin, float radius, float3 direction, float maxDistance, ref NativeList<ColliderCastHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.SphereCastAll(ref this, origin, radius, direction, maxDistance, ref outHits, filter, queryInteraction);
        public bool SphereCastCustom<T>(float3 origin, float radius, float3 direction, float maxDistance, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<ColliderCastHit>
            => QueryWrappers.SphereCastCustom(ref this, origin, radius, direction, maxDistance, ref collector, filter, queryInteraction);

        public bool BoxCast(float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.BoxCast(ref this, center, orientation, halfExtents, direction, maxDistance, filter, queryInteraction);
        public bool BoxCast(float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance, out ColliderCastHit hitInfo, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.BoxCast(ref this, center, orientation, halfExtents, direction, maxDistance, out hitInfo, filter, queryInteraction);
        public bool BoxCastAll(float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance, ref NativeList<ColliderCastHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.BoxCastAll(ref this, center, orientation, halfExtents, direction, maxDistance, ref outHits, filter, queryInteraction);
        public bool BoxCastCustom<T>(float3 center, quaternion orientation, float3 halfExtents, float3 direction, float maxDistance, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<ColliderCastHit>
            => QueryWrappers.BoxCastCustom(ref this, center, orientation, halfExtents, direction, maxDistance, ref collector, filter, queryInteraction);

        public bool CapsuleCast(float3 point1, float3 point2, float radius, float3 direction, float maxDistance, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CapsuleCast(ref this, point1, point2, radius, direction, maxDistance, filter, queryInteraction);
        public bool CapsuleCast(float3 point1, float3 point2, float radius, float3 direction, float maxDistance, out ColliderCastHit hitInfo, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CapsuleCast(ref this, point1, point2, radius, direction, maxDistance, out hitInfo, filter, queryInteraction);
        public bool CapsuleCastAll(float3 point1, float3 point2, float radius, float3 direction, float maxDistance, ref NativeList<ColliderCastHit> outHits, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default)
            => QueryWrappers.CapsuleCastAll(ref this, point1, point2, radius, direction, maxDistance, ref outHits, filter, queryInteraction);
        public bool CapsuleCastCustom<T>(float3 point1, float3 point2, float radius, float3 direction, float maxDistance, ref T collector, CollisionFilter filter, QueryInteraction queryInteraction = QueryInteraction.Default) where T : struct, ICollector<ColliderCastHit>
            => QueryWrappers.CapsuleCastCustom(ref this, point1, point2, radius, direction, maxDistance, ref collector, filter, queryInteraction);

        #endregion

        #endregion
    }
}
