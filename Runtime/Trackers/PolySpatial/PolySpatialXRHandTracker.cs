using System.Collections.Generic;
using UnityEngine;
using Unity.PolySpatial.Internals;
using System;

#if INCLUDE_UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Tracks changes from XRHandSubsystem, subscribes to XRHandSubsystem's updatedHands events,
    /// trackingLost, and trackingFound events.
    /// <see cref="Unity.PolySpatial.XR.Internals.Subsystems.PolySpatialHandSubsystem"/> for detailed description of data flow.
    /// </summary>
    class PolySpatialXRHandTracker : IDisposable
    {
#if INCLUDE_UNITY_XR_HANDS
        XRHandSubsystem m_HandSubsystem;

        HandCacheData m_LeftHand = new (PolySpatialHandID.Left);
        HandCacheData m_RightHand = new (PolySpatialHandID.Right);

        PolySpatialHostID m_PolySpatialHostID;
        int m_CurrentAppFrameNumber;
        int m_LastAppFrameNumber;
        bool m_Initialized;

        static readonly List<XRHandSubsystem> k_SubsystemsReuse = new ();

        /// <summary>
        /// Holds the cached hand data for each hand, so that we can compare the data and only send the changes.
        /// However, most of the time all the joints are updated anyway, but hand data will not be sent for hands
        /// that lose tracking.
        /// </summary>
        class HandCacheData
        {
            internal Pose Root { get; private set; } = Pose.identity;

            internal bool m_IsTracked;

            internal PolySpatialHandID HandID { get; private set; }

            internal Dictionary<XRHandJointID, PolySpatialJointData> m_JointData = new ();

            void ResetData()
            {
                Root = Pose.identity;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                var length = XRHandJointID.EndMarker + VisionOSHandExtensions.NumVisionOSJoints;
#else
                var length = XRHandJointID.EndMarker;
#endif

                // Initialize to identity
                for (var id = XRHandJointID.BeginMarker; id < length; id++)
                {
                    m_JointData.Add(id, new()
                    {
                        pose = Pose.identity,
                        radius = 0,
                        angularVelocity = Vector3.zero,
                        linearVelocity = Vector3.zero,
                        visionOSRotation = Quaternion.identity
                    });
                }
            }

            internal HandCacheData(PolySpatialHandID handID)
            {
                HandID = handID;
                ResetData();
            }

            internal void SetRoot(Pose pose)
            {
                Root = pose;
            }
        }

        void CheckAndSendChanges(HandCacheData hand)
        {
            if (!hand.m_IsTracked)
                return;

            // Build a list of all the updated poses
            var updatedPoses = new List<PolySpatialJointData>();
            foreach (var joint in hand.m_JointData.Values)
            {
                var polySpatialData = new PolySpatialJointData
                {
                    jointId = joint.jointId,
                    pose = joint.pose,
                    radius = joint.radius,
                    angularVelocity = joint.angularVelocity,
                    linearVelocity = joint.linearVelocity,
                    visionOSRotation = joint.visionOSRotation,
                    visionOSTrackingState = joint.visionOSTrackingState,
                    trackingState = joint.trackingState
                };

                updatedPoses.Add(polySpatialData);
            }

            HostCommandHelper.SendXRHandData(new()
            {
                handID = hand.HandID,
                rootPose = hand.Root,
                updatedPoses = updatedPoses
            }, m_PolySpatialHostID);
        }

        //Polling for hand subsystem and subscribing to hand data changed event
        internal void Update()
        {
            if (!m_Initialized)
            {
                if (m_HandSubsystem != null)
                    return;

                SubsystemManager.GetSubsystems(k_SubsystemsReuse);
                if (k_SubsystemsReuse.Count == 0)
                    return;

                m_HandSubsystem = k_SubsystemsReuse[0];

                m_HandSubsystem.trackingAcquired += OnTrackingAcquired;
                m_HandSubsystem.trackingLost += OnTrackingLost;
                m_HandSubsystem.updatedHands += OnUpdatedHands;

                HostCommandHelper.UpdateHandLayout(m_HandSubsystem.jointsInLayout, m_PolySpatialHostID);

                m_Initialized = true;
            }
            else
            {
                if (!m_PolySpatialHostID.IsLocal())
                {
                    // Prevent hand updates from being sent over the network faster than the app simulation is running
                    // This is because XRHandData is too big to send at 90hz over network. This could change once todo LXR-4000 is completed
                    if (m_LastAppFrameNumber < m_CurrentAppFrameNumber)
                        m_LastAppFrameNumber = m_CurrentAppFrameNumber;
                    else
                        return;
                }
                CheckAndSendChanges(m_LeftHand);
                CheckAndSendChanges(m_RightHand);
            }
        }

        static void UpdateJoint (HandCacheData cacheData, XRHandJoint joint)
        {
            var jointID = joint.id;

            joint.TryGetPose(out var pose);
            cacheData.m_JointData[jointID].pose =  pose;

            joint.TryGetRadius(out var radius);
            cacheData.m_JointData[jointID].radius = radius;

            joint.TryGetAngularVelocity(out var angularVelocity);
            cacheData.m_JointData[jointID].angularVelocity = angularVelocity;

            joint.TryGetLinearVelocity(out var linearVelocity);
            cacheData.m_JointData[jointID].linearVelocity = linearVelocity;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
            VisionOSHandExtensions.TryGetVisionOSRotation(joint, out var rotation);
            cacheData.m_JointData[jointID].visionOSRotation = rotation;

            VisionOSHandExtensions.TryGetVisionOSTrackingState(joint, out var trackingState);
            cacheData.m_JointData[jointID].visionOSTrackingState = trackingState;
#endif

            cacheData.m_JointData[joint.id].trackingState = (PolySpatialJointTrackingState)joint.trackingState;
        }

        static void UpdateHandJoints(XRHand hand, HandCacheData cacheData)
        {
            for (var jointID = XRHandJointID.BeginMarker; jointID < XRHandJointID.EndMarker; jointID++)
            {
                var joint = hand.GetJoint(jointID);
                UpdateJoint(cacheData, joint);
            }

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
            UpdateJoint(cacheData, hand.GetVisionOSJoint(VisionOSHandJointID.ForearmWrist));
            UpdateJoint(cacheData, hand.GetVisionOSJoint(VisionOSHandJointID.ForearmArm));
#endif
        }

        void OnUpdatedHands(
            XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            //only update dynamic hands, don't need to check both this and before render
            if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            {
                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose) != 0)
                {
                    if (m_LeftHand.Root != subsystem.leftHand.rootPose)
                        m_LeftHand.SetRoot(subsystem.leftHand.rootPose);
                }

                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose) != 0)
                {
                    if (m_RightHand.Root != subsystem.rightHand.rootPose)
                        m_RightHand.SetRoot(subsystem.rightHand.rootPose);
                }

                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0)
                    UpdateHandJoints(subsystem.leftHand, m_LeftHand);

                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0)
                    UpdateHandJoints(subsystem.rightHand, m_RightHand);
            }
        }

        void OnTrackingLost(XRHand hand)
        {
            var isLeftHand = hand.handedness == Handedness.Left;

            if (isLeftHand)
            {
                m_LeftHand.m_IsTracked = false;
            }
            else
            {
                m_RightHand.m_IsTracked = false;
            }

            HostCommandHelper.OnXRHandTrackingEvent(isLeftHand ? PolySpatialHandID.Left : PolySpatialHandID.Right,
                PolySpatialXRHandTrackingEvent.Lost,
                m_PolySpatialHostID);
        }

        void OnTrackingAcquired(XRHand hand)
        {
            var isLeftHand = hand.handedness == Handedness.Left;

            if (isLeftHand)
            {
                m_LeftHand.m_IsTracked = true;
            }
            else
            {
                m_RightHand.m_IsTracked = true;
            }

            HostCommandHelper.OnXRHandTrackingEvent(isLeftHand ? PolySpatialHandID.Left : PolySpatialHandID.Right,
                PolySpatialXRHandTrackingEvent.Acquired,
                m_PolySpatialHostID);
        }

        internal void EndConnection()
        {
            Dispose();
        }

        internal void UpdateFrameNumber(PolySpatialHostID hostID, int frameNumber)
        {
            if (hostID != m_PolySpatialHostID)
                return;

            m_CurrentAppFrameNumber = frameNumber;
        }

        internal void InitHandTracking(PolySpatialHostID hostID)
        {
            m_PolySpatialHostID = hostID;
            m_CurrentAppFrameNumber = 0;
            m_LastAppFrameNumber = 0;

            if (m_LeftHand.m_IsTracked)
                HostCommandHelper.OnXRHandTrackingEvent(PolySpatialHandID.Left, PolySpatialXRHandTrackingEvent.Acquired, m_PolySpatialHostID);

            if (m_RightHand.m_IsTracked)
                HostCommandHelper.OnXRHandTrackingEvent(PolySpatialHandID.Right, PolySpatialXRHandTrackingEvent.Acquired, m_PolySpatialHostID);
        }

        public void Dispose()
        {
            if (m_Initialized)
            {
                if (m_HandSubsystem != null)
                {
                    m_HandSubsystem.trackingAcquired -= OnTrackingAcquired;
                    m_HandSubsystem.trackingLost -= OnTrackingLost;
                    m_HandSubsystem.updatedHands -= OnUpdatedHands;
                }

                m_HandSubsystem = null;
                m_Initialized = false;
            }
        }
#else
        internal void EndConnection()
        {
        }

        internal void Update()
        {
        }

        public void InitHandTracking(PolySpatialHostID hostID)
        {
        }

        public void Dispose()
        {
        }
#endif
    }
}

