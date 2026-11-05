using System;
using Unity.PolySpatial.Internals;
using UnityEngine;
using Unity.PolySpatial.Input;
using UnityObject = UnityEngine.Object;

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

        GameObject m_DisabledXRRig;

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
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialFrameData* frameData);
                    if (cmdHeader.connectionId.IsLocal())
                        break;

                    XRHandTracker?.UpdateFrameNumber(cmdHeader.connectionId, frameData->frameNumber);

                    break;
                }
                case PolySpatialCommand.BeginSession:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialSessionData session);
                    var xrSettings = session.xrSettings;
                    if (xrSettings != null)
                    {
                        Logging.Log(LogCategory.Input, $"Has XR in connected simulation.");
                        if (xrSettings.hasXRRig)
                        {
                            var xrRigHelper = UnityObject.FindAnyObjectByType<XRRigHelper>();
                            if (xrRigHelper)
                            {
                                Logging.Log(LogCategory.Input, $"Disabling host rig controls as sim wil render it's own.");
                                // If the simulation has a rig, we need to disable rendering of our
                                // controllers to remove visual duplicates.
                                xrRigHelper.DisableObjects();
                            }
                        }
                    }
                    XRDisplayTracker?.StartSession(cmdHeader.connectionId);

                    // Send the current state of the hand tracking. Hand tracking state might be active when a Client connects.
                    XRHandTracker.InitHandTracking(cmdHeader.connectionId);

                    ARImageTracker.InitializeARImageTracker();

                    XRMeshTracker.InitializeXRMeshes();

                    PolySpatialXRHeadTracker.StartConnection(cmdHeader.connectionId);
                    break;
                }
                case PolySpatialCommand.EndSession:
                {
                    var xrRigHelper = UnityObject.FindAnyObjectByType<XRRigHelper>();
                    if (xrRigHelper)
                    {
                        Logging.Log(LogCategory.Input, $"Re-enabling host rig controls.");
                        // If there is a rig, we need to re-enable the controllers that
                        // we disabled when the session started.
                        xrRigHelper.ResetObjects();
                    }

                    if (Core.m_ARSessionData == null)
                        break;

                    ARPlaneTracker.EndSession();

                    XRHandTracker.EndSession();

                    ARImageTracker.EndSession();

                    XRMeshTracker.EndSession();

                    PolySpatialXRHeadTracker.EndSession();

#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
                    XRInputTracker.EndSession();
#endif
                    break;
                }

                case PolySpatialCommand.CreateOrUpdateReferenceImageLibrary:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes,
                        out PolySpatialReferenceImageLibrary referenceImageLibrary);
                    ARImageTracker.CreateOrUpdateReferenceImageLibrary(referenceImageLibrary);

                    break;
                }
                case PolySpatialCommand.AddTrackedImage:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialXRReferenceImage image);
                    ARImageTracker.AddTrackedImage(image);
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

        internal static XRLocalCommandHandler Create(CommandHandlerGraph.HandlerCreationContext context)
        {
            return new XRLocalCommandHandler();
        }
    }
}
