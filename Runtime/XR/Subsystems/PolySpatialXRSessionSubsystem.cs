using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    /// <summary>
    /// The Subsystem which is to be instantiated by PolySpatialXRLoader which controls the lifecycle of an XR Session.
    /// Here we can implement custom functionality to be able to turn the session on and off to enter and exit
    /// XR mode(s) of operation if needed.
    /// </summary>
    public sealed class PolySpatialXRSessionSubsystem : XRSessionSubsystem
    {
        internal const string k_SubsystemId = "XRPolySpatial-Session";

        class PolySpatialXRSessionProvider : Provider
        {

            public override TrackingState trackingState => TrackingState.Tracking;

            public override Promise<SessionAvailability> GetAvailabilityAsync() =>
                Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Installed | SessionAvailability.Supported);

            public override void Start()
            {
            }

            public override void Stop()
            {
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
            }
        }

        protected override void OnCreate()
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = k_SubsystemId,
                providerType = typeof(PolySpatialXRSessionProvider),
                subsystemTypeOverride = typeof(PolySpatialXRSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false,
            });
        }
    }
}
