using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// RPC = a one-time message sent over the network. It's a component on its own entity.
// This one carries no data: receiving it IS the message ("I'm ready, start sending me the game").
// Sent client -> server by GoInGameClientSystem, handled by GoInGameServerSystem.
public struct GoInGameRequest : IRpcCommand
{
}

// Client side of "going in game".
// Connected is not enough: the server only sends ghosts (synced entities) to connections marked NetworkStreamInGame.
// As soon as the server accepts us (our connection entity gets a NetworkId), this system:
// 1. marks our own connection in game, so we accept ghost data
// 2. sends GoInGameRequest, so the server marks its side too
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Only run while a connection is accepted but not in game yet. So in practice: once, right after connecting.
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<NetworkStreamInGame>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // "ClientWorld", for the log
        FixedString128Bytes worldName = state.WorldUnmanaged.Name;

        // Adding components / creating entities can't happen while looping over a query,
        // so they're recorded here and done after the loop (Playback)
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        // On a client there is only one connection entity: the one to the server
        foreach ((RefRO<NetworkId> networkId, Entity connectionEntity)
            in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            entityCommandBuffer.AddComponent<NetworkStreamInGame>(connectionEntity);

            // Sending an RPC: new entity + the RPC component + SendRpcCommandRequest.
            // Netcode sends it and destroys the entity for us.
            Entity rpcEntity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<GoInGameRequest>(rpcEntity);
            entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connectionEntity });

            UnityEngine.Debug.Log($"'{worldName}' connected as NetworkId {networkId.ValueRO.Value}, sending GoInGameRequest");
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
