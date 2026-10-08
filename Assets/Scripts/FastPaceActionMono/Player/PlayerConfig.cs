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
    public float VelocityDampTime => _velocityDampTime;
}
