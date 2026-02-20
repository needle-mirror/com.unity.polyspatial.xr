using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    /// <summary>
    /// The Subsystem which runs on the PolySpatial Client side. It is Initialized by PolySpatialXRLoader (When the
    /// Play To Device XR Plug-In is selected. This class keeps track of all discovered planes from over the network
    /// sent over from the Host and is queried by ARPlaneManager to update Trackables in the scene and to invoke
    /// update events.
    ///
    /// ╔═════════════════════════════════════════╗     ╔════════════════════════════════════════════════════════════╗
    /// ║            PolySpatial HOST             ║     ║                      PolySpatial Client                    ║
    /// ╚═════════════════════════════════════════╝     ╚════════════════════════════════════════════════════════════╝
    ///
    ///           ┌────────────────┐                        ┌─────────────────────────────┐
    ///           │XRPlaneSubsystem│                        │    XRHostCommandHandler     │
    ///           └────────────────┘                        └─────────────────────────────┘
    /// (implemented by ARKit or ARCore, etc.)                           │
    ///                  │                                   (set added, changed, remove)
    ///          (gets plane data)                                       │
    ///                  │                                               V                              (on client side)
    ///                  V                                    ┌───────────────────────────┐             ┌──────────────┐
    ///           ┌──────────────┐                            │PolySpatialXRPlaneSubsystem│             │ARPlaneManager│
    ///           │ARPlaneManager│ (on host side)             └───────────────────────────┘             └──────────────┘
    ///           └──────────────┘                                       │                                  ^
    ///                  │                                               │                                  │
    ///      (invokes planesChanged event)                               │   ARPlaneManager Calls           │
    ///                  │                                               │        GetChanges                │
    ///                  V                                               └───────────────────────────> GetChanges
    ///    ┌─────────────────────────┐
    ///    │PolySpatialARPlaneTracker│
    ///    └─────────────────────────┘
    ///                  │
    ///                  │
    ///                  V
    /// HostCommandHelper.InitARPlaneData
    /// HostCommandHelper.SetARPlaneData
    /// </summary>
    [Preserve]
    class PolySpatialXRPlaneSubsystem : XRPlaneSubsystem
    {
        internal const string k_SubsystemId = "XRPolySpatial-Plane";

        PolySpatialXRPlaneProvider PolySpatialProvider => provider as PolySpatialXRPlaneProvider;

        class PolySpatialXRPlaneProvider : Provider
        {
            public override PlaneDetectionMode currentPlaneDetectionMode => requestedPlaneDetectionMode;

            public override PlaneDetectionMode requestedPlaneDetectionMode { get; set; }

            const int k_InitialPlanesCapacity = 4;

            Dictionary<TrackableId, DiscoveredPlane> m_AddedPlanes = new(k_InitialPlanesCapacity);
            Dictionary<TrackableId, DiscoveredPlane> m_UpdatedPlanes = new(k_InitialPlanesCapacity);
            HashSet<TrackableId> m_RemovedPlanes = new(k_InitialPlanesCapacity);
            Dictionary<TrackableId, DiscoveredPlane> m_AllPlanes = new();

            private PolySpatialHostID m_HostID;

            public override void Destroy()
            {
                CleanUp();
            }

            void CleanUp()
            {
                DiscoveredPlane.DisposeAllVertices();
            }

            public void InitializeClient(PolySpatialHostID hostID)
            {
                m_HostID = hostID;

                Logging.Log(LogCategory.XR, $"Plane Subsystem has initialized client with hostID: {hostID}");
            }

            // We need to make sure there is only 1 update event per frame (Added, updated, or removed)
            // For TryAddPlane, TryUpdatePlane, TryRemovePlane, we will remove or update lists
            // to contain one event per trackableId.
            internal void TryAddPlane(DiscoveredPlane plane)
            {
                m_AddedPlanes[plane.trackableId] = plane;
                m_UpdatedPlanes.Remove(plane.trackableId);
            }

            internal void TryUpdatePlane(DiscoveredPlane plane)
            {
                // Make sure there isn't already a removed event
                if (!m_RemovedPlanes.Contains(plane.trackableId))
                {
                    // If the plane added hasn't been processed yet, update the added values with these values
                    if (!m_AllPlanes.ContainsKey(plane.trackableId))
                    {
                        m_UpdatedPlanes.Remove(plane.trackableId);
                        m_AddedPlanes[plane.trackableId] = plane;
                    }
                    else
                    {
                        m_UpdatedPlanes[plane.trackableId] = plane;
                    }
                }
            }

            internal void TryRemovePlane(DiscoveredPlane plane)
            {
                // make sure the plane has already been previously added before trying to remove it
                if (m_AllPlanes.ContainsKey(plane.trackableId))
                {
                    m_RemovedPlanes.Add(plane.trackableId);
                }

                m_AddedPlanes.Remove(plane.trackableId);
                m_UpdatedPlanes.Remove(plane.trackableId);
            }

            /// <inheritdoc/>
            public override TrackableChanges<BoundedPlane> GetChanges(BoundedPlane defaultPlane, Allocator allocator)
            {
                var addedPlanesCount = m_AddedPlanes.Count;
                var updatedPlanesCount = m_UpdatedPlanes.Count;
                var removedPlanesCount = m_RemovedPlanes.Count;

                var changes = new TrackableChanges<BoundedPlane>(addedPlanesCount, updatedPlanesCount, removedPlanesCount, allocator);

                using (new ScopedProfiler("PolySpatialXRPlaneSubsystem.GetChanges"))
                {
                    if (addedPlanesCount > 0)
                    {
                        var added = changes.added;

                        for (var i = 0; i < addedPlanesCount; i++)
                        {
                            var plane = m_AddedPlanes.ElementAt(i);
                            added[i] = plane.Value.boundedPlane;
                            m_AllPlanes[plane.Key] = plane.Value;
                        }

                        m_AddedPlanes.Clear();
                    }

                    if (updatedPlanesCount > 0)
                    {
                        var updated = changes.updated;

                        for (var i = 0; i < updatedPlanesCount; i++)
                        {
                            var plane = m_UpdatedPlanes.ElementAt(i);
                            updated[i] = plane.Value.boundedPlane;
                            m_AllPlanes[plane.Key] = plane.Value;
                        }

                        m_UpdatedPlanes.Clear();
                    }

                    if (removedPlanesCount > 0)
                    {
                        var removed = changes.removed;

                        for (var i = 0; i < removedPlanesCount; i++)
                        {
                            var planeId = m_RemovedPlanes.ElementAt(i);
                            removed[i] = planeId;
                            m_AllPlanes.Remove(planeId);
                        }
                        m_RemovedPlanes.Clear();
                    }
                }

                return changes;
            }


            public override void Start()
            {
                // Notify P2D Host
                if (PolySpatialRuntime.HasLocalSimulation)
                    PolySpatialCore.UnitySimulation.NextHandler.Command(PolySpatialCommand.XRPlaneSubsystemStart);
            }

            public override void Stop()
            {
                // Notify P2D Host
                if (PolySpatialRuntime.HasLocalSimulation)
                    PolySpatialCore.UnitySimulation.NextHandler.Command(PolySpatialCommand.XRPlaneSubsystemStop);
                CleanUp();
            }

            bool TryFindPlane(TrackableId trackableId, out DiscoveredPlane discoveredPlane)
            {
                if (trackableId == TrackableId.invalidId || !m_AllPlanes.TryGetValue(trackableId, out discoveredPlane))
                {
                    discoveredPlane = DiscoveredPlane.defaultValue;
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Retrieves the boundary points of the plane with <paramref name="trackableId"/>.
            /// </summary>
            /// <param name="trackableId">The id of the plane.</param>
            /// <param name="allocator">An <c>Allocator</c> to use for the returned <c>NativeArray</c>.</param>
            /// <param name="boundary">An existing <c>NativeArray</c> to update or recreate if necessary.
            /// See <see cref="CreateOrResizeNativeArrayIfNecessary{T}(int, Allocator, ref NativeArray{T})"/>.</param>
            public override void GetBoundary(TrackableId trackableId, Allocator allocator, ref NativeArray<Vector2> boundary)
            {
                if (!TryFindPlane(trackableId, out var plane))
                {
                    if (boundary.IsCreated)
                        boundary.Dispose();

                    return;
                }

                var vertices = plane.vertices;
                if (vertices.IsCreated)
                {
                    CreateOrResizeNativeArrayIfNecessary(vertices.Length, allocator, ref boundary);
                    NativeArray<Vector2>.Copy(vertices, boundary);
                }
                else if (boundary.IsCreated)
                {
                    // there are no vertices, so dispose the boundary if it exists to reflect this
                    boundary.Dispose();
                }
            }
        }

        internal void InitializeClient(PolySpatialHostID hostID)
        {
            PolySpatialProvider?.InitializeClient(hostID);
        }

        internal void TryAddPlane(DiscoveredPlane plane)
        {
            PolySpatialProvider?.TryAddPlane(plane);
        }

        internal void TryUpdatePlane(DiscoveredPlane plane)
        {
            PolySpatialProvider?.TryUpdatePlane(plane);
        }

        internal void TryRemovePlane(DiscoveredPlane plane)
        {
            PolySpatialProvider?.TryRemovePlane(plane);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void RegisterDescriptor()
        {
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = k_SubsystemId,
                providerType = typeof(PolySpatialXRPlaneProvider),
                subsystemTypeOverride = typeof(PolySpatialXRPlaneSubsystem),
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = true
            };
            XRPlaneSubsystemDescriptor.Register(cinfo);
        }
    }
}
