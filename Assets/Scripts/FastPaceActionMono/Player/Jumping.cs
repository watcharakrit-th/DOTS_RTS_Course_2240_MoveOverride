using static PlayerStateMachine;

// In the air after a jump, until landing. Steers at run speed while Sprint is held, otherwise at walk speed.
public class Jumping : PlayerState
{
    public Jumping(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void EnterState()
    {
        Context.Jump();
        Context.Animator.SetBool(PlayerContext.IsJumpingHash, true);
    }

    public override void ExitState()
    {
        Context.Animator.SetBool(PlayerContext.IsJumpingHash, false);
    }

    public override void UpdateState()
    {
        Context.Move(Context.IsSprintPressed ? Context.Config.RunSpeed : Context.Config.WalkSpeed);
        Context.FaceMoveDirection();
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
}
