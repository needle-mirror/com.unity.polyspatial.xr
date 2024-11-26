using System;
using Unity.PolySpatial.Internals;
using UnityEngine;

#if INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialXRHeadTracker : MonoBehaviour
    {
        static PolySpatialHostID s_HostID;
        static bool s_Connected;
        bool m_IsSimulator;

        void Awake()
        {
#if UNITY_EDITOR
            m_IsSimulator = true;
#elif INCLUDE_UNITY_XR_VISIONOS && UNITY_VISIONOS
            // Cache isSimulator because it isn't cheap to call
            m_IsSimulator = VisionOS.IsSimulator();
#endif
        }

        internal static void StartConnection(PolySpatialHostID hostID)
        {
            s_HostID = hostID;
            s_Connected = true;
        }

        internal static void EndConnection()
        {
            s_Connected = false;
        }

        internal void Update()
        {
            if (!s_Connected)
                return;

            // Always send head pose from simulator because the Editor will reset input devices on regaining focus;
            // For example when switching tasks to move around in the simulator, focusing the Editor to make changes will reset head pose unless this command keeps firing
            if (m_IsSimulator || transform.hasChanged)
            {
                HostCommandHelper.SendXRHeadPose(s_HostID, transform.position, transform.rotation);
                transform.hasChanged = false;
            }
        }
    }
}
