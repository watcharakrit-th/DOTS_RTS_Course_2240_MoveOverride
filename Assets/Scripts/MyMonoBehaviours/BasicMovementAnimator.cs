using UnityEngine;

// GameObject-only setup: drives the Animator from BasicMovement.
// Reads what the character DID (BasicMovement's state), not the keyboard,
// so the animation always matches the movement.
[RequireComponent(typeof(Animator), typeof(BasicMovement))]
public class BasicMovementAnimator : MonoBehaviour
{
    // "Velocity" values the YBotController blend trees and transitions expect
    private const float IdleVelocity = 0f;
    private const float CrouchWalkVelocity = 0.1f;
    private const float WalkVelocity = 0.5f;
    private const float RunVelocity = 1f;

    // Convert parameter names to ids once, instead of passing strings every frame
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");

    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float deceleration = 2f;

    private Animator animator;
    private BasicMovement basicMovement;
    private float velocity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        basicMovement = GetComponent<BasicMovement>();
    }

    // LateUpdate so BasicMovement.Update has already moved the character this frame
    private void LateUpdate()
    {
        float targetVelocity = GetTargetVelocity();
        // Speed up at the acceleration rate, slow down at the deceleration rate
        float rate = targetVelocity > velocity ? acceleration : deceleration;
        velocity = Mathf.MoveTowards(velocity, targetVelocity, rate * Time.deltaTime);

        animator.SetFloat(VelocityHash, velocity);
        animator.SetBool(IsCrouchingHash, basicMovement.IsCrouching);
        animator.SetBool(IsJumpingHash, basicMovement.IsJumping);
    }

    private float GetTargetVelocity()
    {
        if (!basicMovement.IsMoving)
        {
            return IdleVelocity;
        }
        if (basicMovement.IsCrouching)
        {
            return CrouchWalkVelocity;
        }
        if (basicMovement.IsRunning)
        {
            return RunVelocity;
        }
        return WalkVelocity;
    }
}
