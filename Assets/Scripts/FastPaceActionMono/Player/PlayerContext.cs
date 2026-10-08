using UnityEngine;
using UnityEngine.InputSystem;

// Everything the player states share: settings, components, input, and the movement every state builds on.
// States read input and call the helpers here instead of touching the components themselves.
// The velocity lives here, not in a state, so it carries over when the state changes (e.g. the run speed you jumped with).
public class PlayerContext
{
    // Animator parameters in YBotController
    public static readonly int VelocityHash = Animator.StringToHash("Velocity");
    public static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    public static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
    // Switches on two layers: the UpperBody aim pose (which the State-Driven Camera watches) and the LowerBody strafe legs
    public static readonly int IsAimingHash = Animator.StringToHash("isAiming");
    // Move direction relative to the body, for the LowerBody layer's strafe blend tree
    public static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
    public static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");
    // Air jumps (2nd, 3rd...): airJump when still steering the way the character faces, airJumpTurn when changing direction
    public static readonly int AirJumpHash = Animator.StringToHash("airJump");
    public static readonly int AirJumpTurnHash = Animator.StringToHash("airJumpTurn");
    // Tag on the Air Jump and Air Jump Turn states in the Animator (state Inspector > Tag)
    public const string AirJumpTag = "AirJump";

    private readonly PlayerConfig _config;
    private readonly Transform _transform;
    private readonly CharacterController _characterController;
    private readonly Animator _animator;
    private readonly Transform _cameraTransform;
    private readonly GameObject _crosshair;

    private readonly InputAction _moveAction;
    private readonly InputAction _sprintAction;
    private readonly InputAction _crouchAction;
    private readonly InputAction _jumpAction;
    private readonly InputAction _aimAction;

    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    // 0 while on the ground, then counts up in the air (for coyote time)
    private float _timeSinceGrounded;

    public PlayerContext(PlayerConfig config, Transform transform, CharacterController characterController,
        Animator animator, Transform cameraTransform, GameObject crosshair)
    {
        _config = config;
        _transform = transform;
        _characterController = characterController;
        _animator = animator;
        _cameraTransform = cameraTransform;
        _crosshair = crosshair;

        // Project-wide actions asset (InputSystem_Actions), "Player" action map
        InputActionMap playerMap = InputSystem.actions.FindActionMap("Player", throwIfNotFound: true);
        _moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        _sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: true);
        _crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: true);
        _jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
        // Aim is E, which is bound to the "Interact" action (north button on a gamepad)
        _aimAction = playerMap.FindAction("Interact", throwIfNotFound: true);
        playerMap.Enable();

        SetCrosshairVisible(false);
    }

    public PlayerConfig Config => _config;
    public Transform Transform => _transform;
    public Animator Animator => _animator;

    // ---------- Input ----------

    public bool IsMovePressed => _moveAction.ReadValue<Vector2>().sqrMagnitude > 0.0001f;
    public bool IsSprintPressed => _sprintAction.IsPressed();
    public bool IsCrouchPressed => _crouchAction.IsPressed();
    // IsPressed follows the key itself, even though Interact has a Hold interaction
    public bool IsAimPressed => _aimAction.IsPressed();
    public bool WasJumpPressed => _jumpAction.WasPressedThisFrame();
    // Move input as a direction on the ground (length 0 to 1), relative to the camera
    public Vector3 MoveDirection => GetMoveDirection();

    // ---------- Movement ----------

    public bool IsGrounded => _characterController.isGrounded;
    // Up is positive. Negative while falling (and slightly negative while standing, to stay on the ground).
    public float VerticalVelocity => _verticalVelocity;
    // On the ground, or walked off a ledge less than Coyote Time ago: Jump is still a normal ground jump
    public bool CanGroundJump => _timeSinceGrounded <= _config.CoyoteTime;
    // Whether a grounded state may start a jump: from the ground, or during a fall when air jumps exist
    // (the fall then counts as the first jump)
    public bool CanJump => CanGroundJump || _config.MaxJumps > 1;

    // Speeds up or slows down toward the move input at this speed, applies gravity, and moves the character.
    // Call it once per frame from a state's UpdateState. Speed 0 slows down to a stop.
    public void Move(float speed)
    {
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, GetMoveDirection() * speed,
            _config.Acceleration * Time.deltaTime);

        if (_characterController.isGrounded && _verticalVelocity < 0f)
        {
            // Small downward push keeps the controller on the ground, so isGrounded stays true
            _verticalVelocity = -2f;
        }
        else if (_verticalVelocity > 0f && (_characterController.collisionFlags & CollisionFlags.Above) != 0)
        {
            // Hit a ceiling while going up: start falling now instead of sticking to it
            _verticalVelocity = 0f;
        }
        _verticalVelocity += _config.Gravity * Time.deltaTime;

        Vector3 velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
        _characterController.Move(velocity * Time.deltaTime);

        _timeSinceGrounded = _characterController.isGrounded ? 0f : _timeSinceGrounded + Time.deltaTime;
    }

    // Starts a jump this high. Move() then carries the character up, and gravity brings it back down.
    // Also works in the air (air jumps): it replaces the current up/down speed.
    public void Jump(float height)
    {
        _verticalVelocity = Mathf.Sqrt(height * -2f * _config.Gravity);
    }

    // Turns toward where the move input points. Does nothing without input.
    public void FaceMoveDirection()
    {
        FaceDirection(GetMoveDirection());
    }

    // Turns toward where the camera looks (used while aiming)
    public void FaceCamera()
    {
        if (_cameraTransform == null)
        {
            return;
        }
        FaceDirection(Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f) * Vector3.forward);
    }

    // Turns toward this direction at Turn Speed. Does nothing for a zero direction.
    public void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, _config.TurnSpeed * Time.deltaTime);
    }

    // Move input as a direction on the ground (length 0 to 1), relative to the camera
    private Vector3 GetMoveDirection()
    {
        // Clamp so diagonal keys aren't faster than straight ones. A half-tilted stick still moves at half speed.
        Vector2 input = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        // Without a camera, use world axes
        if (_cameraTransform == null)
        {
            return direction;
        }

        // Use only the camera's yaw. Its pitch (tilted down at the player) would otherwise shorten "forward" and slow us down.
        return Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f) * direction;
    }

    // ---------- Animation ----------

    // Velocity parameter while standing: 0 = idle, 0.5 = walk (at Walk Speed), 1 = run (at Run Speed).
    // Uses the speed the CharacterController really moved, so walking into a wall shows idle instead of running in place.
    public void UpdateMoveAnimation()
    {
        float speed = GetGroundSpeed();
        float velocity;
        if (speed <= _config.WalkSpeed)
        {
            velocity = 0.5f * Mathf.InverseLerp(0f, _config.WalkSpeed, speed);
        }
        else
        {
            velocity = 0.5f + 0.5f * Mathf.InverseLerp(_config.WalkSpeed, _config.RunSpeed, speed);
        }
        SetVelocityParameter(velocity);
    }

    // Velocity parameter while crouched: 0.2 at Crouch Speed, whatever the speeds are. Fits YBotController's crouch
    // transitions: Walk/Run -> Sneak Walk below 0.5, Sneak Walk -> Crouching below 0.15, Crouching -> Sneak Walk above 0.05.
    public void UpdateCrouchAnimation()
    {
        SetVelocityParameter(0.2f * Mathf.InverseLerp(0f, _config.CrouchSpeed, GetGroundSpeed()));
    }

    // VelocityX / VelocityZ for the strafe blend tree: the move direction relative to where the body faces,
    // in walk-speed units. X: 1 = walking right, -1 = left. Z: 1 = walking forward, -1 = backward.
    public void UpdateStrafeAnimation()
    {
        Vector3 localVelocity = _transform.InverseTransformDirection(GetGroundVelocity()) / Mathf.Max(_config.WalkSpeed, 0.01f);
        localVelocity = Vector3.ClampMagnitude(localVelocity, 1f);
        _animator.SetFloat(VelocityXHash, localVelocity.x, _config.VelocityDampTime, Time.deltaTime);
        _animator.SetFloat(VelocityZHash, localVelocity.z, _config.VelocityDampTime, Time.deltaTime);
    }

    // True while the Base Layer plays a state tagged "AirJump" and its clip hasn't finished yet,
    // including the short blend into it. Follows each clip's own length (Running Jump 0.9 s, Front Twist Flip 2.2 s).
    public bool IsPlayingAirJump
    {
        get
        {
            if (_animator.IsInTransition(0) && _animator.GetNextAnimatorStateInfo(0).IsTag(AirJumpTag))
            {
                return true;
            }
            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
            return current.IsTag(AirJumpTag) && current.normalizedTime < 1f;
        }
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (_crosshair != null)
        {
            _crosshair.SetActive(visible);
        }
    }

    // Velocity along the ground that the CharacterController really moved last Move(), after collisions
    private Vector3 GetGroundVelocity()
    {
        Vector3 groundVelocity = _characterController.velocity;
        groundVelocity.y = 0f;
        return groundVelocity;
    }

    private float GetGroundSpeed()
    {
        return GetGroundVelocity().magnitude;
    }

    private void SetVelocityParameter(float velocity)
    {
        _animator.SetFloat(VelocityHash, velocity, _config.VelocityDampTime, Time.deltaTime);
    }
}
