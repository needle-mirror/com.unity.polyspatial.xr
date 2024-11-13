using System;
using Unity.PolySpatial.Internals;
using UnityEngine;
using FlatSharp.Runtime.Extensions;

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

        PolySpatialXRHeadTracker XRHeadTracker => Core.m_ARSessionData.m_XRHeadTracker;

        PolySpatialXRMeshTracker XRMeshTracker => Core.m_ARSessionData.m_XRMeshTracker;

        PolySpatialARImageTracker ARImageTracker => Core.m_ARSessionData.m_XRImageTracker;

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
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out Span<byte> _);
                    // Send the current state of the ARPlane tracking. ARPlane tracking state might be active when a Client connects.
                    ARPlaneTracker.InitializeARPlanes(id->hostId);

                    // Send the current state of the hand tracking. Hand tracking state might be active when a Client connects.
                    XRHandTracker.InitHandTracking(id->hostId);

                    ARImageTracker.InitializeARImageTracker(id->hostId);

                    XRMeshTracker.InitializeXRMeshes(id->hostId);

                    PolySpatialXRHeadTracker.StartConnection(id->hostId);

                    break;
                }
                case PolySpatialCommand.EndConnection:
                {
                    ARPlaneTracker.EndConnection();

                    XRHandTracker.EndConnection();

                    ARImageTracker.EndConnection();
                    
                    XRMeshTracker.EndConnection();

                    PolySpatialXRHeadTracker.EndConnection();

                    break;
                }
                case PolySpatialCommand.CreateOrUpdateReferenceImageLibrary:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var referenceImageLibrary = PolySpatialReferenceImageLibrary.Serializer.Parse(data.Length, p);
                        ARImageTracker.CreateOrUpdateReferenceImageLibrary(referenceImageLibrary);
                    }

                    break;
                }
                case PolySpatialCommand.AddTrackedImage:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var image = PolySpatialXRReferenceImage.Serializer.Parse(data.Length, p);
                        ARImageTracker.AddTrackedImage(image);
                    }
                    break;
                }
            }

            NextHandler.HandleCommand(cmd, argCount, argValues, argSizes);
        }
    }
}
