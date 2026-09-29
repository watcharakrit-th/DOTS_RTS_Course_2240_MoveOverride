using UnityEngine;
using UnityEngine.InputSystem;

// GameObject-only player movement using PhysX (CharacterController). Not connected to DOTS.
// Reads the same InputSystem_Actions as PlayerInputSystem.
[RequireComponent(typeof(CharacterController))]
public class BasicMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float turnSpeed = 1080f; // degrees per second
    [SerializeField] private Transform cameraTransform;

    // What the character did this frame. Read by BasicMovementAnimator.
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }
    // True from the moment a jump starts until the character lands again
    public bool IsJumping { get; private set; }

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction jumpAction;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Project-wide actions asset (InputSystem_Actions), "Player" action map
        InputActionMap playerMap = InputSystem.actions.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: true);
        crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: true);
        jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
        playerMap.Enable();
    }

    private void Update()
    {
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        // Move relative to the camera's facing direction, flattened onto the ground plane.
        // Without a camera, use world axes (not our own transform, since we rotate to face movement).
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }
        Vector3 moveDirection = right * input.x + forward * input.y;

        IsMoving = moveDirection.sqrMagnitude > 0.0001f;

        // Turn smoothly to face the movement direction
        if (IsMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Crouch wins over run
        IsCrouching = crouchAction.IsPressed();
        IsRunning = IsMoving && !IsCrouching && sprintAction.IsPressed();

        float speed = walkSpeed;
        if (IsCrouching)
        {
            speed = crouchSpeed;
        }
        else if (IsRunning)
        {
            speed = runSpeed;
        }

        // Jump and gravity
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // Small downward force keeps the controller snapped to the ground
                verticalVelocity = -2f;

                // Landed
                IsJumping = false;
            }

            if (jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                IsJumping = true;
            }
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }
}
