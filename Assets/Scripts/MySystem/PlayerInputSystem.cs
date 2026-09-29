using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

// Reads keyboard/gamepad and writes it into PlayerInputData.
// SystemBase (not ISystem + Burst) because InputAction and Camera are managed classes.
// Runs first in the frame's simulation, before movement and physics, so input is used the same frame.
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction dashAction;

    protected override void OnCreate()
    {
        // Don't run until the subscene has loaded a player entity
        RequireForUpdate<Player>();
    }

    protected override void OnStartRunning()
    {
        // Project-wide actions asset (InputSystem_Actions), "Player" action map
        InputActionMap playerMap = InputSystem.actions.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
        crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: true);
        // The asset has no "Dash" action, so Sprint (Shift) is used as dash
        dashAction = playerMap.FindAction("Sprint", throwIfNotFound: true);
        playerMap.Enable();
    }

    protected override void OnUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            // No camera tagged MainCamera, so there is no "forward" to move along
            return;
        }

        // Camera forward/right flattened onto the ground
        Vector3 forward = camera.transform.forward;
        Vector3 right = camera.transform.right;
        float3 cameraForward = math.normalizesafe(new float3(forward.x, 0f, forward.z));
        float3 cameraRight = math.normalizesafe(new float3(right.x, 0f, right.z));

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        // Keep length at most 1
        if (math.lengthsq(moveDirection) > 1f)
        {
            moveDirection = math.normalize(moveDirection);
        }

        bool isCrouching = crouchAction.IsPressed();
        bool jumpPressed = jumpAction.WasPressedThisFrame();
        bool dashPressed = dashAction.WasPressedThisFrame();

        foreach (RefRW<PlayerInputData> inputData in SystemAPI.Query<RefRW<PlayerInputData>>().WithAll<Player>())
        {
            inputData.ValueRW.moveDirection = moveDirection;
            inputData.ValueRW.isCrouching = isCrouching;

            // Only ever set to true here. PlayerMoverSystem sets it back to false after reacting,
            // so a press is not lost on a frame where physics doesn't step.
            if (jumpPressed)
            {
                inputData.ValueRW.jumpRequested = true;
            }
            if (dashPressed)
            {
                inputData.ValueRW.dashRequested = true;
            }
        }
    }
}
