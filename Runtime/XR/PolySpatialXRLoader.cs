using System.Collections.Generic;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.XR.Internals.Subsystems;
using UnityEngine;
using UnityEngine.XR;
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
        static List<XRDisplaySubsystemDescriptor> s_DisplaySubsystemDescriptors = new();
        static List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new ();
        static List<XRPlaneSubsystemDescriptor> s_PlaneSubsystemDescriptors = new ();
        static List<XRImageTrackingSubsystemDescriptor> s_ImageTrackingSubsystemDescriptors = new ();

#if INCLUDE_UNITY_XR_HANDS
        static List<XRHandSubsystemDescriptor> s_HandSubsystemDescriptors = new ();
        XRHandProviderUtility.SubsystemUpdater m_HandSubsystemUpdater;
#endif

        static List<XRMeshSubsystemDescriptor> s_MeshSubsystemDescriptors = new();

        PolySpatialXRHMDEventListener m_HMDEventListener;

        /// <summary>
        /// Initializes the loader.
        /// </summary>
        /// <returns>`True` if the session subsystem was successfully created, otherwise `false`.</returns>
        public override bool Initialize()
        {
            CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(s_DisplaySubsystemDescriptors, PolySpatialXRDisplaySubsystemProcessor.k_SubsystemId);
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, PolySpatialXRSessionSubsystem.k_SubsystemId);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneSubsystemDescriptors, PolySpatialXRPlaneSubsystem.k_SubsystemId);
            CreateSubsystem<XRMeshSubsystemDescriptor, XRMeshSubsystem>(s_MeshSubsystemDescriptors, PolySpatialXRMeshSubsystemProcessor.k_SubsystemId);
            CreateSubsystem<XRImageTrackingSubsystemDescriptor, XRImageTrackingSubsystem>(s_ImageTrackingSubsystemDescriptors,
                PolySpatialXRImageTrackingSubsystem.k_SubsystemId);

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

            m_HMDEventListener = new PolySpatialXRHMDEventListener();
            XRInputProvider.EnsureInitialized();

            Logging.Log(LogCategory.XR, "Initialize PolySpatialXRLoader");

            return sessionSubsystem != null;
        }

        /// <summary>
        /// Destroys each subsystem.
        /// </summary>
        /// <returns>Always returns `true`.</returns>
        public override bool Deinitialize()
        {
#if INCLUDE_UNITY_XR_HANDS
            m_HandSubsystemUpdater?.Stop();
            DestroySubsystem<XRHandSubsystem>();
#endif

            DestroySubsystem<XRImageTrackingSubsystem>();
            DestroySubsystem<XRMeshSubsystem>();
            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();
            DestroySubsystem<XRDisplaySubsystem>();
            m_HMDEventListener?.Deinitialize();
            m_HMDEventListener = null;

            return true;
        }
    }
}
