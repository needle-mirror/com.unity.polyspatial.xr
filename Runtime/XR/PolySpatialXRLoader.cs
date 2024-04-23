using System.Collections.Generic;
using Unity.PolySpatial.XR.Internals.Subsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

#if INCLUDE_UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
#endif

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Initialize all the implemented subclasses supported by the PolySpatial XR Plug-In.
    /// </summary>
    public class PolySpatialXRLoader : XRLoaderHelper
    {
        static List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new ();
        static List<XRPlaneSubsystemDescriptor> s_PlaneSubsystemDescriptors = new ();

#if INCLUDE_UNITY_XR_HANDS
        static List<XRHandSubsystemDescriptor> s_HandSubsystemDescriptors = new ();
        XRHandProviderUtility.SubsystemUpdater m_HandSubsystemUpdater;
#endif

        /// <summary>
        /// Initializes the loader.
        /// </summary>
        /// <returns>`True` if the session subsystem was successfully created, otherwise `false`.</returns>
        public override bool Initialize()
        {
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, PolySpatialXRSessionSubsystem.k_SubsystemId);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneSubsystemDescriptors, PolySpatialXRPlaneSubsystem.k_SubsystemId);

            Logging.Log(LogCategory.XR, "Initialize PolySpatialXRLoader");

            var sessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();
            if (sessionSubsystem == null)
                Logging.LogError(LogCategory.XR, "Failed to load session subsystem.");

#if INCLUDE_UNITY_XR_HANDS
            CreateSubsystem<XRHandSubsystemDescriptor, XRHandSubsystem>(s_HandSubsystemDescriptors, PolySpatialHandSubsystem.k_SubsystemId);

            var handSubsystem = GetLoadedSubsystem<XRHandSubsystem>();
            if (handSubsystem != null)
            {
                m_HandSubsystemUpdater = new XRHandProviderUtility.SubsystemUpdater(handSubsystem);
                handSubsystem.Start();
                m_HandSubsystemUpdater.Start();
            }
#endif

            return sessionSubsystem != null;
        }

        /// <summary>
        /// Destroys each subsystem.
        /// </summary>
        /// <returns>Always returns `true`.</returns>
        public override bool Deinitialize()
        {
            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

#if INCLUDE_UNITY_XR_HANDS
            m_HandSubsystemUpdater?.Stop();
            DestroySubsystem<XRHandSubsystem>();
#endif

            return true;
        }
    }
}
