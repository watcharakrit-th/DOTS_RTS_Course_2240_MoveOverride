// using Unity.NetCode;

// // Runs once when the game starts, before any system, and decides which worlds get created.
// // Netcode's default bootstrap already creates a ServerWorld and a ClientWorld, but never connects them.
// // This one does the same, plus turns on auto-connect: the server listens on the port, the client connects to it.
// // Only ONE class may inherit ClientServerBootstrap in the whole project.
// // [Preserve] stops builds from deleting this class, because Unity finds it by reflection (nothing calls it directly).
// [UnityEngine.Scripting.Preserve]
// public class GameBootstrap : ClientServerBootstrap
// {
//     public override bool Initialize(string defaultWorldName)
//     {
//         // Any free port works. 7979 is what Unity's samples use.
//         AutoConnectPort = 7979;

//         // Create the worlds the normal way. Which ones depends on Window > Multiplayer > Play Mode Tools
//         // ("Client & Server" = both in the editor).
//         return base.Initialize(defaultWorldName);
//     }
// }
