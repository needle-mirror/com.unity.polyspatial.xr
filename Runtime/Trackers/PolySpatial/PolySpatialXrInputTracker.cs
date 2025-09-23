#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.PolySpatial.Input.RemoteInputDevices;
using Unity.PolySpatial.InputDevices;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR;
using InputDevice = UnityEngine.InputSystem.InputDevice;

[assembly:InternalsVisibleTo("Unity.PolySpatial.Runtime.IntegrationTests")]

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Tracker responsible for mapping and updating remote PolySpatial input devices
    /// into the XR Input System.
    /// </summary>
    internal class PolySpatialXrInputTracker : IDisposable
    {
        internal const string k_LegacyNamePrefix = "PolySpatialLegacyRemote";
        const string k_FauxManufacturer = "Unity PolySpatial";
        const string k_FauxSerialNumber = "12345";

        bool m_CanMapDevices = true;
        internal bool CanMapDevices => m_CanMapDevices;

        enum XRDeviceType
        {
            LeftController,
            RightController,
            Hmd,
        }

        struct XRMappedDevice
        {
            public uint xrInputDeviceId;
            public XRDeviceType deviceType;
        }

        Dictionary<uint, XRMappedDevice> m_MappedDevices = new Dictionary<uint, XRMappedDevice>();
        HashSet<uint> m_RemovedDevices = new();

        bool m_Disposed = false;

        struct ActiveMappedXRDevice : IEquatable<ActiveMappedXRDevice>
        {
           public uint remoteDeviceId;
           public uint xrInputDeviceId;

           public bool Equals(ActiveMappedXRDevice other)
           {
               return remoteDeviceId == other.remoteDeviceId && xrInputDeviceId == other.xrInputDeviceId;
           }

           public override bool Equals(object obj)
           {
               return obj is ActiveMappedXRDevice other && Equals(other);
           }

           public override int GetHashCode()
           {
               return HashCode.Combine(remoteDeviceId, xrInputDeviceId);
           }
        }

        static ActiveMappedXRDevice s_InvalidMappedXRDevice = new ActiveMappedXRDevice()
        {
            remoteDeviceId = UInt32.MaxValue,
            xrInputDeviceId = UInt32.MaxValue,
        };

        // These are going to represent the single, currently active devices
        // we will be updating. Right now we're just going to set them up to
        // reflect the first instance of each device type that we get. In the
        // future, we may want to have a way of switching between multiple
        // instances of the same device type.
        ActiveMappedXRDevice m_ActiveLeftController = s_InvalidMappedXRDevice;
        ActiveMappedXRDevice m_ActiveRightController = s_InvalidMappedXRDevice;
        ActiveMappedXRDevice m_ActiveHmd = s_InvalidMappedXRDevice;

        bool TryAddXrInputSystemHmdDeviceId(uint deviceId, PolySpatialInputDeviceManager.MappedDevice? mappedDevice,
            UnityEngine.InputSystem.InputDevice device)
        {
            if (m_MappedDevices.ContainsKey(deviceId))
                return false;

            var characteristics = InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.HeadMounted;
            var deviceName = $"{k_LegacyNamePrefix}HMD{mappedDevice.Value.hostDeviceName}:{mappedDevice.Value.hostDeviceId}";
            var xrInputDeviceId = XrInputSystemNativeApi.AddHmd(
                characteristics,
                deviceName,
                k_FauxManufacturer,
                k_FauxSerialNumber);
            if (xrInputDeviceId == UInt32.MaxValue)
            {
                Logging.LogError(LogCategory.Diagnostics, $"Failed to add XR input device for {deviceId}:{device.name}. XR Input ID: {xrInputDeviceId}");
                return false;
            }

            Logging.Log(LogCategory.Diagnostics, $"Adding mapped device {xrInputDeviceId} device {deviceId}:{device.name}");

            m_MappedDevices[deviceId] = new XRMappedDevice()
            {
                xrInputDeviceId = xrInputDeviceId,
                deviceType =  XRDeviceType.Hmd,
            };

            return true;
        }

        bool TryAddXrInputSystemControllerDeviceId(uint deviceId, bool leftHand, bool rightHand,
            PolySpatialInputDeviceManager.MappedDevice? mappedDevice,
            UnityEngine.InputSystem.InputDevice device)
        {
            if (m_MappedDevices.ContainsKey(deviceId))
                return false;

            var characteristics = InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand;
            characteristics |= leftHand ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
            var deviceName = $"{k_LegacyNamePrefix}Controller{mappedDevice.Value.hostDeviceName}:{mappedDevice.Value.hostDeviceId}";
            var xrInputDeviceId = XrInputSystemNativeApi.AddController(
                characteristics,
                deviceName,
                k_FauxManufacturer,
                k_FauxSerialNumber);
            if (xrInputDeviceId == UInt32.MaxValue)
            {
                Logging.LogError(LogCategory.Diagnostics, $"Failed to add XR input device for {deviceId}:{device.name}. XR Input ID: {xrInputDeviceId}");
                return false;
            }

            Logging.Log(LogCategory.Diagnostics, $"Adding mapped device {xrInputDeviceId} device {deviceId}:{device.name}");

            m_MappedDevices[deviceId] = new XRMappedDevice()
            {
                xrInputDeviceId = xrInputDeviceId,
                deviceType = leftHand ? XRDeviceType.LeftController : XRDeviceType.RightController,
            };

            return true;
        }


        ActiveMappedXRDevice FindFallbackDevice(XRDeviceType deviceType)
        {
            foreach (var kvp in m_MappedDevices)
            {
                if (kvp.Value.deviceType == deviceType)
                {
                    return new ActiveMappedXRDevice()
                    {
                        remoteDeviceId = kvp.Key,
                        xrInputDeviceId = kvp.Value.xrInputDeviceId,
                    };
                }
            }

            return s_InvalidMappedXRDevice;
        }

        void RemoveDeviceFromMapping(uint deviceId)
        {
            if (m_MappedDevices.TryGetValue(deviceId, out var mappedDevice))
            {
                m_MappedDevices.Remove(deviceId);
                Logging.Log(LogCategory.Diagnostics, $"DEVICE EVENT Removing controller {mappedDevice.xrInputDeviceId} for device {deviceId}");
                switch (mappedDevice.deviceType)
                {
                    case XRDeviceType.LeftController:
                    case XRDeviceType.RightController:
                    {
                        if (m_ActiveLeftController.xrInputDeviceId == mappedDevice.xrInputDeviceId)
                            m_ActiveLeftController = FindFallbackDevice(mappedDevice.deviceType);
                        if (m_ActiveRightController.xrInputDeviceId == mappedDevice.xrInputDeviceId)
                            m_ActiveRightController = FindFallbackDevice(mappedDevice.deviceType);
                        XrInputSystemNativeApi.RemoveController(mappedDevice.xrInputDeviceId);
                        break;
                    }
                    case XRDeviceType.Hmd:
                    {
                        if (m_ActiveHmd.xrInputDeviceId == mappedDevice.xrInputDeviceId)
                            m_ActiveHmd = FindFallbackDevice(mappedDevice.deviceType);
                        XrInputSystemNativeApi.RemoveHmd(mappedDevice.xrInputDeviceId);
                        break;
                    }
                }

            }
        }

        void Cleanup()
        {
            foreach (var key in m_MappedDevices.Keys.ToArray())
            {
                RemoveDeviceFromMapping(key);
            }

            m_RemovedDevices.Clear();
            m_MappedDevices.Clear();

            // TODO (PLAT-15241): Manually drain the input system of
            // any legacy polyspatial devices created here. This is
            // a workaround for the fact that the XR Input System only
            // disconnects devices, but provides no means for removing them.
            foreach (var device in InputSystem.disconnectedDevices)
            {
                if (device.name.StartsWith(k_LegacyNamePrefix))
                {
                    InputSystem.RemoveDevice(device);
                }
            }

            m_RemovedDevices.Clear();
        }

        /// <summary>
        /// Input tracker needs to be used whether the sim project is using
        /// PolySpatial XR or not. This singleton instance maintains that
        /// one tracker for use.
        /// </summary>
        static Lazy<PolySpatialXrInputTracker> s_Instance = new(() =>
        {
            return new PolySpatialXrInputTracker();
        });

        internal static PolySpatialXrInputTracker Instance => s_Instance.Value;

        internal void Initialize()
        {
            Assert.IsNotNull(PolySpatialInputDeviceManager.Instance);
            m_CanMapDevices = XRInputProvider.EnsureInitialized();
            if (!m_CanMapDevices)
            {
                Logging.LogWarning(LogCategory.Diagnostics, "PolySpatialXrInputTracker cannot map devices because XRInputProvider is not initialized.");
                return;
            }

            PolySpatialInputDeviceManager.Instance.DidAddInputDevice += OnDidAddInputDevice;
            PolySpatialInputDeviceManager.Instance.WillRemoveInputDevice += OnWillRemoveInputDevice;
            PolySpatialInputDeviceManager.Instance.ProcessEvent += OnProcessEvent;

            PolySpatialInputDeviceManager.Instance.IterateMappedDevices(mappedDevice =>
            {
                OnDidAddInputDevice(mappedDevice);
            });
        }

        void OnStateChanged(InputDevice device, InputEventPtr eventPtr)
        {
            if (m_MappedDevices.TryGetValue((uint)device.deviceId, out var mappedDevice))
            {
                OnProcessEvent(eventPtr, device);
            }
        }

        internal uint XRMappedDeviceIdForDeviceId(int deviceId)
        {
            if (m_MappedDevices.TryGetValue((uint)deviceId, out var mappedDevice))
            {
               return mappedDevice.xrInputDeviceId;
            }

            return UInt32.MaxValue;
        }

        void OnDidAddInputDevice(PolySpatialInputDeviceManager.MappedDevice mappedDevice)
        {
            if (mappedDevice.inputDevice == null)
                return;

            if (mappedDevice.inputDevice is not PolySpatialRemoteHmdDevice &&
                mappedDevice.inputDevice is not PolySpatialRemoteControllerDevice)
                return;

            var device = mappedDevice.inputDevice;
            var deviceId = (uint)device.deviceId;
            var leftHand = device.usages.Contains(UnityEngine.InputSystem.CommonUsages.LeftHand);
            var rightHand = device.usages.Contains(UnityEngine.InputSystem.CommonUsages.RightHand);
            var deviceAdded = false;
            if (leftHand || rightHand)
            {
                deviceAdded = TryAddXrInputSystemControllerDeviceId(deviceId, leftHand, rightHand, mappedDevice, device);
            }
            else if (device is PolySpatialRemoteHmdDevice _)
            {
                deviceAdded = TryAddXrInputSystemHmdDeviceId(deviceId, mappedDevice, device);
            }

            if (deviceAdded)
            {
                // Right now, we just always select the last device added to fill
                // a role. In the future, we may want to have a way of switching based
                // on activity or some other criteria.
                if (leftHand)
                {
                    m_ActiveLeftController.remoteDeviceId = deviceId;
                    m_ActiveLeftController.xrInputDeviceId = m_MappedDevices[deviceId].xrInputDeviceId;
                }
                else if (rightHand)
                {
                    m_ActiveRightController.remoteDeviceId = deviceId;
                    m_ActiveRightController.xrInputDeviceId = m_MappedDevices[deviceId].xrInputDeviceId;
                }
                else
                {
                    m_ActiveHmd.remoteDeviceId = deviceId;
                    m_ActiveHmd.xrInputDeviceId = m_MappedDevices[deviceId].xrInputDeviceId;
                }
            }

            m_RemovedDevices.Remove((uint)deviceId);
        }

        void OnWillRemoveInputDevice(PolySpatialInputDeviceManager.MappedDevice mappedDevice)
        {
            if (mappedDevice.inputDevice == null)
                return;
            RemoveDeviceFromMapping((uint)mappedDevice.inputDevice.deviceId);
            m_RemovedDevices.Add((uint)mappedDevice.inputDevice.deviceId);
        }

        void OnProcessEvent(PolySpatialInputDeviceManager.MappedDevice mappedDevice, InputEventPtr eventPtr)
        {
            if (mappedDevice.inputDevice == null)
                return;

            var device = mappedDevice.inputDevice;
            OnProcessEvent(eventPtr, device);
        }

        void OnProcessEvent(InputEventPtr eventPtr, InputDevice device)
        {
            var deviceId = (uint)device.deviceId;

            if (!m_MappedDevices.TryGetValue(deviceId, out var xrMappedDevice))
                return;

            var xrInputDeviceId = xrMappedDevice.xrInputDeviceId;
            if (xrInputDeviceId == UInt32.MaxValue)
                return;

            // Only update the input system and XR Node states for the currently active devices.
            // This is to ensure that we don't have multiples of XR Node States that confuse
            // Tracked Pose Driver.
            if (((xrMappedDevice.deviceType == XRDeviceType.LeftController && m_ActiveLeftController.remoteDeviceId == deviceId)
                 || (xrMappedDevice.deviceType == XRDeviceType.RightController && m_ActiveRightController.remoteDeviceId == deviceId))
                && device is PolySpatialRemoteControllerDevice controller)
            {
                UpdateXrControllerState(xrInputDeviceId, controller, eventPtr);
            }
            else if (xrMappedDevice.deviceType == XRDeviceType.Hmd
                     && m_ActiveHmd.remoteDeviceId == deviceId
                     && device is PolySpatialRemoteHmdDevice remoteHMDDevice)
            {
                UpdateXrHmdState(xrInputDeviceId, remoteHMDDevice, eventPtr);
            }
        }

        static bool IsControlPressed(ButtonControl control, InputEventPtr eventPtr)
        {
            return control.ReadValueFromEvent(eventPtr) > control.pressPointOrDefault;
        }

        static bool HasControlChanged<TValue>(InputControl<TValue> control, InputEventPtr eventPtr) where TValue : struct
        {
            return control.HasValueChangeInEvent(eventPtr);
        }

        /// <summary>
        /// Updates the state of the XR Input System HMD device that is mirroring the PolySpatialRemoteHmdDevice.
        ///
        /// IMPORTANT NOTE: If using the legacy Tracked Pose Driver, you must ensure that there is only one
        /// instance of the PolySpatialRemoteHmdDevice in the scene at a time. Otherwise, the Tracked Pose Driver
        /// may not use the "active" PolySpatialRemoteHmdDevice, and instead use the first one it finds.
        /// </summary>
        /// <param name="xrInputDeviceId">The device id of the device we are going to mirror</param>
        /// <param name="remoteHMDDevice">The actual instance of the device.</param>
        /// <param name="eventPtr">An event ptr to an event from that device that we can use to update data from.</param>
        static void UpdateXrHmdState(uint xrInputDeviceId, PolySpatialRemoteHmdDevice remoteHMDDevice, InputEventPtr eventPtr)
        {
            if (HasControlChanged(remoteHMDDevice.isTracked, eventPtr))
                XrInputSystemNativeApi.SetHmdIsTracked(xrInputDeviceId, IsControlPressed(remoteHMDDevice.isTracked, eventPtr));
            // Tracking State is what is used to tell an interested party that this control
            // is tracking one or more states. So, if your updates are missing position, rotation or some other state change
            // when using XRNodeState, you should ensure that the trackingState is updated.
            if (HasControlChanged(remoteHMDDevice.trackingState, eventPtr))
                XrInputSystemNativeApi.SetHmdTrackingState(xrInputDeviceId, (uint)remoteHMDDevice.trackingState.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.devicePosition, eventPtr))
                XrInputSystemNativeApi.SetHmdPosition(xrInputDeviceId, remoteHMDDevice.devicePosition.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.deviceRotation, eventPtr))
                XrInputSystemNativeApi.SetHmdRotation(xrInputDeviceId, remoteHMDDevice.deviceRotation.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.leftEyePosition, eventPtr))
                XrInputSystemNativeApi.SetLeftEyePosition(xrInputDeviceId, remoteHMDDevice.leftEyePosition.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.leftEyeRotation, eventPtr))
                XrInputSystemNativeApi.SetLeftEyeRotation(xrInputDeviceId, remoteHMDDevice.leftEyeRotation.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.rightEyePosition, eventPtr))
                XrInputSystemNativeApi.SetRightEyePosition(xrInputDeviceId, remoteHMDDevice.rightEyePosition.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.rightEyeRotation, eventPtr))
                XrInputSystemNativeApi.SetRightEyeRotation(xrInputDeviceId, remoteHMDDevice.rightEyeRotation.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.centerEyePosition, eventPtr))
                XrInputSystemNativeApi.SetCenterEyePosition(xrInputDeviceId, remoteHMDDevice.centerEyePosition.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteHMDDevice.centerEyeRotation, eventPtr))
                XrInputSystemNativeApi.SetCenterEyeRotation(xrInputDeviceId, remoteHMDDevice.centerEyeRotation.ReadValueFromEvent(eventPtr));
        }

        /// <summary>
        /// Updates the state of the XR Input System Controller devices that are mirroring the
        /// PolySpatialRemoteControllerDevice.
        ///
        /// IMPORTANT NOTE: If using the legacy Tracked Pose Driver, you must ensure that there is only one
        /// instance of each type of PolySpatialRemoteControllerDevice in the scene at a time. Otherwise,
        /// the Tracked Pose Driver may not use the "active" PolySpatialRemoteControllerDevice, and instead
        /// use the first one it finds.
        /// </summary>
        /// <param name="xrInputDeviceId">The device id of the device we are going to mirror</param>
        /// <param name="remoteControllerDevice">The actual instance of the device.</param>
        /// <param name="eventPtr">An event ptr to an event from that device that we can use to update data from.</param>
        static void UpdateXrControllerState(uint xrInputDeviceId, PolySpatialRemoteControllerDevice remoteControllerDevice, InputEventPtr eventPtr)
        {
            if (HasControlChanged(remoteControllerDevice.isTracked, eventPtr))
                XrInputSystemNativeApi.SetControllerIsTracked(xrInputDeviceId, true);//IsControlPressed(controller.isTracked, eventPtr));
            // Tracking State is what is used to tell an interested party that this control
            // is tracking one or more states. So, if your updates are missing position, rotation or some other state change
            // when using XRNodeState, you should ensure that the trackingState is updated.
            if (HasControlChanged(remoteControllerDevice.trackingState, eventPtr))
                XrInputSystemNativeApi.SetControllerTrackingState(xrInputDeviceId, 3);//(uint)controller.trackingState.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteControllerDevice.devicePosition, eventPtr))
                XrInputSystemNativeApi.SetControllerPosition(xrInputDeviceId, remoteControllerDevice.devicePosition.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteControllerDevice.deviceRotation, eventPtr))
                XrInputSystemNativeApi.SetControllerRotation(xrInputDeviceId, remoteControllerDevice.deviceRotation.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteControllerDevice.primaryButton, eventPtr))
                XrInputSystemNativeApi.SetPrimaryButton(xrInputDeviceId, IsControlPressed(remoteControllerDevice.primaryButton, eventPtr));
            if (HasControlChanged(remoteControllerDevice.primaryTouch, eventPtr))
                XrInputSystemNativeApi.SetPrimaryTouch(xrInputDeviceId, IsControlPressed(remoteControllerDevice.primaryTouch, eventPtr));
            if (HasControlChanged(remoteControllerDevice.secondaryButton, eventPtr))
                XrInputSystemNativeApi.SetSecondaryButton(xrInputDeviceId, IsControlPressed(remoteControllerDevice.secondaryButton, eventPtr));
            if (HasControlChanged(remoteControllerDevice.secondaryTouch, eventPtr))
                XrInputSystemNativeApi.SetSecondaryTouch(xrInputDeviceId,IsControlPressed( remoteControllerDevice.secondaryTouch, eventPtr));
            if (HasControlChanged(remoteControllerDevice.grip, eventPtr))
                XrInputSystemNativeApi.SetGrip(xrInputDeviceId, remoteControllerDevice.grip.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteControllerDevice.trigger, eventPtr))
                XrInputSystemNativeApi.SetTrigger(xrInputDeviceId, remoteControllerDevice.trigger.ReadValueFromEvent(eventPtr));
            if (HasControlChanged(remoteControllerDevice.gripButton, eventPtr))
                XrInputSystemNativeApi.SetGripButton(xrInputDeviceId, IsControlPressed(remoteControllerDevice.gripButton, eventPtr));
            if (HasControlChanged(remoteControllerDevice.triggerButton, eventPtr))
                XrInputSystemNativeApi.SetTriggerButton(xrInputDeviceId, IsControlPressed(remoteControllerDevice.triggerButton, eventPtr));
            if (HasControlChanged(remoteControllerDevice.menuButton, eventPtr))
                XrInputSystemNativeApi.SetMenuButton(xrInputDeviceId, IsControlPressed(remoteControllerDevice.menuButton, eventPtr));
            if (HasControlChanged(remoteControllerDevice.primary2DAxis, eventPtr))
                XrInputSystemNativeApi.SetPrimary2D(xrInputDeviceId, remoteControllerDevice.primary2DAxis.value);
            if (HasControlChanged(remoteControllerDevice.primary2DAxisClick, eventPtr))
                XrInputSystemNativeApi.SetPrimary2DClick(xrInputDeviceId, IsControlPressed(remoteControllerDevice.primary2DAxisClick, eventPtr));
            if (HasControlChanged(remoteControllerDevice.primary2DAxisTouch, eventPtr))
                XrInputSystemNativeApi.SetPrimary2DTouch(xrInputDeviceId, IsControlPressed(remoteControllerDevice.primary2DAxisTouch, eventPtr));
            if (HasControlChanged(remoteControllerDevice.secondary2DAxis, eventPtr))
                XrInputSystemNativeApi.SetSecondary2D(xrInputDeviceId, remoteControllerDevice.secondary2DAxis.value);
            if (HasControlChanged(remoteControllerDevice.secondary2DAxisClick, eventPtr))
                XrInputSystemNativeApi.SetSecondary2DClick(xrInputDeviceId, IsControlPressed(remoteControllerDevice.secondary2DAxisClick, eventPtr));
            if (HasControlChanged(remoteControllerDevice.secondary2DAxisTouch, eventPtr))
                XrInputSystemNativeApi.SetSecondary2DTouch(xrInputDeviceId, IsControlPressed(remoteControllerDevice.secondary2DAxisTouch, eventPtr));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            Cleanup();
        }

        internal void EndConnection()
        {
            Cleanup();
        }
    }
}
#endif
