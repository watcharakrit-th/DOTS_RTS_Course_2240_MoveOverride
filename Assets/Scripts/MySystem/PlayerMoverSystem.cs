using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

// Turns PlayerInputData into movement.
// Runs in the fixed-step group right before physics, so every physics step gets fresh velocity
// and speed does not depend on frame rate.
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
partial struct PlayerMoverSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        PlayerMoverJob playerMoverJob = new PlayerMoverJob
        {
            // Inside the fixed-step group this is the fixed step time (e.g. 1/60), not the frame time
            deltaTime = SystemAPI.Time.DeltaTime,
            // DOTS physics world, used for the ground check
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
        };
        playerMoverJob.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(Player))]
public partial struct PlayerMoverJob : IJobEntity
{
    // How far below the bottom of the collider still counts as "standing on ground"
    private const float GroundCheckDistance = 0.3f;

    public float deltaTime;
    [ReadOnly] public CollisionWorld collisionWorld;

    public void Execute(
        Entity entity,
        ref LocalTransform localTransform,
        ref PhysicsVelocity physicsVelocity,
        ref PhysicsMass physicsMass,
        ref PlayerInputData inputData,
        ref PlayerMoveState moveState,
        in PlayerMovement movement,
        in PhysicsCollider physicsCollider)
    {
        // ---------- Ground check ----------

        moveState.isGrounded = IsGrounded(entity, localTransform, physicsCollider);

        // ---------- Dash ----------

        // Start a dash only if dash was pressed AND the cooldown has finished
        if (inputData.dashRequested && moveState.dashCooldownTimer <= 0f)
        {
            moveState.dashTimer = movement.dashDuration;
            moveState.dashCooldownTimer = movement.dashCooldown;
        }
        // Consume the one-shot request
        inputData.dashRequested = false;

        // Checked before counting down so the first step of the dash counts
        moveState.isDashing = moveState.dashTimer > 0f;
        moveState.dashTimer = math.max(0f, moveState.dashTimer - deltaTime);
        moveState.dashCooldownTimer = math.max(0f, moveState.dashCooldownTimer - deltaTime);

        // ---------- Horizontal movement and rotation ----------

        float3 moveDirection = inputData.moveDirection;
        // Treat tiny values (stick drift) as "no input"
        bool hasMoveInput = math.lengthsq(moveDirection) > 0.0001f;

        float3 horizontalVelocity = float3.zero;
        if (hasMoveInput)
        {
            // Length 1 so speed is the same in every direction
            moveDirection = math.normalize(moveDirection);

            float speed = movement.moveSpeed;
            if (inputData.isCrouching)
            {
                speed *= movement.crouchSpeedMultiplier;
            }
            // Dash overrides normal and crouch speed
            if (moveState.isDashing)
            {
                speed = movement.dashSpeed;
            }
            horizontalVelocity = moveDirection * speed;

            // Turn toward the move direction. saturate keeps the blend amount between 0 and 1
            localTransform.Rotation = math.slerp(
                localTransform.Rotation,
                quaternion.LookRotation(moveDirection, math.up()),
                math.saturate(deltaTime * movement.rotationSpeed));
        }

        // ---------- Vertical movement (gravity and jump) ----------

        // Keep the current vertical velocity so gravity keeps working
        float verticalVelocity = physicsVelocity.Linear.y;
        if (inputData.jumpRequested && moveState.isGrounded)
        {
            verticalVelocity = movement.jumpForce;
        }
        // Consume the one-shot request, even mid-air, so it isn't stored for later
        inputData.jumpRequested = false;

        // ---------- Apply to physics ----------

        physicsVelocity.Linear = new float3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

        // Infinite inertia: collisions can never tip or spin the player. We rotate it ourselves above.
        // (Rigidbody "Freeze Rotation" is not baked into DOTS physics, so it's done here.)
        physicsMass.InverseInertia = float3.zero;
        physicsVelocity.Angular = float3.zero;
    }

    // Raycast straight down from the center to just below the bottom of the collider
    private bool IsGrounded(Entity self, LocalTransform localTransform, PhysicsCollider physicsCollider)
    {
        // Collider bounds relative to the entity. Min.y is the bottom (negative number).
        float colliderBottom = physicsCollider.Value.Value.CalculateAabb().Min.y * localTransform.Scale;

        RaycastInput raycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = localTransform.Position + new float3(0f, colliderBottom - GroundCheckDistance, 0f),
            Filter = CollisionFilter.Default,
        };

        NativeList<RaycastHit> hits = new NativeList<RaycastHit>(Allocator.Temp);
        collisionWorld.CastRay(raycastInput, ref hits);
        foreach (RaycastHit hit in hits)
        {
            // The ray starts inside our own collider, so ignore hits on ourselves
            if (hit.Entity != self)
            {
                return true;
            }
        }
        return false;
    }
}
