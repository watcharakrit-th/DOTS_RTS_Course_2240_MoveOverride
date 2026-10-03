using UnityEngine;
using UnityEngine.InputSystem;

// Player character movement for the GameObject-only game (no ECS): move, sprint, jump, gravity.
// Uses a CharacterController (PhysX), so it collides with normal GameObject colliders.
// Move input is relative to the camera: "forward" always means away from the camera, whatever angle the Cinemachine camera has.
// Moves in Update. The CinemachineBrain moves the camera in LateUpdate, so the camera follows this frame's position without jitter.
[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [Tooltip("How fast speed changes, in m/s per second. Higher = snappier starts and stops.")]
    [SerializeField] private float acceleration = 60f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float turnSpeed = 1080f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -25f;

    [Header("Camera")]
    [Tooltip("Move input is relative to this. Leave empty to use the Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private Vector3 horizontalVelocity;
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
        jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
        playerMap.Enable();
    }

    private void Update()
    {
        Vector3 moveDirection = GetMoveDirection();

        // Turn to face the way we're moving
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Speed up or slow down toward the velocity the input asks for
        float speed = sprintAction.IsPressed() ? sprintSpeed : moveSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDirection * speed, acceleration * Time.deltaTime);

        // Jump and gravity
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                // Small downward push keeps the controller on the ground, so isGrounded stays true
                verticalVelocity = -2f;
            }

            if (jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else if (verticalVelocity > 0f && (characterController.collisionFlags & CollisionFlags.Above) != 0)
        {
            // Hit a ceiling while going up: start falling now instead of sticking to it
            verticalVelocity = 0f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    // Move input as a direction on the ground (length 0 to 1), relative to the camera
    private Vector3 GetMoveDirection()
    {
        // Clamp so diagonal keys aren't faster than straight ones. A half-tilted stick still moves at half speed.
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        // Without a camera, use world axes
        if (cameraTransform == null)
        {
            return direction;
        }

        // Use only the camera's yaw. Its pitch (tilted down at the player) would otherwise shorten "forward" and slow us down.
        return Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * direction;
    }
}
