using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Internals.Subsystems;
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

        List<IPolySpatialHostCommandHandler> m_HostCommandHandlers = new ();
        XRLocalCommandHandler m_XRLocalCommandHandler;
        PolySpatialNetworkAppHostBase m_AppHostBase;

        internal ARData m_ARSessionData;

        public PolySpatialXRCore() : base(k_SubsystemId)
        {
        }

        /// <summary>
        /// Initializes the host handlers depending on the current networking mode
        /// </summary>
        internal override void Initialize()
        {
            if (PolySpatialCore.CurrentNetworkingMode == PolySpatialSettings.NetworkingMode.LocalAndClient)
                AddHostCommandHandlers();

            // PolySpatialNetworkSingleAppHost does not exist when we initialize.  We can't register a IPolySpatialCommandHandler until it exists.
            PolySpatialNetworkAppHostBase.OnEnableCommandHandlers += AddLocalCommandHandler;
        }

        // This will only run in the P2D app and not in the editor.
        void AddLocalCommandHandler(PolySpatialNetworkAppHostBase appHost, IPolySpatialLocalBackend backend)
        {
            m_AppHostBase = appHost;

            m_ARSessionData = new ARData();

            m_ARSessionData.m_ARPlaneTracker = new PolySpatialARPlaneTracker();
            m_ARSessionData.m_XRHandTracker = new PolySpatialXRHandTracker();
            m_ARSessionData.m_XRMeshTracker = new PolySpatialXRMeshTracker();
            m_ARSessionData.m_XRImageTracker = new PolySpatialARImageTracker();

            m_XRLocalCommandHandler = new();
            m_XRLocalCommandHandler.NextHandler = m_AppHostBase.NextHandler;
            m_AppHostBase.NextHandler = m_XRLocalCommandHandler;

            m_AppHostBase.ConnectionError += OnAppHostConnectionError;
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
        /// Add the handlers that will handle commands coming from a host over the network
        /// </summary>
        void AddHostCommandHandlers()
        {
            var commandHandler = new XRHostCommandHandler();
            commandHandler.Initialize();
            m_HostCommandHandlers.Add(commandHandler);
        }

        /// <summary>
        /// Cleanup all the handlers
        /// </summary>
        public override void Dispose()
        {
            foreach (var handler in m_HostCommandHandlers)
                (handler as IDisposable)?.Dispose();

            m_HostCommandHandlers.Clear();

            if (m_ARSessionData != null)
            {
                m_ARSessionData.Dispose();
                m_ARSessionData = null;
            }

            PolySpatialNetworkAppHostBase.OnEnableCommandHandlers -= AddLocalCommandHandler;

            if (m_AppHostBase != null)
            {
                m_AppHostBase.ConnectionError -= OnAppHostConnectionError;
                m_AppHostBase = null;
            }
        }

        internal override void Update()
        {
            m_ARSessionData?.Update();
        }
    }
}

