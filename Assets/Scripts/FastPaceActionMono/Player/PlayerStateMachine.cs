using UnityEngine;

// The player character's state machine (GameObject-only game, no ECS). Put it on the character root.
// Each state (Idling, Walking, ...) does its own movement and animation, and decides which state comes next.
// This component only creates the shared PlayerContext and registers the states.
// States run in Update; the CinemachineBrain moves the camera in LateUpdate, so the camera follows without jitter.
[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : StateManager<PlayerStateMachine.EPlayerState>
{
    public enum EPlayerState
    {
        Idling,
        Walking,
        Running,
        Jumping,
        Crouching,
        Dashing,
        Climbing,
        Attacking,
        Aiming
    }

    [SerializeField] private PlayerConfig _config = new PlayerConfig();
    [Tooltip("Move input is relative to this. Leave empty to use the Main Camera.")]
    [SerializeField] private Transform _cameraTransform;
    [Tooltip("UI crosshair shown only while aiming. Optional. Drag it in from the Hierarchy, not the Project window.")]
    [SerializeField] private GameObject _crosshair;

    private PlayerContext _context;

    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }

        // The Animator can be on this GameObject or on a child (the model)
        _context = new PlayerContext(_config, transform, GetComponent<CharacterController>(),
            GetComponentInChildren<Animator>(), _cameraTransform, _crosshair);
        InitializeStates();
    }

    private void InitializeStates()
    {
        States.Add(EPlayerState.Idling, new Idling(_context, EPlayerState.Idling));
        States.Add(EPlayerState.Walking, new Walking(_context, EPlayerState.Walking));
        States.Add(EPlayerState.Running, new Running(_context, EPlayerState.Running));
        States.Add(EPlayerState.Jumping, new Jumping(_context, EPlayerState.Jumping));
        States.Add(EPlayerState.Crouching, new Crouching(_context, EPlayerState.Crouching));
        // Not implemented yet. No state returns these from GetNextState, so they're never needed.
        // States.Add(EPlayerState.Dashing, new Dashing(_context, EPlayerState.Dashing));
        // States.Add(EPlayerState.Climbing, new Climbing(_context, EPlayerState.Climbing));
        // States.Add(EPlayerState.Attacking, new Attacking(_context, EPlayerState.Attacking));
        States.Add(EPlayerState.Aiming, new Aiming(_context, EPlayerState.Aiming));

        CurrentState = States[EPlayerState.Idling];
    }
}
