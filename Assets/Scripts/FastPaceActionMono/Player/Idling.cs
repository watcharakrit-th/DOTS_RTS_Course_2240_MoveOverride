using static PlayerStateMachine;

// Standing still. Slows down to a stop if the player was moving.
public class Idling : PlayerState
{
    public Idling(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void UpdateState()
    {
        Context.Move(0f);
        Context.UpdateMoveAnimation();
    }

    public override EPlayerState GetNextState()
    {
        if (Context.WasJumpPressed && Context.IsGrounded) return EPlayerState.Jumping;
        if (Context.IsAimPressed) return EPlayerState.Aiming;
        if (Context.IsCrouchPressed) return EPlayerState.Crouching;
        if (Context.IsMovePressed) return Context.IsSprintPressed ? EPlayerState.Running : EPlayerState.Walking;
        return StateKey;
    }
}
