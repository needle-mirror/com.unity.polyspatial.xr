using System;
using System.Collections.Generic;
using Unity.Collections;
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

        // This is invoked each frame on `Update` from ARPlaneManager
        void OnPlanesChanged(ARPlanesChangedEventArgs planeChanges)
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
                arClassification = (ARPlaneClassification)plane.classification,
                center = plane.centerInPlaneSpace,
                position = planeTransform.localPosition,
                rotation = planeTransform.localRotation,
                size = plane.size,
                vertices = plane.boundary
            };
        }

        public void InitializeARPlanes(PolySpatialHostID hostID)
        {
            var planeManagers = UnityObject.FindObjectsByType<ARPlaneManager>(FindObjectsSortMode.None);

            if (planeManagers.Length == 0)
                return;

            if (planeManagers.Length > 1)
                Logging.LogWarning(LogCategory.XR, "Multiple ARPlaneManagers found in scene, using the first one");

            m_PlaneManager = planeManagers[0];

            if (m_PlaneManager == null)
                return;

            m_PlaneManager.planesChanged += OnPlanesChanged;

            PolySpatialARPlaneArray planeEngineData = new ();
            planeEngineData.planes = new List<PolySpatialARPlane>();

            m_PolySpatialHostID = hostID;

            foreach (var plane in m_PlaneManager.trackables)
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane, ARPlaneOperation.Created));

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

        public void EndConnection()
        {
            m_HostConnected = false;
            if (m_PlaneManager != null)
                m_PlaneManager.planesChanged -= OnPlanesChanged;

        }

        /// <summary>
        /// Update loop for sending ARPlane data to the PolySpatial Host, the ARPlane data gets updated once per frame
        /// from the OnPlanesChanged event (from the <see cref="ARPlaneManager"/> Component).
        /// </summary>
        void CheckAndSendChanges(List<ARPlane> added, List<ARPlane> updated, List<ARPlane> removed)
        {
            if (!m_HostConnected)
                return;

            PolySpatialARPlaneArray planeEngineData = new ();
            planeEngineData.planes = new List<PolySpatialARPlane>();

            foreach (var plane in removed)
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane, ARPlaneOperation.Removed));

            foreach (var plane in added)
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane, ARPlaneOperation.Created));

            foreach (var plane in updated)
                planeEngineData.planes.Add(CreatePolySpatialARPlane(plane, ARPlaneOperation.Updated));

            if (planeEngineData.planes.Count > 0)
            {
                HostCommandHelper.UpdateARPlanes(planeEngineData, m_PolySpatialHostID);
                planeEngineData.planes.Clear();
            }
        }

        public void Dispose()
        {
            if (m_PlaneManager != null)
                m_PlaneManager.planesChanged -= OnPlanesChanged;
        }
    }
}
