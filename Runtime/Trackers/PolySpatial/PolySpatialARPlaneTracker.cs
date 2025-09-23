using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityObject = UnityEngine.Object;
using PlaneAlignment = Unity.PolySpatial.Internals.PlaneAlignment;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// Tracks changes from ARPlaneManager objects, and subscribes to ARPlaneManager's planesChanged event,
    /// ARPlane data changes are forwarded to PolySpatial host's connected to the Simulation.
    /// <see cref="Unity.PolySpatial.XR.Internals.Subsystems.PolySpatialXRPlaneSubsystem"/> for detailed description of data flow.
    /// </summary>
    class PolySpatialARPlaneTracker : IDisposable
    {
        bool m_HostConnected;

        PolySpatialHostID m_PolySpatialHostID;

        /// <summary>
        /// This should be initialized with the ARPlaneManager to listen to.
        /// </summary>
        ARPlaneManager m_PlaneManager;

        // Track which planes have been sent through GetChanges
        HashSet<TrackableID> m_SentPlanes = new();

        // This is invoked each frame on `Update` from ARPlaneManager
        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> planeChanges)
        {
            CheckAndSendChanges(planeChanges.added, planeChanges.updated, planeChanges.removed);
        }

        PolySpatialARPlane CreatePolySpatialARPlane(ARPlane plane, ARPlaneOperation operation)
        {
            var planeTransform = plane.transform;
            var planeSubsumedBy = plane.subsumedBy;

            return new()
            {
                operation = operation,
                trackingId = new()
                {
                    subId1 = plane.trackableId.subId1,
                    subId2 = plane.trackableId.subId2
                },
                subsumming = planeSubsumedBy != null,
                subsumedBy = new()
                {
                    subId1 = planeSubsumedBy == null ? TrackableId.invalidId.subId1 : planeSubsumedBy.trackableId.subId1,
                    subId2 = planeSubsumedBy == null ? TrackableId.invalidId.subId2 : planeSubsumedBy.trackableId.subId2
                },
                alignment = (PlaneAlignment)plane.alignment,
                arTrackingState = (ARTrackingState)plane.trackingState,
                arClassification = (uint)plane.classifications,
                center = plane.centerInPlaneSpace,
                position = planeTransform.localPosition,
                rotation = planeTransform.localRotation,
                size = plane.size,
                vertices = plane.boundary
            };
        }

        // The Sim's XRPlaneSybsystem will notify us it wants to Start tracking Planes here.
        internal void Start()
        {
            InitializeARPlanes();
        }

        // The Sim's XRPlaneSybsystem will notify us it wants to Stop tracking Planes here.
        internal void Stop()
        {
            EndConnection();
        }

        internal void SetHostID(PolySpatialHostID hostID)
        {
            m_PolySpatialHostID = hostID;
        }

        internal void InitializeARPlanes()
        {
            var planeManagers = UnityObject.FindObjectsByType<ARPlaneManager>(FindObjectsSortMode.None);

            if (planeManagers.Length == 0)
                return;

            if (planeManagers.Length > 1)
                Logging.LogWarning(LogCategory.XR, "Multiple ARPlaneManagers found in scene, using the first one");

            m_PlaneManager = planeManagers[0];

            if (m_PlaneManager == null)
                return;

            m_PlaneManager.trackablesChanged.AddListener(OnTrackablesChanged);

            var planeEngineData = new PolySpatialARPlaneArray();
            planeEngineData.planes = new List<PolySpatialARPlane>();

            foreach (var plane in m_PlaneManager.trackables)
            {
                var localPlane = CreatePolySpatialARPlane(plane, ARPlaneOperation.Created);
                planeEngineData.planes.Add(localPlane);
                if (localPlane.trackingId.HasValue)
                {
                    m_SentPlanes.Add(localPlane.trackingId.Value);
                }
            }

            // Send this event regardless of there being any planes.
            // There is the possibility of a race condition on a Client connecting with a Host, where before this
            // Init event, the OnPlanesChanged can receive updates from the ARPlaneManager and send updates
            // to the Client. So the Client will wait for the Init event in PolySpatialARPlaneSubsystem.InitializeClient
            // before doing any Add/Update/Remove events (because the ARFoundation doesn't like updating planes that
            // haven't been previously Added, and does other various sanity checks on Add/Update/Remove events)
            HostCommandHelper.InitializeARPlanes(planeEngineData, m_PolySpatialHostID);
            planeEngineData.planes.Clear();

            m_HostConnected = true;
        }

        internal void EndConnection()
        {
            m_SentPlanes.Clear();
            m_HostConnected = false;
            if (m_PlaneManager != null)
                m_PlaneManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        /// <summary>
        /// Update loop for sending ARPlane data to the PolySpatial Host, the ARPlane data gets updated once per frame
        /// from the OnPlanesChanged event (from the <see cref="ARPlaneManager"/> Component).
        /// </summary>
        void CheckAndSendChanges(IReadOnlyCollection<ARPlane> added, IReadOnlyCollection<ARPlane> updated, IReadOnlyCollection<KeyValuePair<TrackableId, ARPlane>> removed)
        {
            if (!m_HostConnected)
                return;

            var planeEngineData = new PolySpatialARPlaneArray();
            planeEngineData.planes = new List<PolySpatialARPlane>();

            foreach (var plane in removed)
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane.Value, ARPlaneOperation.Removed));

            foreach (var plane in added)
            {
                var localPlane = CreatePolySpatialARPlane(plane, ARPlaneOperation.Created);
                planeEngineData.planes.Add(localPlane);
                if (localPlane.trackingId.HasValue)
                    m_SentPlanes.Add(localPlane.trackingId.Value);
            }

            foreach (var plane in updated)
            {
                var trackableID = new TrackableID()
                {
                    subId1 = plane.trackableId.subId1,
                    subId2 = plane.trackableId.subId2
                };
                var operation = m_SentPlanes.Contains(trackableID) ? ARPlaneOperation.Updated : ARPlaneOperation.Created;
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane, operation));
                if (operation == ARPlaneOperation.Created)
                {
                    m_SentPlanes.Add(trackableID);
                }
            }

            if (planeEngineData.planes.Count > 0)
            {
                HostCommandHelper.UpdateARPlanes(planeEngineData, m_PolySpatialHostID);
                planeEngineData.planes.Clear();
            }
        }

        public void Dispose()
        {
            m_SentPlanes.Clear();
            if (m_PlaneManager != null)
                m_PlaneManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }
    }
}
