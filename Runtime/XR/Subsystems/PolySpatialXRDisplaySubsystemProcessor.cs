using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    class PolySpatialXRDisplaySubsystemProcessor
    {
        internal const string k_SubsystemId = "PolySpatialXR-Display";

        XRDisplaySubsystem m_Subsystem;

        static XRDisplaySubsystem GetActiveSubsystemInstance()
        {
            XRDisplaySubsystem activeSubsystem = null;

            // Query the currently active loader for the created subsystem, if one exists.
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var loader = XRGeneralSettings.Instance.Manager.activeLoader;
                if (loader != null)
                    activeSubsystem = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
                if (activeSubsystem != null && activeSubsystem.subsystemDescriptor.id != k_SubsystemId)
                    activeSubsystem = null;
            }

            return activeSubsystem;
        }

        internal PolySpatialXRDisplaySubsystemProcessor()
        {
            m_Subsystem ??= GetActiveSubsystemInstance();
        }

        internal unsafe void SetData(PolySpatialXRDisplayData data, Span<byte> rawData)
        {
            if (m_Subsystem == null)
            {
                Logging.LogError(
                    LogCategory.XR,
                    "Received XR display data, but PolySpatial XR plug-in is not enabled. " +
                    "Please enable PolySpatial XR plug-in for proper XR display support.");
                return;
            }
            if (!data.running)
            {
                if (m_Subsystem.running)
                    m_Subsystem.Stop();
                return;
            }
            if (!m_Subsystem.running)
                m_Subsystem.Start();

            fixed (void* p = rawData)
            {
                SetDisplayData(p, rawData.Length);
            }
        }

        [DllImport("PolySpatialXRPlugin", EntryPoint = "PolySpatialXRSubsystem_SetDisplayData")]
        static extern unsafe void SetDisplayData(void* data, int size);
    }
}