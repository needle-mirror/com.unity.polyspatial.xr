// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
#define USE_XR_INPUT
#endif

using FlatSharp.Runtime.Extensions;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.XR.Internals.Subsystems;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Host Command handler, this should receive ARPlane data from a connected host,
    /// as well Hand data, mesh data, etc. and interfacing with the PolySpatial XR Plug-in to therefore provide ARFoundation with this data.
    /// </summary>
    class XRHostCommandHandler : IPolySpatialChainableHostCommandHandler, IDisposable
    {
        public IPolySpatialHostCommandHandler NextHostHandler { get; set; }

        PolySpatialXRPlaneSubsystem m_PolySpatialXRPlaneSubsystem;
        List<PolySpatialXRPlaneSubsystem> m_PlaneSubsystems = new (1);

        PolySpatialXRImageTrackingSubsystem m_PolySpatialXRImageSubsystem;
        List<PolySpatialXRImageTrackingSubsystem> m_ImageTrackingSubsystems = new (1);

        PolySpatialXRPlaneSubsystem PolySpatialXRPlaneSubsystem
        {
            get
            {
                if (m_PolySpatialXRPlaneSubsystem == null)
                {
                    m_PlaneSubsystems.Clear();
                    SubsystemManager.GetSubsystems(m_PlaneSubsystems);

                    if (m_PlaneSubsystems.Count == 0)
                        return null;

                    m_PolySpatialXRPlaneSubsystem = m_PlaneSubsystems[0];
                }

                return m_PolySpatialXRPlaneSubsystem;
            }
        }

        PolySpatialXRImageTrackingSubsystem PolySpatialXRImageTrackingSubsystem
        {
            get
            {
                if (m_PolySpatialXRImageSubsystem == null)
                {
                    m_ImageTrackingSubsystems.Clear();
                    SubsystemManager.GetSubsystems(m_ImageTrackingSubsystems);

                    if (m_ImageTrackingSubsystems.Count == 0)
                        return null;

                    m_PolySpatialXRImageSubsystem = m_ImageTrackingSubsystems[0];
                }

                return m_PolySpatialXRImageSubsystem;
            }
        }

        PolySpatialXRDisplaySubsystemProcessor m_PolySpatialXRDisplaySubsystemProcessor;

        PolySpatialXRDisplaySubsystemProcessor PolySpatialXRDisplaySubsystemProcessor
        {
            get
            {
                if (m_PolySpatialXRDisplaySubsystemProcessor == null)
                    m_PolySpatialXRDisplaySubsystemProcessor = new();

                return m_PolySpatialXRDisplaySubsystemProcessor;
            }
        }

        internal XRHostCommandHandler()
        {
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
            PolySpatialXrInputTracker.Instance?.Initialize();
#endif
        }

        public void Dispose()
        {
#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS
            DisposeHandSubsystems();
#endif
        }

        public unsafe void HandleHostCommand(PolySpatialHostCommandHeader cmdHeader, int argCount, void** argValues, int* argSizes)
        {
            switch (cmdHeader.HostCommand)
            {
                case PolySpatialHostCommand.InitializeARPlanes:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        InitARPlaneData(*hostID);

                        var planes = PolySpatialARPlaneArray.Serializer.Parse(data.Length, p);
                        foreach (var plane in planes.planes)
                            SetARPlaneData(plane);
                    }

                    break;
                }
                case PolySpatialHostCommand.UpdateARPlanes:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var planes = PolySpatialARPlaneArray.Serializer.Parse(data.Length, p);
                        foreach (var plane in planes.planes)
                            SetARPlaneData(plane);
                    }

                    break;
                }
                case PolySpatialHostCommand.UpdateARTrackedImage:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var images = PolySpatialARTrackedImageArray.Serializer.Parse(data.Length, p);
                        foreach (var image in images.images)
                            SetARTrackedImageData(image);
                    }

                    break;
                }
                case PolySpatialHostCommand.OnXRHandTrackingEvent:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out PolySpatialHandID* handId,
                        out PolySpatialXRHandTrackingEvent* evt);
                    OnXRHandTrackingEvent(*handId, *evt, *hostID);
                    break;
                }
                case PolySpatialHostCommand.SetXRHandData:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out Span<byte> data);

                    fixed (byte* p = data)
                    {
                        var handData = PolySpatialXRHandData.Serializer.Parse(data.Length, p, FlatSharp.FlatBufferDeserializationOption.GreedyMutable);
                        SetXRHandData(handData, *hostID);
                    }

                    break;
                }
                case PolySpatialHostCommand.UpdateHandLayout:
                {
                    Debug.Assert(argCount == 2);

                    var length = argSizes[1] / UnsafeUtility.SizeOf<bool>();
                    var updatedHandLayout = PolySpatialUtils.GetTempNativeArrayForBuffer<bool>(argValues[1], length);

                    UpdateHandLayout(updatedHandLayout);
                    break;
                }
                case PolySpatialHostCommand.SendXRMeshData:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialHostID* hostID, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var meshData = PolySpatialXRMeshesChanged.Serializer.Parse(data.Length, p);
                        SendXRMeshData(meshData);
                    }

                    break;
                }
                case PolySpatialHostCommand.SetXRDisplayData:
                {
                    PolySpatialArgs.ExtractArgs(argCount, argValues, argSizes, out PolySpatialInstanceID* id, out Span<byte> data);
                    fixed (byte* p = data)
                    {
                        var displayData = PolySpatialXRDisplayData.Serializer.Parse(data.Length, p);
                        SetXRDisplayData(displayData, data, *id);
                    }
                    break;
                }

                // InputCommandCategory
                case PolySpatialHostCommand.InputEvent:
                {
                    PolySpatialArgs.ExtractInputEventArgs(argCount, argValues, argSizes,
                        out var type, out var hostId, out var eventCount, out var events, out var stride);
                    OnInputEvent(*type, *hostId, eventCount, events, stride);
                    break;
                }
            }

            NextHostHandler.HandleHostCommand(cmdHeader, argCount, argValues, argSizes);
        }

        static unsafe void OnInputEvent(PolySpatialInputType type, PolySpatialHostID hostId, int eventCount, void* eventsPtr, int stride)
        {
            switch (type)
            {
                case PolySpatialInputType.HeadPose:
                    // If we are tracking an HMD, we don't need to process head pose events.
#if UNITY_EDITOR || ENABLE_XR_INPUT_REMOTING
                    if (PolySpatialXrInputTracker.Instance != null && PolySpatialXrInputTracker.Instance.IsTrackingHmd)
                        return;
#endif
                    var poseEvents = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<PolySpatialHeadPoseEvent>(
                        eventsPtr,
                        eventCount,
                        Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref poseEvents, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif

                    PolySpatialXRHMDEventListener.OnHeadPoseEvent(hostId, poseEvents);
                    break;
            }
        }

        [NonSerialized]
        PolySpatialXRCore m_Core;

        PolySpatialXRCore Core
        {
            get
            {
                if (m_Core == null)
                {
                    m_Core = (PolySpatialXRCore)PolySpatialCore.Instance.GetSubsystemById(PolySpatialXRCore.k_SubsystemId);
                    Debug.Assert(m_Core != null);
                    if (m_Core.m_ARSessionData == null)
                    {
                        m_Core.InitializeARData();
                    }
                    XRInputProvider.EnsureInitialized();
                }

                return m_Core;
            }
        }

        void InitARPlaneData(PolySpatialHostID hostID)
        {
            if (PolySpatialXRPlaneSubsystem == null)
                return;

            PolySpatialXRPlaneSubsystem.InitializeClient(hostID);
        }

        void SetARPlaneData(PolySpatialARPlane arPlaneInfo)
        {
            if (PolySpatialXRPlaneSubsystem == null)
                return;

            var planeData = new DiscoveredPlane(arPlaneInfo);

            switch (arPlaneInfo.operation)
            {
                case ARPlaneOperation.Removed:
                    PolySpatialXRPlaneSubsystem.TryRemovePlane(planeData);
                    break;
                case ARPlaneOperation.Updated:
                    PolySpatialXRPlaneSubsystem.TryUpdatePlane(planeData);
                    break;
                case ARPlaneOperation.Created:
                    PolySpatialXRPlaneSubsystem.TryAddPlane(planeData);
                    break;
            }
        }

        void SetARTrackedImageData(PolySpatialARTrackedImage trackedImage)
        {
            if (PolySpatialXRImageTrackingSubsystem == null)
                return;

            var imageData = new PolySpatialImage(trackedImage);

            switch (trackedImage.operation)
            {
                case ARTrackedImageOperation.Removed:
                    PolySpatialXRImageTrackingSubsystem.TryRemoveARTrackedImage(imageData);
                    break;
                case ARTrackedImageOperation.Updated:
                    PolySpatialXRImageTrackingSubsystem.TryUpdateARTrackedImage(imageData);
                    break;
                case ARTrackedImageOperation.Created:
                    PolySpatialXRImageTrackingSubsystem.TryAddARTrackedImage(imageData);
                    break;
            }
        }

        void SendXRMeshData(PolySpatialXRMeshesChanged meshData)
        {
            // Will be null while ending Play.  We want to discard any incoming mesh Data while we are exiting Play Mode.
            PolySpatialXRMeshSubsystemProcessor.instance?.ProcessMeshUpdates(meshData);
        }

#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS
        List<PolySpatialHandSubsystem> m_HandSubsystems;

        void InitHandSubsystems()
        {
            // Here we assume that unless someone is managing XR manually, subsystems are only ever loaded at the beginning.
            // We are not expecting the set of subsystems to change at runtime.
            m_HandSubsystems = ListPool<PolySpatialHandSubsystem>.Get();
            SubsystemManager.GetSubsystems(m_HandSubsystems);
        }

        void DisposeHandSubsystems()
        {
            if (m_HandSubsystems != null)
            {
                ListPool<PolySpatialHandSubsystem>.Release(m_HandSubsystems);
                m_HandSubsystems = null;
            }
        }
#endif

        void OnXRHandTrackingEvent(PolySpatialHandID handId, PolySpatialXRHandTrackingEvent evt, PolySpatialHostID hostID)
        {
#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS
            if (m_HandSubsystems == null)
                InitHandSubsystems();

            // ReSharper disable once PossibleNullReferenceException
            foreach (var subsystem in m_HandSubsystems)
                subsystem.OnTrackingEvent(handId, evt, hostID);
#endif
        }

        void SetXRHandData(PolySpatialXRHandData handData, PolySpatialHostID hostID)
        {
#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS
            if (m_HandSubsystems == null)
                InitHandSubsystems();

            // ReSharper disable once PossibleNullReferenceException
            foreach (var subsystem in m_HandSubsystems)
            {
                subsystem.SetHandData(handData, hostID);
            }
#endif
        }

        void UpdateHandLayout(NativeArray<bool> updatedHandLayout)
        {
#if USE_XR_INPUT && INCLUDE_UNITY_XR_HANDS
            if (m_HandSubsystems == null)
                InitHandSubsystems();

            // ReSharper disable once PossibleNullReferenceException
            foreach (var subsystem in m_HandSubsystems)
            {
                subsystem.UpdateHandLayout(updatedHandLayout);
            }
#endif
        }

        void SetXRDisplayData(PolySpatialXRDisplayData displayData, Span<byte> rawData, PolySpatialInstanceID id)
        {
            PolySpatialXRDisplaySubsystemProcessor.SetData(displayData, rawData);
        }

        [CommandHandlerCreationCallback(stage: CommandHandlerGraph.Stage.HostConnectionManager)]
        static XRHostCommandHandler Create(CommandHandlerGraph.HandlerCreationContext context)
        {
            return new XRHostCommandHandler();
        }
    }
}
