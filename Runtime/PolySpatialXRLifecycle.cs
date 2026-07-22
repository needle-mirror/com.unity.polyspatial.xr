using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Registers static state resetters with <see cref="PolySpatialCore.OnTerminate"/> so that state does not persist
    /// after the PolyspatialRuntime has shut down. Prevents stale state when domain reload is disabled or using
    /// PolySpatial in edit mode.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class PolySpatialXRLifecycle
    {
#if UNITY_EDITOR
        static PolySpatialXRLifecycle()
        {
            Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            PolySpatialCore.OnTerminate -= ResetStatics;
            PolySpatialCore.OnTerminate += ResetStatics;
        }

        static void ResetStatics()
        {
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
            PolySpatialXrInputTracker.Reset();
#endif
            XRInputProvider.Reset();
        }
    }
}
