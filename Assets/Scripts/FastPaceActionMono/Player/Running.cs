using static PlayerStateMachine;

// Moving at run speed while Sprint is held, facing the way we move.
public class Running : PlayerState
{
    public Running(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void UpdateState()
    {
        Context.Move(Context.Config.RunSpeed);
        Context.FaceMoveDirection();
        Context.UpdateMoveAnimation();
    }

    public override EPlayerState GetNextState()
    {
        if (Context.WasJumpPressed && Context.IsGrounded) return EPlayerState.Jumping;
        if (Context.IsAimPressed) return EPlayerState.Aiming;
        if (Context.IsCrouchPressed) return EPlayerState.Crouching;
        if (!Context.IsMovePressed) return EPlayerState.Idling;
        if (!Context.IsSprintPressed) return EPlayerState.Walking;
        return StateKey;
    }
}
