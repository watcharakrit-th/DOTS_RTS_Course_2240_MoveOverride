// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.NetCode;
// using Unity.Transforms;

// // Server side of "going in game".
// // When a GoInGameRequest RPC arrives, mark that client's connection NetworkStreamInGame.
// // From then on the server sends that client snapshots, which create/update the ghosts in its ClientWorld.
// // It also spawns the player's main character: a ghost owned by them, so their client predicts it and controls it.
// [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// partial struct GoInGameServerSystem : ISystem
// {
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         // The spawner lives in My Sub Scene, so this also waits until the subscene has loaded
//         state.RequireForUpdate<CharacterSpawner>();
//         // Only run on frames where a GoInGameRequest has arrived
//         state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<GoInGameRequest, ReceiveRpcCommandRequest>().Build());
//     }

//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         // "ServerWorld", for the log
//         FixedString128Bytes worldName = state.WorldUnmanaged.Name;
//         CharacterSpawner characterSpawner = SystemAPI.GetSingleton<CharacterSpawner>();
//         DynamicBuffer<CharacterPrefabElement> characterPrefabs = SystemAPI.GetSingletonBuffer<CharacterPrefabElement>(true);
//         // Everyone plays the first character until there's a character select
//         Entity characterPrefab = Entity.Null;
//         if (characterPrefabs.Length > 0)
//         {
//             characterPrefab = characterPrefabs[0].prefab;
//         }
//         else
//         {
//             UnityEngine.Debug.LogError("CharacterSpawner has no Character Prefabs, so players join without a character.");
//         }

//         EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

//         // A received RPC is an entity with the RPC component + ReceiveRpcCommandRequest.
//         // SourceConnection is the connection entity of the client that sent it.
//         foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpc, Entity rpcEntity)
//             in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>().WithEntityAccess())
//         {
//             Entity connectionEntity = receiveRpc.ValueRO.SourceConnection;
//             entityCommandBuffer.AddComponent<NetworkStreamInGame>(connectionEntity);

//             NetworkId networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity);
//             UnityEngine.Debug.Log($"'{worldName}' setting NetworkId {networkId.Value} in game, spawning their character");

//             if (characterPrefab != Entity.Null)
//             {
//                 SpawnMainCharacter(ref state, entityCommandBuffer, characterPrefab, characterSpawner, connectionEntity, networkId.Value);
//             }

//             // Received RPCs are NOT destroyed automatically. Without this, the request would be handled again every frame.
//             entityCommandBuffer.DestroyEntity(rpcEntity);
//         }

//         entityCommandBuffer.Playback(state.EntityManager);
//     }

//     private void SpawnMainCharacter(ref SystemState state, EntityCommandBuffer entityCommandBuffer, Entity characterPrefab,
//         CharacterSpawner characterSpawner, Entity connectionEntity, int networkId)
//     {
//         // Keep the prefab's rotation and scale, only move it. Players stand side by side along X.
//         LocalTransform spawnTransform = SystemAPI.GetComponent<LocalTransform>(characterPrefab);
//         spawnTransform.Position = characterSpawner.spawnPosition + new float3(networkId * characterSpawner.spawnSpacing, 0f, 0f);

//         // A ghost: spawned here on the server, every client gets a copy automatically
//         Entity character = entityCommandBuffer.Instantiate(characterPrefab);
//         entityCommandBuffer.SetComponent(character, spawnTransform);
//         // Owner = this player: their client predicts it and sends input for it
//         entityCommandBuffer.SetComponent(character, new GhostOwner { NetworkId = networkId });
//         // Destroyed automatically when this player disconnects
//         entityCommandBuffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = character });
//         // Control falls back to this unit whenever the player controls nothing (ControlSwitchSystem).
//         // That's also how it gets control right after spawning.
//         // "character" is a placeholder until Playback. The ECB swaps in the real entity inside this component too.
//         entityCommandBuffer.AddComponent(connectionEntity, new PlayerMainCharacter { value = character });
//     }
// }
