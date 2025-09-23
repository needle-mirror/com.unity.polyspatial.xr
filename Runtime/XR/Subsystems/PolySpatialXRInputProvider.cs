using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

[assembly:InternalsVisibleTo("Unity.PolySpatial.Runtime.IntegrationTests")]

namespace Unity.PolySpatial.InputDevices
{
    internal static class XRInputProvider
    {
        static internal bool EnsureInitialized()
        {
            if (s_Subsystem != null)
                return true;

            var descs = new List<XRInputSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descs);
            foreach (var desc in descs)
            {
                if (desc.id != "PolySpatialXR-Input")
                    continue;

                s_Subsystem = desc.Create();

                if (s_Subsystem == null)
                {
                    Debug.LogWarning(
                        "Found PolySpatial provider for XRInputSubsystem, but failed to create it - XR-related PolySpatial input devices will not function correctly!");
                    return false;
                }

                s_Subsystem.Start();
                return s_Subsystem.running;
            }

            Debug.LogWarning("Failed to create PolySpatial XRInputSubsystem - simulated XR input devices won't be able to work!");
            return false;
        }

        static XRInputSubsystem s_Subsystem;
        internal static XRInputSubsystem Subsystem
        {
            get
            {
                EnsureInitialized();
                return s_Subsystem;
            }
        }
    }
}
