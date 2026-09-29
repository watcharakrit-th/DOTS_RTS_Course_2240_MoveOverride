using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Put this on the player GameObject inside a subscene.
// Baking turns the Inspector values into components on the player entity.
public class PlayerAuthoring : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2f;
    [Tooltip("How fast the player turns to face the move direction. Higher = snappier.")]
    [Min(0f)] public float rotationSpeed = 10f;
    [Tooltip("Upward speed at the start of a jump.")]
    [Min(0f)] public float jumpForce = 4f;
    [Tooltip("Move speed multiplier while crouching. 0.5 = half speed.")]
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.5f;

    [Header("Dash")]
    [Min(0f)] public float dashSpeed = 30f;
    [Tooltip("Seconds a dash lasts.")]
    [Min(0f)] public float dashDuration = 1f;
    [Tooltip("Seconds before you can dash again.")]
    [Min(0f)] public float dashCooldown = 1f;

    [Header("Combat (no system uses these yet)")]
    public float baseDefense;
    public float baseDamage;
    public float baseAttackSpeed;

    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Player());
            AddComponent(entity, new PlayerMovement
            {
                moveSpeed = authoring.moveSpeed,
                rotationSpeed = authoring.rotationSpeed,
                jumpForce = authoring.jumpForce,
                crouchSpeedMultiplier = authoring.crouchSpeedMultiplier,
                dashSpeed = authoring.dashSpeed,
                dashDuration = authoring.dashDuration,
                dashCooldown = authoring.dashCooldown,
            });
            AddComponent(entity, new PlayerStats
            {
                baseDefense = authoring.baseDefense,
                baseDamage = authoring.baseDamage,
                baseAttackSpeed = authoring.baseAttackSpeed,
            });

            // Runtime data, starts empty and gets filled in by systems
            AddComponent(entity, new PlayerInputData());
            AddComponent(entity, new PlayerMoveState());
        }
    }
}

// Tag: "this entity is controlled by the keyboard/gamepad".
// No data. Used by queries to find the player, like Unit or Friendly in the course code.
public struct Player : IComponentData
{
}

// Movement settings. Baked once, only read at runtime.
public struct PlayerMovement : IComponentData
{
    public float moveSpeed;
    public float rotationSpeed;
    public float jumpForce;
    public float crouchSpeedMultiplier;
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
}

// Combat settings. Baked once. No system reads them yet.
public struct PlayerStats : IComponentData
{
    public float baseDefense;
    public float baseDamage;
    public float baseAttackSpeed;
}

// What the player WANTS to do.
// Written by PlayerInputSystem, read by PlayerMoverSystem.
public struct PlayerInputData : IComponentData
{
    // Camera-relative direction on the ground (y is always 0), length 0 to 1
    public float3 moveDirection;

    // Held button: true every frame the button is down
    public bool isCrouching;

    // One-shot buttons: PlayerInputSystem sets true on the press,
    // PlayerMoverSystem sets back to false after it has reacted
    public bool jumpRequested;
    public bool dashRequested;
}

// What the player IS doing.
// Written by PlayerMoverSystem, read by anyone else (e.g. PlayerVisualSync for animation).
public struct PlayerMoveState : IComponentData
{
    public bool isGrounded;
    public bool isDashing;
    public float dashTimer;
    public float dashCooldownTimer;
}
