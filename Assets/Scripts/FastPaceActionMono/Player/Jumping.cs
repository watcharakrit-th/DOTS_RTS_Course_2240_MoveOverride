using UnityEngine;
using static PlayerStateMachine;

// In the air after a jump, until landing. Steers at run speed while Sprint is held, otherwise at walk speed.
// Allows Config.MaxJumps jumps in total (double/triple jump). Each extra Jump press in the air is an air jump:
// steered within the cone of the way the character faces, it plays airJump; further off, it plays airJumpTurn
// and the character turns to the new direction. Between air jumps the character keeps facing the jump direction.
// While either air-jump animation plays, move speed is multiplied by Config.AirJumpSpeedMultiplier.
public class Jumping : PlayerState
{
    private int _jumpCount;
    // Frame this state started, so the Jump press that started it isn't also counted as an air jump
    private int _enterFrame;
    // The way the character faces in the air. Only a turning air jump changes it.
    private Vector3 _jumpDirection;

    public Jumping(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void EnterState()
    {
        // From the ground (or just off a ledge) this is jump 1. Later in a fall, the fall already used jump 1.
        bool fromGround = Context.CanGroundJump;
        _jumpCount = fromGround ? 1 : 2;
        Context.Jump(fromGround ? Context.Config.JumpHeight : Context.Config.AirJumpHeight);

        // Face the way the player steers at takeoff, or keep the current facing without input
        Vector3 moveDirection = Context.MoveDirection;
        _jumpDirection = moveDirection.sqrMagnitude > 0.0001f
            ? moveDirection.normalized
            : Vector3.ProjectOnPlane(Context.Transform.forward, Vector3.up).normalized;

        _enterFrame = Time.frameCount;
        Context.Animator.SetBool(PlayerContext.IsJumpingHash, true);
    }

    public override void ExitState()
    {
        Context.Animator.SetBool(PlayerContext.IsJumpingHash, false);
        // A Jump press just before landing shouldn't play an air jump later
        Context.Animator.ResetTrigger(PlayerContext.AirJumpHash);
        Context.Animator.ResetTrigger(PlayerContext.AirJumpTurnHash);
    }

    public override void UpdateState()
    {
        if (Time.frameCount != _enterFrame && Context.WasJumpPressed && _jumpCount < Context.Config.MaxJumps)
        {
            AirJump();
        }

        float speed = Context.IsSprintPressed ? Context.Config.RunSpeed : Context.Config.WalkSpeed;
        // Faster while an air-jump animation plays (states tagged "AirJump"), for that clip's whole length
        if (Context.IsPlayingAirJump)
        {
            speed *= Context.Config.AirJumpSpeedMultiplier;
        }
        Context.Move(speed);
        Context.FaceDirection(_jumpDirection);
        Context.UpdateMoveAnimation();
    }

    public override EPlayerState GetNextState()
    {
        // Still in the air, or still going up
        if (!Context.IsGrounded || Context.VerticalVelocity > 0f) return StateKey;

        // Landed
        if (Context.IsAimPressed) return EPlayerState.Aiming;
        if (Context.IsCrouchPressed) return EPlayerState.Crouching;
        if (!Context.IsMovePressed) return EPlayerState.Idling;
        return Context.IsSprintPressed ? EPlayerState.Running : EPlayerState.Walking;
    }

    private void AirJump()
    {
        _jumpCount++;
        Context.Jump(Context.Config.AirJumpHeight);

        // No input, or still steering within the cone of the way the character faces: clip 1 (airJump).
        // Steering further off: clip 2 (airJumpTurn), and the character turns to the new direction.
        Vector3 moveDirection = Context.MoveDirection;
        if (moveDirection.sqrMagnitude < 0.0001f
            || Vector3.Angle(_jumpDirection, moveDirection) <= Context.Config.AirJumpConeAngle)
        {
            Context.Animator.SetTrigger(PlayerContext.AirJumpHash);
        }
        else
        {
            Context.Animator.SetTrigger(PlayerContext.AirJumpTurnHash);
            _jumpDirection = moveDirection.normalized;
        }
    }
}
