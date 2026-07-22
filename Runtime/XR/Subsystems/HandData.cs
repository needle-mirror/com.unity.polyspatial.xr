// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
#define USE_XR_INPUT
#endif

#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    class HandData
    {
        internal struct PolySpatialXRHand
        {
            internal NativeArray<XRHandJoint> m_Joints;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
            internal Quaternion[] m_VisionOSRotations;
            internal bool[] m_VisionOSTrackingStates;
#endif

            internal Pose m_RootPose;

            Handedness m_Handedness;

            public override string ToString()
            {
                return m_Handedness + " XRHand";
            }

            internal PolySpatialXRHand(Handedness handedness, Allocator allocator)
            {
                m_RootPose = Pose.identity;
                m_Handedness = handedness;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                var length = XRHandJointID.EndMarker + VisionOSHandExtensions.NumVisionOSJoints;
                m_VisionOSRotations = new Quaternion[(int)length];
                m_VisionOSTrackingStates = new bool[(int)length];
#else
                var length = XRHandJointID.EndMarker;
#endif

                m_Joints = new NativeArray<XRHandJoint>((int)length, allocator);
                for (var jointID = XRHandJointID.BeginMarker; jointID < length; ++jointID)
                {
                    var index = jointID.ToIndex();
                    m_Joints[index] = XRHandProviderUtility.CreateJoint(
                        handedness,
                        XRHandJointTrackingState.None,
                        jointID,
                        Pose.identity);
#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                        m_VisionOSRotations[index] = Quaternion.identity;
#endif
                }
            }

            internal void Dispose()
            {
                if (m_Joints.IsCreated)
                    m_Joints.Dispose();
            }

            void SetRootPose(Pose pose)
            {
                m_RootPose = pose;
            }

            void SetJointData(XRHandJointID id, PolySpatialJointData data)
            {
                // In disconnecting from P2D I sometimes see events come in attempting to set
                // joint data after the fact which causes exceptions.
                if (!m_Joints.IsCreated)
                    return;

                var index = id.ToIndex();
                m_Joints[index] = XRHandProviderUtility.CreateJoint(
                    m_Handedness,
                    (XRHandJointTrackingState)data.trackingState,
                    id,
                    data.pose.GetValueOrDefault(),
                    data.radius,
                    data.linearVelocity.GetValueOrDefault(),
                    data.angularVelocity.GetValueOrDefault());
#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                m_VisionOSRotations[index] = data.visionOSRotation ?? default;
                m_VisionOSTrackingStates[index] = data.visionOSTrackingState;
#endif
            }

            internal void  UpdateHandData(PolySpatialXRHandData handData)
            {
                SetRootPose(handData.rootPose);

                var updatedPoses = handData.updatedPoses;
                if (updatedPoses != null)
                {
                    var poses = updatedPoses.ToList();

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
                    var length = XRHandJointID.EndMarker + VisionOSHandExtensions.NumVisionOSJoints;
#else
                    var length = XRHandJointID.EndMarker;
#endif

                    for (var jointID = XRHandJointID.BeginMarker; jointID < length; ++jointID)
                    {
                        var poseData = poses[jointID.ToIndex()];
                        SetJointData(jointID, poseData);
                    }
                }
            }
        }

        internal PolySpatialXRHand m_LeftHand = new (Handedness.Left, Allocator.Persistent);
        internal PolySpatialXRHand m_RightHand = new (Handedness.Right, Allocator.Persistent);

        public void Dispose()
        {
            m_LeftHand.Dispose();
            m_RightHand.Dispose();
        }
    }
}

#endif
