using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthDeadTestSystem : ISystem {


    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRO<Health> health,
            Entity entity) 
            in SystemAPI.Query<
                RefRO<Health>>().WithEntityAccess()) {

            if (health.ValueRO.healthAmount <= 0) {
                // This entity is dead
                entityCommandBuffer.DestroyEntity(entity);
            }
        }
    }


}