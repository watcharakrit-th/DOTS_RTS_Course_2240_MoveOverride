# Netcode for Entities: basic server + client

## Files

| File | What it does |
|---|---|
| `GameBootstrap.cs` | Runs at game start. Creates ServerWorld + ClientWorld and turns on auto-connect (port 7979). |
| `GoInGameClientSystem.cs` | Client: once connected and My Sub Scene has loaded, marks itself "in game" and sends `GoInGameRequest` to the server. Also defines `GoInGameRequest`. |
| `GoInGameServerSystem.cs` | Server: receives `GoInGameRequest`, marks that client "in game" and spawns their main character (a ghost they own). From then on it sends the client the game. |

## How to test

1. **Window → Multiplayer → Play Mode Tools** → Play Mode Type: **Client & Server**.
2. Press Play. The Console should show:
   ```
   'ClientWorld' connected as NetworkId 1, sending GoInGameRequest
   'ServerWorld' setting NetworkId 1 in game
   ```
3. **Window → Entities → Hierarchy** → world dropdown. Switch between `ServerWorld` and `ClientWorld`. Zombies spawned by the server now also appear in `ClientWorld`.
4. More players: Play Mode Tools → **Num Thin Clients**. A thin client is a fake client (no rendering, no gameplay) for testing extra connections. Each one logs its own NetworkId.

## The big idea

- **Server = the truth.** It runs the gameplay and decides everything: spawning, damage, death.
- **Client = what the player sees.** It receives the server's state and sends the player's input.
- In the editor both run in the same Unity (two worlds), but they still talk through a real network connection on `localhost:7979`.

## What happens when you press Play

```
 ServerWorld                                     ClientWorld
 ───────────                                     ───────────
 GameBootstrap: listen on 7979                   GameBootstrap: connect to 7979
        ◄─────────────────── connect ──────────────────┘
 connection entity created                       connection entity gets NetworkId
                                                 GoInGameClientSystem:
                                                   + NetworkStreamInGame (own side)
        ◄─────────── GoInGameRequest (RPC) ──────── send
 GoInGameServerSystem:
   + NetworkStreamInGame (server side)
   spawn main character, GhostOwner = NetworkId
        ──────────── snapshots, every tick ───────────► ghosts appear / update
                                                 (your character: predicted,
                                                  GhostOwnerIsLocal enabled)
        ◄─────────── input (CharacterInput), every tick ─ CharacterInputSystem
```

Connected is not enough. **The server only sends ghosts to connections that have `NetworkStreamInGame`.** That's why "go in game" exists.

## Words you'll see

| Word | Meaning |
|---|---|
| **ServerWorld / ClientWorld** | Two separate ECS worlds. Each has its own entities and runs its own systems. |
| **Thin client** | Extra test client with no rendering or gameplay. |
| **Connection entity** | One entity per connection. The client has 1 (to the server), the server has 1 per client. |
| `NetworkId` | Component on the connection entity. The player number the server gave this connection (1, 2, 3...). Added when the connection is accepted. |
| `NetworkStreamInGame` | Component on the connection entity. "Send/receive the game on this connection." |
| **Ghost** | An entity the server syncs to all clients. Made from a prefab with **Ghost Authoring**. |
| **Snapshot** | The server's packet of ghost data, sent every tick. |
| **Tick** | One fixed server step (default 60 per second). |
| **RPC** | A one-time message (client → server or server → client). |
| **Interpolated ghost** | Client shows it slightly in the past, smoothly. For things you don't control (zombies). |
| **Predicted ghost** | Client simulates it ahead, then corrects from the server. For things you control (your player). |
| **Pre-spawned ghost** | A ghost prefab instance placed in a subscene (your soldiers and the 2 zombies there). Both worlds load it, the server takes over. |

## Syntax cheat sheet

### 1. Pick which world a system runs in

```csharp
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]   // server only
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]   // client only
// no attribute = BOTH client and server
```

Check inside a system: `state.WorldUnmanaged.IsServer()` / `state.WorldUnmanaged.IsClient()`.

Rule: **anything that decides** (spawn, damage, destroy) = server only. **Anything that only shows** (visuals, UI) = client only.

### 2. RPC: send a one-time message

Define it (fields are the message data):
```csharp
public struct BuildRequest : IRpcCommand
{
    public int buildingType;
}
```

Send it: a new entity with the RPC + `SendRpcCommandRequest`. Netcode sends it and destroys the entity.
```csharp
Entity rpcEntity = entityCommandBuffer.CreateEntity();
entityCommandBuffer.AddComponent(rpcEntity, new BuildRequest { buildingType = 2 });
entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest());
```

| `SendRpcCommandRequest` | From client | From server |
|---|---|---|
| `new SendRpcCommandRequest()` | to the server | to **all** clients |
| `new SendRpcCommandRequest { TargetConnection = connectionEntity }` | to the server | to **that one** client |

Receive it: the RPC arrives as an entity with the RPC + `ReceiveRpcCommandRequest`. **You must destroy it yourself**, or it's handled again every frame.
```csharp
foreach ((RefRO<BuildRequest> request, RefRO<ReceiveRpcCommandRequest> receive, Entity rpcEntity)
    in SystemAPI.Query<RefRO<BuildRequest>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
{
    int type = request.ValueRO.buildingType;
    Entity sender = receive.ValueRO.SourceConnection;   // who sent it
    entityCommandBuffer.DestroyEntity(rpcEntity);
}
```

Use RPCs for **rare events** (join, chat, "build this"). Not for movement every frame. That's what ghosts and input components are for.

### 3. Ghost: an entity synced to everyone

1. Add **Ghost Authoring** to a **prefab** (top-level GameObject). On a plain scene object it errors: *"not a valid prefab"*.
2. **Spawn it on the server only.** Server `Instantiate`s → every client gets a copy automatically.
   Spawning a ghost prefab on the client is an advanced feature ("predicted spawning"). Done by accident, it gives *"Failed to initialize predicted spawned ghost"*.
3. `LocalTransform` is synced by default. Your own fields are synced only with `[GhostField]`:
   ```csharp
   public struct Ammo : IComponentData
   {
       [GhostField] public int count;   // server value is sent to clients
       public int max;                   // not sent, client keeps its baked value
   }
   ```
   On the client, synced values are overwritten by every snapshot. Writing them on the client does nothing useful.

Ghost Authoring settings that matter first:

| Setting | Use |
|---|---|
| Default Ghost Mode: **Interpolated** | NPCs, zombies, things nobody controls |
| Default Ghost Mode: **Owner Predicted** + **Has Owner** | Player characters: predicted for the owner, interpolated for everyone else |

### 4. Players

How the units work (spawning, input, control switching): `Assets/Scripts/README.md`.

| Name | What | Used in |
|---|---|---|
| `GhostOwner` | Component on a ghost: `NetworkId` of the connection that owns it | Set when spawning (`GoInGameServerSystem`) |
| `GhostOwnerIsLocal` | Enabled only on the client that owns the ghost. Query with it to find "my" units. Not for use inside prediction. | `CharacterInputSystem`, `CharacterVisual`, `UnitSwitchMenu` |
| `IInputComponentData` | Input component. Client writes it, netcode sends it to the server every tick. | `CharacterInput` |
| `InputEvent` | One-shot button inside an input component. `Set()` on the client, `IsSet` true for exactly one tick. | jump, dash |
| `PredictedSimulationSystemGroup` | Where movement goes for predicted ghosts. Runs on the server, and on clients for the ghosts they predict. May replay old ticks. | `CharacterMoverSystem` (in its fixed-step subgroup) |
| `Simulate` | Enabled on the entities to simulate in the current prediction tick. Add `WithAll<Simulate>` to prediction queries. | `CharacterMoverJob` |
| `[GhostEnabledBit]` | Syncs an enableable component's on/off state from the server. | `ControlledByPlayer` |
| `LinkedEntityGroup` on the connection | Entities added to it are destroyed when that player disconnects. | Characters, summons |

## What's networked right now

| Thing | Status |
|---|---|
| Connect + go in game | ✅ this folder |
| Zombies | ✅ ghosts (`BaseUnit` has Ghost Authoring), spawned server-only (`ZombieSpawnerSystem` has the `ServerSimulation` filter) |
| Soldiers + 2 zombies in the subscene | ✅ pre-spawned ghosts |
| Player characters (`PlayerECS` prefab) | ✅ owner-predicted ghosts. `GoInGameServerSystem` spawns one per player, owned by them. Input goes to the server as `CharacterInput`, movement is predicted (`CharacterMoverSystem`) |
| Summons / other units | ✅ same setup (any prefab with `ControllableAuthoring`). The server decides which unit each player controls (`ControlSwitchSystem`). Test with F1/F2 (`DebugSummonSystems`) |
| Visuals (Y Bot) | ✅ one per unit, spawned in **ClientWorld** by `CharacterVisualSystem`. The camera follows the unit you control |
| Course systems (move, shoot, target, health bar) | ❌ no filter, so they run in both worlds. The client runs its own copy of the gameplay. Snapshots overwrite ghost positions, but e.g. bullets are spawned separately on both sides. `Health` has no `[GhostField]`, so client health comes from the client's own copy of the fight, not from the server. |
| `ZombieSpawner.prefab` | Has Ghost Authoring but doesn't need it (only the server reads spawners) and isn't used by the scene. Remove it. |

## Next steps, in order

1. ✅ **Player as a ghost.** `GoInGameServerSystem` spawns `PlayerECS` (Owner Predicted, Has Owner) per connection and sets `GhostOwner`.
2. ✅ **Networked input.** `CharacterInput` is an `IInputComponentData`. `CharacterMoverSystem` runs in the prediction loop.
3. ✅ **Visuals follow your own units.** `CharacterVisualSystem` spawns visuals in `ClientWorld`. `GhostOwnerIsLocal` + `ControlledByPlayer` pick the one the camera follows.
4. **Server-only gameplay.** Your own copies of the course systems with `ServerSimulation`, plus `[GhostField]` on data clients must show (health).
5. **Character select.** Send the chosen index in `GoInGameRequest`, and spawn `characterPrefabs[index]` instead of `[0]`.

Official tutorial doing steps 1–2 with a cube: **"Networked Cube"**, in the package docs at `Library/PackageCache/com.unity.netcode@*/Documentation~/networked-cube.md`.

## Warnings you'll still see

| Warning | Meaning |
|---|---|
| `Server Tick Batching has occurred...` | The editor can't run 60 server ticks/s with two worlds, so it runs 2 ticks in one frame to catch up. Editor noise unless constant. Keep Burst on. |
| `[ClientWorld] The default physics world contains N dynamic physics objects which are not ghosts` | Something with a Rigidbody isn't a ghost. Every unit prefab needs Ghost Authoring, and the old `PlayerECS` placed in My Sub Scene must be deleted (players are spawned now). |
| `Failed to initialize predicted spawned ghost` | The client `Instantiate`d a ghost prefab. Spawn ghosts on the server only. |
