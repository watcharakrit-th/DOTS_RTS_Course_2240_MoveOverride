using Unity.Entities;

// TEMPORARY. Delete this file when EntitiesSubscene is turned back on.
// These course systems call GetSingleton<EntitiesReferences>(), which lives in the subscene.
// With the subscene off, that singleton is missing, so they log an error every frame.
// Runs first in the frame (before those systems), turns them off, then turns itself off.
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
partial struct TempDisableCourseSystemsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        Disable<ShootAttackSystem>(ref state);
        Disable<ShootLightSpawnerSystem>(ref state);
        Disable<ZombieSpawnerSystem>(ref state);

        // Only needs to run once
        state.Enabled = false;
    }

    private static void Disable<T>(ref SystemState state) where T : unmanaged, ISystem
    {
        SystemHandle handle = state.WorldUnmanaged.GetExistingUnmanagedSystem<T>();
        // Null when the system isn't in this world (e.g. ZombieSpawnerSystem is server-only)
        if (handle == SystemHandle.Null)
        {
            return;
        }
        state.WorldUnmanaged.ResolveSystemStateRef(handle).Enabled = false;
    }
}
