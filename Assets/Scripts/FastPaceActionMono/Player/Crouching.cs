using static PlayerStateMachine;

// Moving slowly while Crouch is held (Crouching / Sneak Walk animations).
public class Crouching : PlayerState
{
    public Crouching(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void EnterState()
    {
        Context.Animator.SetBool(PlayerContext.IsCrouchingHash, true);
    }

    public override void ExitState()
    {
        Context.Animator.SetBool(PlayerContext.IsCrouchingHash, false);
    }

    public override void UpdateState()
    {
        Context.Move(Context.Config.CrouchSpeed);
        Context.FaceMoveDirection();
        Context.UpdateCrouchAnimation();
    }

    // No jumping while crouched: YBotController has no Crouching -> Jump transition
    public override EPlayerState GetNextState()
    {
        if (Context.IsAimPressed) return EPlayerState.Aiming;
        if (Context.IsCrouchPressed) return StateKey;
        if (!Context.IsMovePressed) return EPlayerState.Idling;
        return Context.IsSprintPressed ? EPlayerState.Running : EPlayerState.Walking;
    }
}
