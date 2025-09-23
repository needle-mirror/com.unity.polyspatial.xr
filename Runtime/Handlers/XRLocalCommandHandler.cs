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
    class XRLocalCommandHandler : IPolySpatialChainableCommandHandler
    {
        public IPolySpatialCommandHandler NextHandler { get; set; }

        PolySpatialXRDisplayTracker XRDisplayTracker => Core.m_ARSessionData.m_XRDisplayTracker;

        PolySpatialARPlaneTracker ARPlaneTracker => Core.m_ARSessionData.m_ARPlaneTracker;

        PolySpatialXRHandTracker XRHandTracker => Core.m_ARSessionData.m_XRHandTracker;

        PolySpatialXRHeadTracker XRHeadTracker => Core.m_ARSessionData.m_XRHeadTracker;

        PolySpatialXRMeshTracker XRMeshTracker => Core.m_ARSessionData.m_XRMeshTracker;

        PolySpatialARImageTracker ARImageTracker => Core.m_ARSessionData.m_XRImageTracker;

#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
        PolySpatialXrInputTracker XRInputTracker => Core.m_ARSessionData.m_XRInputTracker;
#endif

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
                    m_Core.InitializeARData();
                }

                return m_Core;
            }
        }

        public unsafe void HandleCommand(PolySpatialCommandHeader cmdHeader, int argCount, void** argValues, int* argSizes)
        {
            switch (cmdHeader.Command)
            {
                case PolySpatialCommand.BeginAppFrame:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out PolySpatialFrameData* frameData);
                    if (id->IsLocal())
                        break;

                    XRHandTracker?.UpdateFrameNumber(id->hostId, frameData->frameNumber);

                    break;
                }
                case PolySpatialCommand.BeginConnection:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out Span<byte> _);

                    ARPlaneTracker.SetHostID(id->hostId);

                    // Send the current state of the hand tracking. Hand tracking state might be active when a Client connects.
                    XRHandTracker.InitHandTracking(id->hostId);

                    ARImageTracker.InitializeARImageTracker(id->hostId);

                    XRMeshTracker.InitializeXRMeshes(id->hostId);

                    PolySpatialXRHeadTracker.StartConnection(id->hostId);

                    break;
                }
                case PolySpatialCommand.EndConnection:
                {
                    if (Core.m_ARSessionData == null)
                        break;

                    ARPlaneTracker.EndConnection();

                    XRHandTracker.EndConnection();

                    ARImageTracker.EndConnection();

                    XRMeshTracker.EndConnection();

                    PolySpatialXRHeadTracker.EndConnection();

#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
                    XRInputTracker.EndConnection();
#endif
                    break;
                }
                case PolySpatialCommand.BeginSession:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out Span<byte> _);
                    XRDisplayTracker?.StartSession(id->hostId);
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
                case PolySpatialCommand.XRPlaneSubsystemStart:
                {
                    ARPlaneTracker.Start();
                    break;
                }
                case PolySpatialCommand.XRPlaneSubsystemStop:
                {
                    ARPlaneTracker.Stop();
                    break;
                }
            }

            NextHandler.HandleCommand(cmdHeader, argCount, argValues, argSizes);
        }

        [CommandHandlerCreationCallback(stage: CommandHandlerGraph.Stage.NetworkAppHost)]
        static IPolySpatialChainableCommandHandler Create(CommandHandlerGraph.HandlerCreationContext context)
        {
            return new XRLocalCommandHandler();
        }
    }
}
