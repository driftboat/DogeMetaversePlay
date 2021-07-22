using Unity.Collections; 
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Assertions;

// Stores the impulse to be applied by the character controller body
public struct DeferredCharacterControllerImpulse
{
    public Entity Entity;
    public float3 Impulse;
    public float3 Point;
}

public static class CharacterControllerUtilities
{
    const float k_SimplexSolverEpsilon = 0.0001f;
    const float k_SimplexSolverEpsilonSq = k_SimplexSolverEpsilon * k_SimplexSolverEpsilon;

    const int k_DefaultQueryHitsCapacity = 8;
    const int k_DefaultConstraintsCapacity = 2 * k_DefaultQueryHitsCapacity;

    public enum CharacterSupportState : byte
    {
        Unsupported = 0,
        Sliding,
        Supported
    }

    public struct CharacterControllerStepInput
    {
        public BPhysicsWorld World;
        public float DeltaTime;
        public float3 Gravity;
        public float3 Up;
        public int MaxIterations;
        public float Tau;
        public float Damping;
        public float SkinWidth;
        public float ContactTolerance;
        public float MaxSlope;
        public int RigidBodyIndex;
        public float3 CurrentVelocity;
        public float MaxMovementSpeed;
    }

     
    public static unsafe void CollideAndIntegrate(
        CharacterControllerStepInput stepInput, float characterMass, bool affectBodies,
        ref RigidTransform transform, ref float3 linearVelocity)
    {
        // Copy parameters
        float deltaTime = stepInput.DeltaTime;
        float3 up = stepInput.Up;
        BPhysicsWorld world = stepInput.World;

        float remainingTime = deltaTime;

        float3 newPosition = transform.pos;
        quaternion orientation = transform.rot;
        float3 newVelocity = linearVelocity;

        float maxSlopeCos = math.cos(stepInput.MaxSlope);

        const float timeEpsilon = 0.000001f;
        for (int i = 0; i < stepInput.MaxIterations && remainingTime > timeEpsilon; i++)
        {
            
            // Min delta time for solver to break
            float minDeltaTime = 0.0f;
            if (math.lengthsq(newVelocity) > k_SimplexSolverEpsilonSq)
            {
                // Min delta time to travel at least 1cm
                minDeltaTime = 0.01f / math.length(newVelocity);
            }

            // Solve
            float3 prevVelocity = newVelocity;
            float3 prevPosition = newPosition;
            float integratedTime = remainingTime;
            newPosition += (prevVelocity *remainingTime ); 

            // Apply impulses to hit bodies and store collision events
           

            // Calculate new displacement
            float3 newDisplacement = newPosition - prevPosition;
 

            // Reduce remaining time
            remainingTime -= integratedTime;

            // Write back position so that the distance query will update results
            transform.pos = newPosition;
        }

        // Write back final velocity
        linearVelocity = newVelocity;
    }
 
}
