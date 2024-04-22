using FlatSharp.Runtime.Extensions;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
    class XRHostCommandHandler : IPolySpatialHostCommandHandler, IDisposable
    {
        PolySpatialXRPlaneSubsystem m_PolySpatialXRPlaneSubsystem;

        List<PolySpatialXRPlaneSubsystem> m_PlaneSubsystems = new (1);

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

        public void Initialize()
        {
            PolySpatialCore.HostMulticastHandler?.AddHandler(this);
        }

        public void Dispose()
        {
            PolySpatialCore.HostMulticastHandler?.RemoveHandler(this);

#if INCLUDE_UNITY_XR_HANDS
            DisposeHandSubsystems();
#endif
        }

        public unsafe void HandleHostCommand(PolySpatialHostCommand cmd, int argCount, void** argValues, int* argSizes)
        {
            switch (cmd)
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
                    var updatedHandLayout = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<bool>(argValues[1], length, Allocator.None);

                    UpdateHandLayout(updatedHandLayout);
                    break;
                }
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

            var operation = arPlaneInfo.operation;

            var planeData = new DiscoveredPlane(arPlaneInfo);

            switch (operation)
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
                default:
                    Logging.LogError(LogCategory.XR, "Supported ARPlane operation.");
                    break;
            }
        }

#if INCLUDE_UNITY_XR_HANDS
        List<PolySpatialHandSubsystem> m_HandSubsystems;

        void InitHandSubsystems()
        {
            // Here we assume that unless someone is managing XR manually, subsystems are only ever loaded at the beginning.
            // We are not expecting the set of subsystems to change at runtime.
            m_HandSubsystems = ListPool<PolySpatialHandSubsystem>.Get();
            SubsystemManager.GetInstances(m_HandSubsystems);
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
#if INCLUDE_UNITY_XR_HANDS
            if (m_HandSubsystems == null)
                InitHandSubsystems();

            // ReSharper disable once PossibleNullReferenceException
            foreach (var subsystem in m_HandSubsystems)
                subsystem.OnTrackingEvent(handId, evt, hostID);
#endif
        }

        void SetXRHandData(PolySpatialXRHandData handData, PolySpatialHostID hostID)
        {
#if INCLUDE_UNITY_XR_HANDS
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
#if INCLUDE_UNITY_XR_HANDS
            if (m_HandSubsystems == null)
                InitHandSubsystems();

            // ReSharper disable once PossibleNullReferenceException
            foreach (var subsystem in m_HandSubsystems)
            {
                subsystem.UpdateHandLayout(updatedHandLayout);
            }
#endif
        }
    }
}
