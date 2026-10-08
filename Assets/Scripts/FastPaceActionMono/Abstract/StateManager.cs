using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class StateManager<EState> : MonoBehaviour where EState : Enum
{
    protected Dictionary<EState, BaseState<EState>> States = new Dictionary<EState, BaseState<EState>>();
    protected BaseState<EState> CurrentState;

    protected bool IsTransitioningState = false;

    // protected virtual: a subclass that needs its own Start/Update overrides them and calls base.Start()/base.Update().
    // A plain "void Update()" in the subclass would silently replace this one and stop the state machine.
    protected virtual void Start() {
        CurrentState.EnterState();
    }

    protected virtual void Update(){
        EState nextStateKey = CurrentState.GetNextState();

        if (!IsTransitioningState && !nextStateKey.Equals(CurrentState.StateKey)) {
            TransitionToState(nextStateKey);
        }

        // Also runs right after a transition, so the new state updates in the same frame.
        // Otherwise every state change would skip one frame of movement (a visible hitch).
        if (!IsTransitioningState) {
            CurrentState.UpdateState();
        }
    }

    public void TransitionToState(EState stateKey)
    {
        IsTransitioningState = true;
        CurrentState.ExitState();
        CurrentState = States[stateKey];
        CurrentState.EnterState();
        IsTransitioningState = false;
    }

    void OnTriggerEnter(Collider other){
        CurrentState.OnTriggerEnter(other);
    }

    void OnTriggerStay(Collider other){
        CurrentState.OnTriggerStay(other);
    }

    void OnTriggerExit(Collider other){
        CurrentState.OnTriggerExit(other);
    }
}