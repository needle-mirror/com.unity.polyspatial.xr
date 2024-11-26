// This comes from com.unity.inputsystem/InputSystem/Plugins/XR/GenericXRDevice.cs and was added to avoid errors on TvOS.
// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
#define XR_DEVICES_AVAILABLE
#endif

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

#if XR_DEVICES_AVAILABLE
using UnityEngine.InputSystem.XR;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.PolySpatial.InputDevices
{
    /// All PolySpatialInput which is fed into an app is fed into the various PolySpatial_____ device abstraction.
    /// This helps with internal testing/development, but also enables a user to selectively change
    /// behaviour based on input from PolySpatial if for whatever reason it becomes relevant.

#if XR_DEVICES_AVAILABLE

    [StructLayout(LayoutKind.Explicit, Size = k_SizeInBytes)]
    struct PolySpatialXRHMDDeviceState : IInputStateTypeInfo
    {
        const int k_SizeOfVector3 = sizeof(float) * 3;
        const int k_SizeOfQuaternion = sizeof(float) * 4;
        const int k_TrackingStateOffset = 0;
        const int k_IsTrackedOffset = k_TrackingStateOffset + sizeof(int);
        const int k_DevicePositionOffset = k_IsTrackedOffset + sizeof(bool);
        const int k_DeviceRotationOffset = k_DevicePositionOffset + k_SizeOfVector3;
        const int k_LeftEyePositionOffset = k_DeviceRotationOffset + k_SizeOfQuaternion;
        const int k_LeftEyeRotationOffset = k_LeftEyePositionOffset + k_SizeOfVector3;
        const int k_RightEyePositionOffset = k_LeftEyeRotationOffset + k_SizeOfQuaternion;
        const int k_RightEyeRotationOffset = k_RightEyePositionOffset + k_SizeOfVector3;
        const int k_CenterEyePositionOffset = k_RightEyeRotationOffset + k_SizeOfQuaternion;
        const int k_CenterEyeRotationOffset = k_CenterEyePositionOffset + k_SizeOfVector3;

        /// <summary>
        /// The size in bytes of the PolySpatial XR HMD state struct.
        /// </summary>
        const int k_SizeInBytes = k_CenterEyeRotationOffset + k_SizeOfQuaternion;

        static FourCC Format => new('P', 'H', 'M', 'D');

        [InputControl(displayName = "Tracking State", noisy = true, dontReset = true)]
        [FieldOffset(k_TrackingStateOffset)]
        public int trackingState;

        [InputControl(displayName = "Is Tracked", noisy = true, dontReset = true)]
        [FieldOffset(k_IsTrackedOffset)]
        public bool isTracked;

        [InputControl(displayName = "Device Position", noisy = true, dontReset = true)]
        [FieldOffset(k_DevicePositionOffset)]
        public Vector3 devicePosition;

        [InputControl(displayName = "Device Rotation", noisy = true, dontReset = true)]
        [FieldOffset(k_DeviceRotationOffset)]
        public Quaternion deviceRotation;

        [InputControl(displayName = "Left Eye Position", noisy = true, dontReset = true)]
        [FieldOffset(k_LeftEyePositionOffset)]
        public Vector3 leftEyePosition;

        [InputControl(displayName = "Left Eye Rotation", noisy = true, dontReset = true)]
        [FieldOffset(k_LeftEyeRotationOffset)]
        public Quaternion leftEyeRotation;

        [InputControl(displayName = "Right Eye Position", noisy = true, dontReset = true)]
        [FieldOffset(k_RightEyePositionOffset)]
        public Vector3 rightEyePosition;

        [InputControl(displayName = "Right Eye Rotation", noisy = true, dontReset = true)]
        [FieldOffset(k_RightEyeRotationOffset)]
        public Quaternion rightEyeRotation;

        [InputControl(displayName = "Center Eye Position", noisy = true, dontReset = true)]
        [FieldOffset(k_CenterEyePositionOffset)]
        public Vector3 centerEyePosition;

        [InputControl(displayName = "Center Eye Rotation", noisy = true, dontReset = true)]
        [FieldOffset(k_CenterEyeRotationOffset)]
        public Quaternion centerEyeRotation;

        public FourCC format => Format;
    }

    // ReSharper disable ClassNeverInstantiated.Global
    [InputControlLayout(displayName = "PolySpatial XR Controller", canRunInBackground = true, commonUsages = new[] { "LeftHand", "RightHand" })]
    class PolySpatialXRController : XRController
    {
    }

    [InputControlLayout(displayName = "PolySpatial XR HMD", canRunInBackground = true, stateType = typeof(PolySpatialXRHMDDeviceState))]
    class PolySpatialXRHMD : XRHMD
    {
    }
    // ReSharper restore ClassNeverInstantiated.Global
#endif


    /// <summary>
    /// Initializes PolySpatial Devices.
    /// Cleans up PolySpatial Devices in Editor.
    /// Ensures InputSystem is configured properly for PolySpatialDevices.
    /// </summary>
#if UNITY_EDITOR
    // Add UnityEditor.InitializeOnLoad here to register layouts on editor open so they are in InputSystem UI.
    [InitializeOnLoad]
#endif
    static class PolySpatialXRInputDeviceInitialization
    {
#if UNITY_EDITOR
        static PolySpatialXRInputDeviceInitialization()
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            Initialize();
        }

        static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // PolySpatial devices get lazily instantiated when needed and stay the full runtime of the application.
                    // So in editor clean them up when exiting edit mode.
                    CleanupPolySpatialDevices();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateChange), stateChange, null);
            }
        }

        static void CleanupPolySpatialDevices()
        {
            for (var i = InputSystem.devices.Count - 1; i > -1; i--)
            {
                if (InputSystem.devices[i].name.Contains("PolySpatial"))
                {
                    InputSystem.RemoveDevice(InputSystem.devices[i]);
                }
            }
        }
#endif

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static void Initialize()
        {
#if XR_DEVICES_AVAILABLE
            InputSystem.RegisterLayout<PolySpatialXRController>();
            InputSystem.RegisterLayout<PolySpatialXRHMD>();
#endif
        }
    }
}
