using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.PolySpatial.XR.Internals
{
    internal static class XrInputSystemNativeApi
    {
        const string k_LibraryName = "PolySpatialXRPlugin";

        // Controller API
        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_AddController")]
        public static extern uint AddController(
            InputDeviceCharacteristics characteristics,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string manufacturer,
            [MarshalAs(UnmanagedType.LPStr)] string serialNumber);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_RemoveController")]
        public static extern void RemoveController(uint xrInputDeviceId);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_IsTracked")]
        public static extern void SetControllerIsTracked(uint xrInputDeviceId, bool isTracked);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_TrackingState")]
        public static extern void SetControllerTrackingState(uint xrInputDeviceId, uint trackingState);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Position")]
        public static extern void SetControllerPosition(uint xrInputDeviceId, in Vector3 position);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Rotation")]
        public static extern void SetControllerRotation(uint xrInputDeviceId, in Quaternion rotation);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_PrimaryButton")]
        public static extern void SetPrimaryButton(uint xrInputDeviceId, bool pressed);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_PrimaryTouch")]
        public static extern void SetPrimaryTouch(uint xrInputDeviceId, bool touched);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_SecondaryButton")]
        public static extern void SetSecondaryButton(uint xrInputDeviceId, bool pressed);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_SecondaryTouch")]
        public static extern void SetSecondaryTouch(uint xrInputDeviceId, bool touched);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Grip")]
        public static extern void SetGrip(uint xrInputDeviceId, float grip);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Trigger")]
        public static extern void SetTrigger(uint xrInputDeviceId, float trigger);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_GripButton")]
        public static extern void SetGripButton(uint xrInputDeviceId, bool pressed);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_TriggerButton")]
        public static extern void SetTriggerButton(uint xrInputDeviceId, bool pressed);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_MenuButton")]
        public static extern void SetMenuButton(uint xrInputDeviceId, bool pressed);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Primary2DClick")]
        public static extern void SetPrimary2DClick(uint xrInputDeviceId, bool clicked);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Primary2DTouch")]
        public static extern void SetPrimary2DTouch(uint xrInputDeviceId, bool touched);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Secondary2DClick")]
        public static extern void SetSecondary2DClick(uint xrInputDeviceId, bool clicked);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Secondary2DTouch")]
        public static extern void SetSecondary2DTouch(uint xrInputDeviceId, bool touched);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Primary2D")]
        public static extern void SetPrimary2D(uint xrInputDeviceId, in Vector2 position);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetController_Secondary2D")]
        public static extern void SetSecondary2D(uint xrInputDeviceId, in Vector2 position);

        // HMD API

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_AddHmd")]
        public static extern uint AddHmd(
            InputDeviceCharacteristics characteristics,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string manufacturer,
            [MarshalAs(UnmanagedType.LPStr)] string serialNumber);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_RemoveHmd")]
        public static extern void RemoveHmd(uint deviceId);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_IsTracked")]
        public static extern void SetHmdIsTracked(uint xrInputDeviceId, bool isTracked);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_TrackingState")]
        public static extern void SetHmdTrackingState(uint xrInputDeviceId, uint trackingState);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_Position")]
        public static extern void SetHmdPosition(uint xrInputDeviceId, in Vector3 position);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_Rotation")]
        public static extern void SetHmdRotation(uint xrInputDeviceId, in Quaternion rotation);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_LeftEyePosition")]
        public static extern void SetLeftEyePosition(uint xrInputDeviceId, in Vector3 leftEyePosition);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_LeftEyeRotation")]
        public static extern void SetLeftEyeRotation(uint xrInputDeviceId, in Quaternion leftEyeRotation);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_RightEyePosition")]
        public static extern void SetRightEyePosition(uint xrInputDeviceId, in Vector3 rightEyePosition);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_RightEyeRotation")]
        public static extern void SetRightEyeRotation(uint xrInputDeviceId, in Quaternion rightEyeRotation);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_CenterEyePosition")]
        public static extern void SetCenterEyePosition(uint xrInputDeviceId, in Vector3 centerEyePosition);

        [DllImport(k_LibraryName, EntryPoint = "UnityPolySpatial_SetHmd_CenterEyeRotation")]
        public static extern void SetCenterEyeRotation(uint xrInputDeviceId, in Quaternion centerEyeRotation);
    }
}
