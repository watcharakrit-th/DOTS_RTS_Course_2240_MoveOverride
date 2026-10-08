using static PlayerStateMachine;

// Moving at walk speed, facing the way we move.
public class Walking : PlayerState
{
    public Walking(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void UpdateState()
    {
        Context.Move(Context.Config.WalkSpeed);
        Context.FaceMoveDirection();
        Context.UpdateMoveAnimation();
    }

    public override EPlayerState GetNextState()
    {
        if (Context.WasJumpPressed && Context.CanJump) return EPlayerState.Jumping;
        if (Context.IsAimPressed) return EPlayerState.Aiming;
        if (Context.IsCrouchPressed) return EPlayerState.Crouching;
        if (!Context.IsMovePressed) return EPlayerState.Idling;
        if (Context.IsSprintPressed) return EPlayerState.Running;
        return StateKey;
    }
}
