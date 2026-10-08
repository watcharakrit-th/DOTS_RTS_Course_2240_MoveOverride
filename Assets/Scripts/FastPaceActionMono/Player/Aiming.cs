using static PlayerStateMachine;

// Aiming while E is held: walk speed, facing where the camera looks (strafing), crosshair shown.
// Sets the Animator's isAiming, which switches on two layers: UpperBody (aim pose) and LowerBody (strafe legs).
// The State-Driven Camera watches the UpperBody aim state, so it switches to the aim camera by itself.
public class Aiming : PlayerState
{
    public Aiming(PlayerContext context, EPlayerState stateKey) : base(context, stateKey) { }

    public override void EnterState()
    {
        Context.Animator.SetBool(PlayerContext.IsAimingHash, true);
        Context.SetCrosshairVisible(true);
    }

    public override void ExitState()
    {
        Context.Animator.SetBool(PlayerContext.IsAimingHash, false);
        Context.SetCrosshairVisible(false);
    }

    public override void UpdateState()
    {
        Context.Move(Context.Config.WalkSpeed);
        Context.FaceCamera();
        // Velocity keeps the base layer ready for when aiming ends; VelocityX/Z drive the LowerBody strafe legs
        Context.UpdateMoveAnimation();
        Context.UpdateStrafeAnimation();
    }

    public override EPlayerState GetNextState()
    {
        if (Context.WasJumpPressed && Context.CanJump) return EPlayerState.Jumping;
        if (Context.IsAimPressed) return StateKey;
        if (Context.IsCrouchPressed) return EPlayerState.Crouching;
        if (!Context.IsMovePressed) return EPlayerState.Idling;
        return Context.IsSprintPressed ? EPlayerState.Running : EPlayerState.Walking;
    }
}
