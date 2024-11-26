// This comes from com.unity.inputsystem/InputSystem/Plugins/XR/GenericXRDevice.cs and was added to avoid errors on TvOS.
// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
#define XR_DEVICES_AVAILABLE
#endif

using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;

#if XR_DEVICES_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.XR;
#endif

namespace Unity.PolySpatial.InputDevices
{
    /// <summary>
    /// Receives serialized TrackedPose events and converts them to XRHMD device.
    /// </summary>
    class PolySpatialXRHMDEventListener
    {
        static PolySpatialXRHMDEventListener s_Instance;
#if XR_DEVICES_AVAILABLE
        readonly PolySpatialXRHMD m_XRHMD;
#endif

        public PolySpatialXRHMDEventListener()
        {
#if XR_DEVICES_AVAILABLE
            InputUtils.AddDevice(out PolySpatialXRHMD device);
            m_XRHMD = device;
#endif

            s_Instance = this;
        }

        void OnTrackedDeviceInputEvent(NativeArray<PolySpatialHeadPoseEvent> events)
        {
            foreach (var pointerEvent in events)
            {
                ForwardToInputSystem(pointerEvent);
            }
        }

        void ForwardToInputSystem(PolySpatialHeadPoseEvent poseEvent)
        {
#if XR_DEVICES_AVAILABLE
            var pose = poseEvent.pose;
            var position = pose.position;
            var rotation = pose.rotation;
            InputSystem.QueueStateEvent(m_XRHMD, new PolySpatialXRHMDDeviceState
            {
                trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation),
                isTracked = poseEvent.tracked,
                devicePosition = position,
                deviceRotation = rotation,
                centerEyePosition = position,
                centerEyeRotation = rotation
            });
#endif
        }

        public static void OnHeadPoseEvent(PolySpatialHostID hostId, NativeArray<PolySpatialHeadPoseEvent> poseEvents)
        {
            if (s_Instance == null)
                return;

            s_Instance.OnTrackedDeviceInputEvent(poseEvents);
        }

        public void Deinitialize()
        {
            if (s_Instance == this)
                s_Instance = null;
            else
                Debug.LogWarning($"Attempting to deinitialize a {nameof(PolySpatialXRHMDEventListener)} that is not the current singleton instance.");
        }
    }
}
