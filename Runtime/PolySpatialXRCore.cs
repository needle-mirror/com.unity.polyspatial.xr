using System;
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Internals.Subsystems;
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
            internal PolySpatialARPlaneTracker m_ARPlaneTracker;
            internal PolySpatialXRHandTracker m_XRHandTracker;
            internal PolySpatialXRHeadTracker m_XRHeadTracker;
            internal PolySpatialXRMeshTracker m_XRMeshTracker;
            internal PolySpatialARImageTracker m_XRImageTracker;

            internal void Update()
            {
                // No need to call Update for m_ARPlaneTracker, it gets all its updates from a callback
                // only when the set of ARPlanes changes.
                m_XRHandTracker.Update();
                m_XRImageTracker.Update();
            }

            public void Dispose()
            {
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

        PolySpatialNetworkAppHost m_NetworkAppHost;

        internal ARData m_ARSessionData;

        public PolySpatialXRCore() : base(k_SubsystemId)
        {
        }

        internal void InitializeARData()
        {
            m_NetworkAppHost = PolySpatialCore.CommandHandlerGraph.NetworkAppHost;

            m_ARSessionData = new ARData
            {
                m_ARPlaneTracker = new PolySpatialARPlaneTracker(),
                m_XRHandTracker = new PolySpatialXRHandTracker(),
                m_XRMeshTracker = new PolySpatialXRMeshTracker(),
                m_XRImageTracker = new PolySpatialARImageTracker()
            };

            m_NetworkAppHost.ConnectionError += OnAppHostConnectionError;
        }

        // This should stay in sync with the PolySpatialCommand.EndConnection handler in XRLocalCommandHandler.cs
        private void OnAppHostConnectionError(ErrorCode errorcode, string errormessage)
        {
            if (m_ARSessionData == null)
                return;

            m_ARSessionData.m_ARPlaneTracker.EndConnection();
            m_ARSessionData.m_XRHandTracker.EndConnection();
            m_ARSessionData.m_XRImageTracker.EndConnection();
            m_ARSessionData.m_XRMeshTracker.EndConnection();
            PolySpatialXRHeadTracker.EndConnection();
        }

        /// <summary>
        /// Cleanup all the handlers
        /// </summary>
        public override void Dispose()
        {
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
        }

        internal override void Update()
        {
            m_ARSessionData?.Update();
        }
    }
}
