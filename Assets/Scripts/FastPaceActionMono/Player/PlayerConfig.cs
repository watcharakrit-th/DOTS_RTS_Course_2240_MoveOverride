using System;
using UnityEngine;

// Player tuning values. Edited in the Inspector on PlayerStateMachine (the "Config" foldout).
// States read them through Context.Config.
[Serializable]
public class PlayerConfig
{
    [Header("Move")]
    [SerializeField] private float _walkSpeed = 3f;
    [SerializeField] private float _runSpeed = 10f;
    [SerializeField] private float _crouchSpeed = 2.5f;
    [Tooltip("How fast speed changes, in m/s per second. Higher = snappier starts and stops.")]
    [SerializeField] private float _acceleration = 60f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float _turnSpeed = 1080f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _gravity = -25f;
    [Tooltip("Jumps in total before landing: 1 = single, 2 = double, 3 = triple.")]
    [Min(1)]
    [SerializeField] private int _maxJumps = 3;
    [Tooltip("Height of each extra jump in the air (2nd, 3rd...).")]
    [SerializeField] private float _airJumpHeight = 1.2f;
    [Tooltip("Seconds after walking off a ledge that Jump still counts as a normal ground jump. "
        + "After that, the fall counts as the first jump.")]
    [SerializeField] private float _coyoteTime = 0.1f;
    [Tooltip("Degrees to each side of the way the character faces. An air jump steered within this cone plays airJump; "
        + "further off plays airJumpTurn and turns the character to the new direction.")]
    [SerializeField] private float _airJumpConeAngle = 30f;
    [Tooltip("Move speed multiplier while an air-jump animation plays (Animator states tagged \"AirJump\"). 2 = twice as fast.")]
    [SerializeField] private float _airJumpSpeedMultiplier = 2f;

    [Header("Animation")]
    [Tooltip("Seconds the Animator's Velocity parameter takes to catch up with the real speed. Smooths the walk/run blend.")]
    [SerializeField] private float _velocityDampTime = 0.1f;

    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public float CrouchSpeed => _crouchSpeed;
    public float Acceleration => _acceleration;
    public float TurnSpeed => _turnSpeed;
    public float JumpHeight => _jumpHeight;
    public float Gravity => _gravity;
    public int MaxJumps => _maxJumps;
    public float AirJumpHeight => _airJumpHeight;
    public float CoyoteTime => _coyoteTime;
    public float AirJumpConeAngle => _airJumpConeAngle;
    public float AirJumpSpeedMultiplier => _airJumpSpeedMultiplier;
    public float VelocityDampTime => _velocityDampTime;
}
