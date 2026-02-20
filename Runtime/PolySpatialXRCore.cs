using System;
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Internals.Subsystems;
using Unity.PolySpatial.XR.Internals.Subsystems;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// The PolySpatialXRCore subsystem is responsible for initializing the PolySpatialXRCore
    /// Add all the handlers for the commands that are supported by the PolySpatialXRCore, and
    /// add all the trackers necessary for tracking changes from XR components and events.
    /// </summary>
    [Preserve]
    class PolySpatialXRCore : PolySpatialSubsystemBase
    {
        internal const string k_SubsystemId = "PolySpatialXRCore_Subsystem";

        internal class ARData : IDisposable
        {
            // The XR display tracker is optional; we don't create it if running in batch mode, since in that case,
            // there is by definition no display to track.
            internal PolySpatialXRDisplayTracker m_XRDisplayTracker;

            internal PolySpatialARPlaneTracker m_ARPlaneTracker;
            internal PolySpatialXRHandTracker m_XRHandTracker;
            internal PolySpatialXRHeadTracker m_XRHeadTracker;
            internal PolySpatialXRMeshTracker m_XRMeshTracker;
            internal PolySpatialARImageTracker m_XRImageTracker;
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
            // This is a reference to the shared PolySpatialXrInputTracker instance
            // that the class holds.
            internal PolySpatialXrInputTracker m_XRInputTracker;
#endif
            internal void Update()
            {
                // No need to call Update for m_ARPlaneTracker, it gets all its updates from a callback
                // only when the set of ARPlanes changes.
                m_XRDisplayTracker?.Update();
                m_XRHandTracker.Update();
                m_XRImageTracker.Update();
                // Input tracker is a shared singleton that is updated at the same time
                // as the AR data instance. No need to call it here.
            }

            public void Dispose()
            {
                m_XRDisplayTracker = null;

                m_ARPlaneTracker.Dispose();
                m_ARPlaneTracker = null;

                m_XRHandTracker.Dispose();
                m_XRHandTracker = null;

                m_XRMeshTracker.Dispose();
                m_XRMeshTracker = null;

                m_XRImageTracker.Dispose();
                m_XRImageTracker = null;
            }
        }

        NetworkAppHost m_NetworkAppHost;
        internal ARData m_ARSessionData;

        public PolySpatialXRCore() : base(k_SubsystemId)
        {
        }

        internal void InitializeARData()
        {
            m_NetworkAppHost = PolySpatialCore.CommandHandlerGraph.NetworkAppHost;

            m_ARSessionData = new ARData
            {
                m_XRDisplayTracker = Application.isBatchMode ? null : new(),
                m_ARPlaneTracker = new PolySpatialARPlaneTracker(),
                m_XRHandTracker = new PolySpatialXRHandTracker(),
                m_XRMeshTracker = new PolySpatialXRMeshTracker(),
                m_XRImageTracker = new PolySpatialARImageTracker(),
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
                m_XRInputTracker = PolySpatialXrInputTracker.Instance,
#endif
            };

            m_NetworkAppHost.ConnectionError += OnAppHostConnectionError;
        }

        // This should stay in sync with the PolySpatialCommand.EndConnection handler in XRLocalCommandHandler.cs
        private void OnAppHostConnectionError(ErrorCode errorcode, string errormessage)
        {
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
            PolySpatialXrInputTracker.Instance?.EndSession();
#endif
            if (m_ARSessionData == null)
                return;

            m_ARSessionData.m_ARPlaneTracker.EndSession();
            m_ARSessionData.m_XRHandTracker.EndSession();
            m_ARSessionData.m_XRImageTracker.EndSession();
            m_ARSessionData.m_XRMeshTracker.EndSession();
            PolySpatialXRHeadTracker.EndSession();
        }

        /// <summary>
        /// Cleanup all the handlers
        /// </summary>
        public override void Dispose()
        {
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
            PolySpatialXrInputTracker.Instance?.Dispose();
#endif
            if (m_ARSessionData != null)
            {
                m_ARSessionData.Dispose();
                m_ARSessionData = null;
            }

            if (m_NetworkAppHost != null)
            {
                m_NetworkAppHost.ConnectionError -= OnAppHostConnectionError;
                m_NetworkAppHost = null;
            }

            PolySpatialXRMeshSubsystemProcessor.instance?.Dispose();
        }

        internal override void Update()
        {
            m_ARSessionData?.Update();
        }
    }
}
