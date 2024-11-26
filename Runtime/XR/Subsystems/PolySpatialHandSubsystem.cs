#if INCLUDE_UNITY_XR_HANDS
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    /// <summary>
    /// The Subsystem which runs on the PolySpatial Client side. It is Initialized by PolySpatialXRLoader (When the
    /// PolySpatial Networked XR Plug-In is selected. This class keeps track of all Hand joints and tracking events,
    /// and provides other XR classes with hand data when they subscript to XRHandSubsystem's handsUpdated, trackingLost,
    /// and trackingFound events.
    ///
    /// ╔═════════════════════════════════════════╗     ╔════════════════════════════════════════════════════════════╗
    /// ║            PolySpatial HOST             ║     ║                      PolySpatial Client                    ║
    /// ╚═════════════════════════════════════════╝     ╚════════════════════════════════════════════════════════════╝
    ///
    ///         ┌─────────────────┐                             ┌─────────────────────────────┐
    ///         │ XRHandSubsystem │                             │PolySpatialSimulationHostImpl│
    ///         └─────────────────┘                             └─────────────────────────────┘
    /// (implemented by ARKit or OpenXR, etc.)                                │
    ///                  │                                        (set Init, Tracking, SetData)
    ///    (gets hands data on handsUpdated                                   │
    ///                event)                                                 │
    ///                  │                                                    V
    ///                  V                                           ┌────────────────────────┐
    ///       ┌──────────────────────────┐                           │PolySpatialHandSubsystem│
    ///       │ PolySpatialXRHandTracker │ (on host side)            │     which is a         │
    ///       └──────────────────────────┘                           │   XRHandSubsystem      │
    ///                  │                                           └────────────────────────┘
    ///                  │                                                    │
    ///                  │                                                    │       handsUpdated event
    ///                  V                                                    └───────────────────────────> Any Subscriber
    ///    HostCommandHelper.SendXRHandData
    /// HostCommandHelper.SendXRHandTrackingData
    ///
    /// </summary>
    class PolySpatialHandSubsystem : XRHandSubsystem
    {
        internal const string k_SubsystemId = "XRPolySpatial-Hand";

        /// <summary>
        /// The provider implementation for this subsystem.
        /// Keeps track of all Hand joint information and tracking state of hands/joints
        /// </summary>
        class PolySpatialHandProvider : XRHandSubsystemProvider
        {
            UpdateSuccessFlags m_UpdateFlags = UpdateSuccessFlags.None;
            readonly HandData m_HandData = new();
            PolySpatialHostID m_FirstHostId;

            bool m_HandLayoutUpdated;

            NativeArray<bool> m_UpdatedHandLayout;

            public override void Destroy()
            {
                m_HandData.Dispose();
            }

            public override void Start()
            {
                // Reset first host ID to accept a new connection. Commands shouldn't be coming from local, so a connectionId of 0 can be considered invalid
                m_FirstHostId = default;
            }

            public override void Stop() { }

            public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
            {
                // This will get called once during subsystem creation, but we will call it again in
                // PolySpatialHandSubsystem with any updated values from the host.

                if (!m_HandLayoutUpdated)
                {
                    // Default to assume all joints are supported.
                    for (var i = 0; i < handJointsInLayout.Length; i++)
                    {
                        handJointsInLayout[i] = true;
                    }
                }
                else
                {
                    // Host gave us updated joint support data. Ex: on visionOS it is know that the Palm is not supported.
                    for (var i = 0; i < handJointsInLayout.Length; i++)
                    {
                        handJointsInLayout[i] = m_UpdatedHandLayout[i];
                    }
                }
            }

            internal void UpdateHandLayout(NativeArray<bool> updatedHandLayout)
            {
                m_UpdatedHandLayout = updatedHandLayout;
                m_HandLayoutUpdated = true;
            }

            public override UpdateSuccessFlags TryUpdateHands(UpdateType updateType, ref Pose leftHandRootPose, NativeArray<XRHandJoint> leftHandJoints,
                ref Pose rightHandRootPose, NativeArray<XRHandJoint> rightHandJoints)
            {
                var ret = m_UpdateFlags;

                if (m_UpdateFlags != UpdateSuccessFlags.None)
                {
                    leftHandRootPose = m_HandData.m_LeftHand.m_RootPose;
                    rightHandRootPose = m_HandData.m_RightHand.m_RootPose;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                    var length = XRHandJointID.EndMarker + VisionOSHandExtensions.NumVisionOSJoints;
#else
                    var length = XRHandJointID.EndMarker;
#endif

                    var leftHandJointData = m_HandData.m_LeftHand.m_Joints;
                    var rightHandJointData = m_HandData.m_RightHand.m_Joints;
                    for (var jointID = XRHandJointID.BeginMarker; jointID < length; jointID++)
                    {
                        var index = jointID.ToIndex();
                        var leftJoint = leftHandJointData[index];
                        var rightJoint = rightHandJointData[index];

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                        VisionOSHandExtensions.SetVisionOSRotation(Handedness.Left, leftJoint, m_HandData.m_LeftHand.m_VisionOSRotations[index]);
                        VisionOSHandExtensions.SetVisionOSRotation(Handedness.Right, rightJoint, m_HandData.m_RightHand.m_VisionOSRotations[index]);

                        VisionOSHandExtensions.SetVisionOSTrackingState(Handedness.Left, leftJoint, m_HandData.m_LeftHand.m_VisionOSTrackingStates[index]);
                        VisionOSHandExtensions.SetVisionOSTrackingState(Handedness.Right, rightJoint, m_HandData.m_RightHand.m_VisionOSTrackingStates[index]);

                        if (jointID >= XRHandJointID.EndMarker)
                        {
                            VisionOSHandExtensions.SetVisionOSJoint(Handedness.Left, leftJoint);
                            VisionOSHandExtensions.SetVisionOSJoint(Handedness.Right, rightJoint);
                            continue;
                        }
#endif

                        leftHandJoints[index] = leftJoint;
                        rightHandJoints[index] = rightJoint;
                    }
                }

                return ret;
            }

            internal void OnTrackingEvent(PolySpatialHandID handId, PolySpatialXRHandTrackingEvent evt, PolySpatialHostID hostID)
            {
                // If m_FirstHostId is equal to the default value, we know this is the first tracking event we've received since startup
                if (m_FirstHostId == default)
                    m_FirstHostId = hostID;

                // If we get hand tracking events from any host other than the first, ignore it
                if (hostID != m_FirstHostId)
                    return;

                switch (handId)
                {
                    case PolySpatialHandID.Left:
                        if (evt == PolySpatialXRHandTrackingEvent.Acquired)
                            m_UpdateFlags |= UpdateSuccessFlags.LeftHandRootPose | UpdateSuccessFlags.LeftHandJoints;
                        else
                            m_UpdateFlags &= ~(UpdateSuccessFlags.LeftHandRootPose | UpdateSuccessFlags.LeftHandJoints);
                        break;
                    case PolySpatialHandID.Right:
                        if (evt == PolySpatialXRHandTrackingEvent.Acquired)
                            m_UpdateFlags |= UpdateSuccessFlags.RightHandRootPose | UpdateSuccessFlags.RightHandJoints;
                        else
                            m_UpdateFlags &= ~(UpdateSuccessFlags.RightHandRootPose | UpdateSuccessFlags.RightHandJoints);
                        break;
                }
            }

            internal void SetHandData(PolySpatialXRHandData handData, PolySpatialHostID hostID)
            {
                if (m_FirstHostId == default) //This should get initialized from Tracking event Acquired
                {
                    // Eat all hand updates until we get our first OnTrackingEvent, this is expected.
                    return;
                }

                // If we get hand data from any host other than the first, ignore it
                if (hostID != m_FirstHostId)
                    return;

                switch (handData.handID)
                {
                    case PolySpatialHandID.Left:
                        m_HandData.m_LeftHand.UpdateHandData(handData);
                        break;
                    case PolySpatialHandID.Right:
                        m_HandData.m_RightHand.UpdateHandData(handData);
                        break;
                }
            }
        }

        // This method registers the subsystem descriptor with the SubsystemManager
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void RegisterDescriptor()
        {
            var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
            {
                id = k_SubsystemId,
                providerType = typeof(PolySpatialHandProvider),
                subsystemTypeOverride = typeof(PolySpatialHandSubsystem)
            };

            XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);
        }

        // Notifies the provider that a hand tracking event has occurred. Lost or Acquired
        internal void OnTrackingEvent(PolySpatialHandID handId, PolySpatialXRHandTrackingEvent evt, PolySpatialHostID hostID)
        {
            if (provider is PolySpatialHandProvider handProvider)
            {
                handProvider.OnTrackingEvent(handId, evt, hostID);
            }
            else
            {
                Logging.LogError(LogCategory.XR, $"provider is not of type PolySpatialHandProvider.");
            }
        }

        // Sets the data for joints on the hand. Joint pose, rotation, velocity, and angular velocity are set.
        internal void SetHandData(PolySpatialXRHandData handData, PolySpatialHostID hostID)
        {
            if (provider is PolySpatialHandProvider handProvider)
            {
                handProvider.SetHandData(handData, hostID);
            }
            else
            {
                Logging.LogError(LogCategory.XR, $"provider is not of type PolySpatialHandProvider.");
            }
        }

        internal void UpdateHandLayout(NativeArray<bool> updatedHandLayout)
        {
            if (provider is PolySpatialHandProvider handProvider)
            {
                handProvider.UpdateHandLayout(updatedHandLayout);
            }
            else
            {
                Logging.LogError(LogCategory.XR, $"provider is not of type PolySpatialHandProvider.");
            }
        }
    }
}

#endif
