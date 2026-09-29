using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// Server side of "going in game".
// When a GoInGameRequest RPC arrives, mark that client's connection NetworkStreamInGame.
// From then on the server sends that client snapshots, which create/update the ghosts in its ClientWorld.
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Only run on frames where a GoInGameRequest has arrived
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<GoInGameRequest, ReceiveRpcCommandRequest>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // "ServerWorld", for the log
        FixedString128Bytes worldName = state.WorldUnmanaged.Name;
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        // A received RPC is an entity with the RPC component + ReceiveRpcCommandRequest.
        // SourceConnection is the connection entity of the client that sent it.
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpc, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>().WithEntityAccess())
        {
            Entity connectionEntity = receiveRpc.ValueRO.SourceConnection;
            entityCommandBuffer.AddComponent<NetworkStreamInGame>(connectionEntity);

            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity);
            UnityEngine.Debug.Log($"'{worldName}' setting NetworkId {networkId.Value} in game");

            // Received RPCs are NOT destroyed automatically. Without this, the request would be handled again every frame.
            entityCommandBuffer.DestroyEntity(rpcEntity);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
