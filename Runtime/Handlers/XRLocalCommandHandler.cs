using System;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Command handler for initializing AR/XR data when a client connects to _this_ host.
    /// ARPlane data, Hand data, etc. can be generated on the platform, we need to send this
    /// to the client when they connect to the host.
    /// </summary>
    class XRLocalCommandHandler : IPolySpatialCommandHandler
    {
        public IPolySpatialCommandHandler NextHandler { get; set; }

        PolySpatialARPlaneTracker ARPlaneTracker => Core.m_ARSessionData.m_ARPlaneTracker;

        PolySpatialXRHandTracker XRHandTracker => Core.m_ARSessionData.m_XRHandTracker;

        [NonSerialized]
        PolySpatialXRCore m_Core;

        PolySpatialXRCore Core
        {
            get
            {
                if (m_Core == null)
                {
                    m_Core = (PolySpatialXRCore)PolySpatialCore.Instance.GetSubsystemById(PolySpatialXRCore.k_SubsystemId);
                    Debug.Assert(m_Core != null);
                }

                return m_Core;
            }
        }

        public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
        {
            switch (cmd)
            {
                case PolySpatialCommand.BeginConnection:
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out Span<byte> _);
                    if (ARPlaneTracker != null)
                    {
                        // Send the current state of the ARPlane tracking. ARPlane tracking state might be active when a Client connects.
                        ARPlaneTracker.InitializeARPlanes(id->hostId);
                    }
                    if (XRHandTracker != null) {
                        // Send the current state of the hand tracking. Hand tracking state might be active when a Client connects.
                        XRHandTracker.InitHandTracking(id->hostId);
                    }

                    break;
                case PolySpatialCommand.EndConnection:
                    if (ARPlaneTracker != null)
                    {
                        ARPlaneTracker.EndConnection();
                    }
                    if (XRHandTracker != null)
                    {
                        XRHandTracker.EndConnection();
                    }
                    break;
            }

            NextHandler.HandleCommand(cmd, argCount, argValues, argSizes);
        }
    }
}
