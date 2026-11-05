using Unity.PolySpatial.Internals;
using UnityEngine;
using static Unity.PolySpatial.Internals.CommandHandlerGraph;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Registers this package's command handler creation callbacks with <see cref="CommandHandlerGraph"/>.
    /// </summary>
    static class XRCommandHandlerRegistration
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void RegisterInEditor() => Register();
#endif

        // SubsystemRegistration runs before BeforeSplashScreen, where PolySpatialCore initializes in players.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            RegisterCreationCallback(Stage.NetworkAppHost, XRLocalCommandHandler.Create);
            RegisterCreationCallback(Stage.HostConnectionManager, XRHostCommandHandler.Create);
#if HAS_XR_INTERACTION_TOOLKIT
            RegisterCreationCallback(Stage.PostLocalBackend, XRPostUnityBackendCommandHandler.Create);
#endif
        }
    }
}
