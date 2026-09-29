using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

// Hybrid setup: the DOTS player entity does all the logic, this GameObject (e.g. Y Bot) only shows it.
// Every frame: copy position/rotation from the entity, and set Animator parameters from the entity's state.
// Only READS from DOTS, never writes back.
// Execution order -100: runs before CameraFollowPlayer, so the camera follows this frame's position.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Animator))]
public class PlayerVisualSync : MonoBehaviour
{
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");

    [Tooltip("How fast the Animator's Velocity parameter follows the real speed.")]
    [SerializeField] private float velocityChangeRate = 4f;

    private Animator animator;
    private EntityManager entityManager;
    private EntityQuery playerQuery;
    private float velocity;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (TryGetComponent(out BasicMovement basicMovement) && basicMovement.enabled)
        {
            Debug.LogWarning("PlayerVisualSync and BasicMovement both move this GameObject. " +
                "Remove BasicMovement, BasicMovementAnimator and CharacterController.", this);
        }
    }

    private void Start()
    {
        // The bridge: the DOTS world, reached from a GameObject script
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        playerQuery = entityManager.CreateEntityQuery(typeof(Player), typeof(LocalToWorld));
    }

    // LateUpdate runs after the DOTS simulation, so the entity has already moved this frame
    private void LateUpdate()
    {
        // False while the subscene is still loading
        if (!playerQuery.TryGetSingletonEntity<Player>(out Entity playerEntity))
        {
            return;
        }

        // 1. Position and rotation: entity -> GameObject
        // LocalToWorld (not LocalTransform) includes physics smoothing when the Rigidbody has Interpolate on
        LocalToWorld localToWorld = entityManager.GetComponentData<LocalToWorld>(playerEntity);
        transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);

        // 2. Animation: entity data -> Animator parameters
        PlayerMovement movement = entityManager.GetComponentData<PlayerMovement>(playerEntity);
        PlayerInputData inputData = entityManager.GetComponentData<PlayerInputData>(playerEntity);
        PlayerMoveState moveState = entityManager.GetComponentData<PlayerMoveState>(playerEntity);
        PhysicsVelocity physicsVelocity = entityManager.GetComponentData<PhysicsVelocity>(playerEntity);

        // Blend tree: 0 = idle, 0.5 = walk (at moveSpeed), 1 = run (dash is faster, so it shows run)
        float horizontalSpeed = math.length(physicsVelocity.Linear.xz);
        float targetVelocity = 0f;
        if (movement.moveSpeed > 0f)
        {
            targetVelocity = math.saturate(horizontalSpeed / movement.moveSpeed * 0.5f);
        }
        velocity = Mathf.MoveTowards(velocity, targetVelocity, velocityChangeRate * Time.deltaTime);

        animator.SetFloat(VelocityHash, velocity);
        animator.SetBool(IsCrouchingHash, inputData.isCrouching);
        animator.SetBool(IsJumpingHash, !moveState.isGrounded);
    }
}
