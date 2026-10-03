# Controllable units: how they work

## Folders

| Folder | What |
|---|---|
| `MyAuthoring/`, `MySystem/`, `MyMonoBehaviours/` | My unit code (this README) |
| `MyNetcodeScripts/` | Server + client connection setup. Netcode guide: `MyNetcodeScripts/README.md` |
| `Authoring/`, `Systems/`, `MonoBehaviours/`, `UI/` | Course example code. Reference only, don't edit. |

## The idea: "IControllable" is a set of components

Entities can't have C# interfaces, and queries can't search for one. So "IControllable" is **`ControllableAuthoring`**: put it on any unit prefab (main character, summon, decoy, dummy) and it bakes a set of components. Every system below works on any entity that has them.

- **Same code for every unit.** What makes a summon different from the main character is values on the prefab (speed, jump, dash), not code.
- **Each player controls exactly one unit at a time.** The **server** decides which one. Clients send input to it and predict it.
- **Brain and body are separate.** `CharacterInputSystem` (the brain) writes what the player wants. `CharacterMoverSystem` (the body) moves any unit from its input and doesn't care who wrote it.

## Components on a unit

All defined in `MyAuthoring/ControllableAuthoring.cs`, except where noted.

| Component | Contains | Written by | Read by |
|---|---|---|---|
| `Controllable` | Display name. The "IControllable" marker | Baker | Queries, `UnitSwitchMenu` |
| `ControlledByPlayer` | Enableable, no data. On = the unit its owner controls right now. Synced to clients (`[GhostEnabledBit]`) | `ControlSwitchSystem` (server) | `CharacterInputSystem`, `CharacterVisual`, `UnitSwitchMenu` |
| `RequestControl` | Enableable, no data. On = "give this unit's owner control of it". Server only | Gameplay code, `TakeControlRpcSystem` | `ControlSwitchSystem` |
| `CharacterInput` | Move direction, crouch held, jump/dash events. `IInputComponentData`: Netcode sends it to the server | `CharacterInputSystem` (client) | `CharacterMoverSystem` |
| `CharacterMovement` | Speeds, jump, dash settings | Baker (once) | `CharacterMoverSystem`, `CharacterVisual` |
| `CharacterStats` | Defense, damage, attack speed | Baker (once) | Nothing yet |
| `CharacterMoveState` | Grounded, crouching, dashing, dash timers. `[GhostField]`s | `CharacterMoverSystem` | `CharacterVisual` |
| `CharacterVisualPrefab` | The visual GameObject prefab. Client only (`CharacterVisualAuthoring.cs`) | Baker | `CharacterVisualSystem` |
| `PhysicsVelocity`, `PhysicsMass`, `PhysicsCollider` | Physics data | Baked from Rigidbody/Collider | `CharacterMoverSystem`, physics |
| `GhostOwner` | `NetworkId` of the player who owns the unit | Server, when spawning | Netcode, `ControlSwitchSystem` |

On the **server's connection entity** (one per player): `PlayerMainCharacter`, the unit control falls back to.

## Who does what

| Script | Runs on | When | Does |
|---|---|---|---|
| `GoInGameServerSystem` | Server | A player joins | Spawns their main character: a ghost with `GhostOwner` = their `NetworkId` |
| `CharacterInputSystem` | Client | `GhostInputSystemGroup`, every frame | Keys + camera → `CharacterInput` of the unit you control. Your other units get empty input, so they stand still |
| `CharacterMoverSystem` | Server + owning client | Prediction loop, fixed step, before physics | `CharacterInput` → velocity, rotation, `CharacterMoveState` |
| `TakeControlRpcSystem` | Server | A `TakeControlRpc` arrives | Checks the unit is yours → turns on its `RequestControl` |
| `ControlSwitchSystem` | Server | Every frame | `RequestControl` → that unit becomes the controlled one (one per player). Controlling nothing → main character |
| `CharacterVisualSystem` | Client | Presentation | Spawns a visual GameObject per unit, destroys it when the unit is gone |
| `CharacterVisual` | Client (MonoBehaviour) | LateUpdate, order -100 | Entity → GameObject position and Animator |
| `CameraFollowPlayer` | Client (MonoBehaviour) | LateUpdate | Follows the unit you control |
| `UnitSwitchMenu` | Client (MonoBehaviour) | Tab | Lists your units, click one → sends `TakeControlRpc` |
| `DebugSummonSystems` | Both | F1 / F2 | TEMPORARY test tool: spawn a summon and take control / destroy it |

## Switching control

```
 CLIENT                                            SERVER
 UnitSwitchMenu ──── TakeControlRpc ─────────────► TakeControlRpcSystem ─┐
                                                   gameplay code (summon) ┼─► RequestControl
                                                                          ▼
                                                   ControlSwitchSystem ──► ControlledByPlayer
 CharacterInputSystem ◄─────────── snapshots ───── (enabled bit, synced)
   └─► CharacterInput ──────── input stream ─────► CharacterMoverSystem (the truth)
 CharacterMoverSystem (prediction) ◄── corrections ┘
 LocalToWorld ─► CharacterVisual (-100) ─► CameraFollowPlayer (0)
```

The client only **asks**. It takes one round trip before input and camera move to the new unit, because the server decides.

## Order inside one frame (client)

```
1. PreUpdate          Input System reads keyboard/gamepad
2. Update  (DOTS)     CharacterInputSystem        keys -> CharacterInput (unit you control)
                      Netcode                     sends the input to the server
                      Prediction loop, per tick (can replay older ticks after a server correction):
                        CharacterMoverSystem      CharacterInput -> velocity, rotation, CharacterMoveState
                        Physics                   velocity -> new position, collisions
                      Transform systems           position -> LocalToWorld
3. LateUpdate (Mono)  CharacterVisual (-100)      LocalToWorld -> GameObject, state -> Animator
                      CameraFollowPlayer          follows the unit you control
                      UnitSwitchMenu              Tab: open/close the unit list
4. Presentation (DOTS) CharacterVisualSystem      spawn/destroy visual GameObjects (after LateUpdate)
5. Render
```

## How to...

**Add a unit type** (summon, decoy, dummy, another character):
1. Make a prefab (copy `PlayerECS`): Rigidbody (Interpolate on), a Collider, **Ghost Authoring** (Has Owner, Owner Predicted), **Controllable Authoring**, **Character Visual Authoring**.
2. Reference it from a component baked in **My Sub Scene** (e.g. `CharacterSpawnerAuthoring`). Netcode only knows ghost prefabs that are baked in a subscene. The client can't show a ghost whose prefab it doesn't know.

**Give a player control of a unit from code** (server only):
```csharp
entityCommandBuffer.SetComponentEnabled<RequestControl>(unitEntity, true);
```
The unit's owner (`GhostOwner`) gets control. `ControlSwitchSystem` turns their other units off and `RequestControl` back off.

**Spawn a unit for a player** (server only). See `DebugSummonServerSystem`:
```csharp
Entity unit = entityCommandBuffer.Instantiate(prefab);
entityCommandBuffer.SetComponent(unit, new GhostOwner { NetworkId = networkId });                          // theirs
entityCommandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = unit });               // gone when they leave
entityCommandBuffer.SetComponentEnabled<RequestControl>(unit, true);                                        // optional: take control
```

## Rules this code follows

1. **One owner per piece of data.** Only one script writes it. `ControlledByPlayer` only `ControlSwitchSystem`, `CharacterInput` only `CharacterInputSystem`.
2. **Server decides, client asks.** Anything that decides (who controls what, spawning, destroying) runs on the server. The client sends input and RPCs.
3. **Settings, input and state are separate components.** Settings never change, input is what you want, state is what happened.
4. **Held vs. one-shot input.** Held (move, crouch) is written every frame. One-shot (jump, dash) is an `InputEvent`: `Set()` on the press, `IsSet` is true for exactly one tick on the server and in prediction.
5. **Prediction code only reads components.** `CharacterMoverSystem` may replay old ticks, so it never reads keys, the camera or anything else outside its components.
6. **Animation reads what happened, not the keys.** Jump pressed mid-air doesn't play the jump animation, because no jump happened.
7. **Managed code stays out of Burst.** `CharacterInputSystem`, `CharacterVisualSystem` and `DebugSummonClientSystem` touch classes (`InputAction`, `Camera`, `GameObject`), so they're `SystemBase`. The rest is Burst compiled.

## Things to know

- **Two physics worlds.** `CameraFollowPlayer` uses PhysX: it only sees colliders in the main scene (`Plane`, `Ground`). Units only see colliders in the subscene.
- **Inspector values.** Default values in `ControllableAuthoring` only apply when the component is newly added. An existing object keeps its old values. Right-click the component → **Reset**, or type them in.
- **Ground check** is one ray straight down from the center. Standing on an edge can count as "not grounded".
- **Rotation lock.** Rigidbody "Freeze Rotation" isn't baked into DOTS physics, so `CharacterMoverSystem` sets `PhysicsMass.InverseInertia` to zero instead.
- **Units you don't control stand still.** Later, AI could drive them: add a `MoveIntent` component that both player input and AI write, and have the mover read that.
- **`BasicMovement` / `BasicMovementAnimator`** (GameObject-only movement) aren't networked. Kept for reference only.
