using UnityEngine;
using static PlayerStateMachine;

// Base for every player state. Gives each state the shared PlayerContext.
// EnterState, ExitState and the trigger methods do nothing by default, so a state only overrides the ones it uses.
// UpdateState and GetNextState stay abstract: every state must say what it does and which state comes next.
public abstract class PlayerState : BaseState<EPlayerState>
{
    protected PlayerContext Context;

    public PlayerState(PlayerContext context, EPlayerState stateKey) : base(stateKey)
    {
        Context = context;
    }

    public override void EnterState() { }
    public override void ExitState() { }
    public override void OnTriggerEnter(Collider other) { }
    public override void OnTriggerStay(Collider other) { }
    public override void OnTriggerExit(Collider other) { }
}
