# Player scripts: how they work

## Folders

| Folder | What |
|---|---|
| `MyAuthoring/`, `MySystem/`, `MyMonoBehaviours/` | My player code (this README) |
| `MyNetcodeScripts/` | Server + client connection setup. Netcode guide: `MyNetcodeScripts/README.md` |
| `Authoring/`, `Systems/`, `MonoBehaviours/`, `UI/` | Course example code. Reference only, don't edit. |

## Two ways to run the player

The scene currently has **both**. Both read the same keys, so both move at the same time.

| | A. GameObject only | B. Hybrid (DOTS logic + GameObject visual) |
|---|---|---|
| Moves the player | `BasicMovement` (PhysX `CharacterController`) | `PlayerMoverSystem` (DOTS physics) |
| Animation | `BasicMovementAnimator` reads `BasicMovement` | `PlayerVisualSync` reads the entity |
| Player lives in | Main scene (Y Bot) | Subscene entity (Cube) + main scene Y Bot as the visual |
| Physics world | PhysX | DOTS (Unity Physics) |

Pick one. To use **B**:
1. Subscene: the player object has `PlayerAuthoring`, `Rigidbody` (Interpolate on), a Collider, and no animated model.
2. Main scene Y Bot: remove `BasicMovement`, `BasicMovementAnimator`, `CharacterController`. Add `PlayerVisualSync`. Keep the tag `Player` so the camera follows it.

## Order inside one frame (B, hybrid)

```
1. PreUpdate          Input System reads keyboard/gamepad
2. Update  (DOTS)     PlayerInputSystem          buttons -> PlayerInputData
                      Fixed step, 0..n times:
                        PlayerMoverSystem        PlayerInputData -> velocity, rotation, PlayerMoveState
                        Physics                  velocity -> new position, collisions
                      Transform systems          position -> LocalToWorld (smoothed)
3. LateUpdate (Mono)  PlayerVisualSync (-100)    LocalToWorld -> Y Bot transform, state -> Animator
                      CameraFollowPlayer         follows Y Bot
4. Render
```

- **Fixed step:** physics runs at a fixed rate (e.g. 60/s), not once per frame. On a fast frame it may run 0 times, on a slow frame 2+ times. That's why jump/dash are "requests" that stay `true` until the mover uses them.
- **LateUpdate:** the only MonoBehaviour moment that is after DOTS has moved the entity.

## Data flow (B)

```
 keys ──► PlayerInputSystem ──► PlayerInputData ──► PlayerMoverSystem ──► PhysicsVelocity ──► Physics
                                 (what I WANT)              │                                  │
                                                            ▼                                  ▼
                                                     PlayerMoveState                     LocalToWorld
                                                     (what I AM doing)                         │
                                                            └──────────► PlayerVisualSync ◄────┘
                                                                          (Y Bot + Animator)
```

One direction only. The GameObject never writes back to DOTS.

## Components on the player entity

All defined in `MyAuthoring/PlayerAuthoring.cs`.

| Component | Contains | Written by | Read by |
|---|---|---|---|
| `Player` | Nothing (tag) | Baker | Queries, to find the player |
| `PlayerMovement` | Speeds, jump, dash settings | Baker (once) | `PlayerMoverSystem`, `PlayerVisualSync` |
| `PlayerStats` | Defense, damage, attack speed | Baker (once) | Nothing yet |
| `PlayerInputData` | Move direction, crouch held, jump/dash requested | `PlayerInputSystem` | `PlayerMoverSystem`, `PlayerVisualSync` |
| `PlayerMoveState` | Grounded, dashing, dash timers | `PlayerMoverSystem` | `PlayerVisualSync` |
| `PhysicsVelocity`, `PhysicsMass`, `PhysicsCollider` | Physics data | Baked from Rigidbody/Collider | `PlayerMoverSystem`, physics |

## Rules this code follows

1. **One owner per piece of data.** Only one script writes it. Position belongs to DOTS in B, to `BasicMovement` in A.
2. **Settings, input and state are separate components.** Settings never change, input is what you want, state is what happened.
3. **Held vs. one-shot input.** Held (move, crouch) is overwritten every frame. One-shot (jump, dash) is set `true` by input, set `false` by the system that reacts.
4. **Animation reads what happened, not the keys.** Jump pressed mid-air doesn't play the jump animation, because no jump happened.
5. **Managed code stays out of Burst.** `PlayerInputSystem` touches `Camera` and `InputAction` (classes), so it's a `SystemBase`. `PlayerMoverSystem` only uses structs, so it's Burst compiled.

## Things to know

- **Two physics worlds.** `CameraFollowPlayer` and `BasicMovement` use PhysX: they only see colliders in the main scene (`Plane`, `Ground`). The DOTS player only sees colliders in the subscene.
- **Inspector values.** Default values in `PlayerAuthoring` only apply when the component is newly added. An existing object keeps its old values. Right-click the component → **Reset**, or type them in.
- **Ground check** is one ray straight down from the center. Standing on an edge can count as "not grounded".
- **Rotation lock.** Rigidbody "Freeze Rotation" isn't baked into DOTS physics, so `PlayerMoverSystem` sets `PhysicsMass.InverseInertia` to zero instead.
