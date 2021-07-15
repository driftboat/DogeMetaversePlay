using Unity.Entities;
using UnityEngine;
// in order to circumvent API breakages that do not affect physics, some packages are removed from the project on CI
// any code referencing APIs in com.unity.inputsystem must be guarded behind UNITY_INPUT_SYSTEM_EXISTS
#if UNITY_INPUT_SYSTEM_EXISTS
using UnityEngine.InputSystem;
#endif

[AlwaysUpdateSystem]
[UpdateInGroup(typeof(InitializationSystemGroup))]
class DemoInputGatheringSystem : SystemBase
#if UNITY_INPUT_SYSTEM_EXISTS
    ,
    InputActions.ICharacterControllerActions,
    InputActions.IVehicleActions
#endif
{
    EntityQuery m_CharacterControllerInputQuery;
    EntityQuery m_CharacterGunInputQuery;
    EntityQuery m_VehicleInputQuery;

#pragma warning disable 649
    Vector2 m_CharacterMovement;
    Vector2 m_CharacterLooking;
    float m_CharacterFiring;
    float m_CharacterPlacing;
    bool m_CharacterJumped;
    bool m_CharacterFalled;
    Vector2 m_CharacterBoxSelect;

    Vector2 m_VehicleLooking;
    Vector2 m_VehicleSteering;
    float m_VehicleThrottle;
    int m_VehicleChanged;
#pragma warning restore 649

    protected override void OnCreate()
    {
#if UNITY_INPUT_SYSTEM_EXISTS
        m_InputActions = new InputActions();
        m_InputActions.CharacterController.SetCallbacks(this);
        m_InputActions.Vehicle.SetCallbacks(this);
#endif

        m_CharacterControllerInputQuery = GetEntityQuery(typeof(CharacterControllerInput));
        m_CharacterGunInputQuery = GetEntityQuery(typeof(CharacterGunInput));
    }

#if UNITY_INPUT_SYSTEM_EXISTS
    InputActions m_InputActions;

    protected override void OnStartRunning() => m_InputActions.Enable();

    protected override void OnStopRunning() => m_InputActions.Disable();

    void InputActions.ICharacterControllerActions.OnMove(InputAction.CallbackContext context) => m_CharacterMovement = context.ReadValue<Vector2>();
    void InputActions.ICharacterControllerActions.OnLook(InputAction.CallbackContext context) => m_CharacterLooking = context.ReadValue<Vector2>();
    void InputActions.ICharacterControllerActions.OnFire(InputAction.CallbackContext context) => m_CharacterFiring = context.ReadValue<float>();
    
    void InputActions.ICharacterControllerActions.OnPlace(InputAction.CallbackContext context) => m_CharacterPlacing = context.ReadValue<float>();
    void InputActions.ICharacterControllerActions.OnJump(InputAction.CallbackContext context) { if (context.started) m_CharacterJumped = true; }

    void InputActions.ICharacterControllerActions.OnFall(InputAction.CallbackContext context)
    {
        if (context.started) m_CharacterFalled = true;
    }

    void InputActions.ICharacterControllerActions.OnBoxSelect(InputAction.CallbackContext context)
    {
        m_CharacterBoxSelect =  context.ReadValue<Vector2>();
    }

    void InputActions.IVehicleActions.OnLook(InputAction.CallbackContext context) => m_VehicleLooking = context.ReadValue<Vector2>();
    void InputActions.IVehicleActions.OnSteering(InputAction.CallbackContext context) => m_VehicleSteering = context.ReadValue<Vector2>();
    void InputActions.IVehicleActions.OnThrottle(InputAction.CallbackContext context) => m_VehicleThrottle = context.ReadValue<float>();
    void InputActions.IVehicleActions.OnPrevious(InputAction.CallbackContext context) { if (context.started) m_VehicleChanged = -1; }
    void InputActions.IVehicleActions.OnNext(InputAction.CallbackContext context) { if (context.started) m_VehicleChanged = 1; }
    
#endif

    protected override void OnUpdate()
    {
        // character controller
        if (m_CharacterControllerInputQuery.CalculateEntityCount() == 0)
            EntityManager.CreateEntity(typeof(CharacterControllerInput));
        
        m_CharacterControllerInputQuery.SetSingleton(new CharacterControllerInput
        {
            Looking = m_CharacterLooking,
            Movement = m_CharacterMovement,
            Jumped = m_CharacterJumped ? 1 : 0,
            Falled = m_CharacterFalled ? 1 : 0
        });
        if(m_CharacterBoxSelect.y != 0) Debug.Log("============" + m_CharacterBoxSelect.y);
        if (m_CharacterGunInputQuery.CalculateEntityCount() == 0)
            EntityManager.CreateEntity(typeof(CharacterGunInput));
        var cg = m_CharacterGunInputQuery.GetSingleton<CharacterGunInput>();
        cg.Looking = m_CharacterLooking;
        cg.Firing = m_CharacterFiring;
        cg.Placing = m_CharacterPlacing;
        cg.BoxSelect = m_CharacterBoxSelect.y;
        m_CharacterGunInputQuery.SetSingleton(cg);

        m_CharacterJumped = false;
        m_CharacterFalled = false;

      

        m_VehicleChanged = 0;
    }
}
