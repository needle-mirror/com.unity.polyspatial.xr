using System;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    struct PolySpatialImage : IEquatable<PolySpatialImage>, IDisposable
    {
        static readonly PolySpatialImage k_Default = new(
            TrackableId.invalidId,
            new Guid(),
            Pose.identity,
            Vector2.zero,
            TrackingState.None,
            IntPtr.Zero);

        internal static PolySpatialImage defaultValue => k_Default;

        internal XRTrackedImage trackedImage => new(
            m_TrackableId,
            m_SourceImageId,
            m_Pose,
            m_Size,
            m_TrackingState,
            m_NativePtr);

        TrackableId m_TrackableId;
        Guid m_SourceImageId;
        Pose m_Pose;
        Vector2 m_Size;
        TrackingState m_TrackingState;
        IntPtr m_NativePtr;

        internal TrackableId trackableId => m_TrackableId;

        internal Guid guid => m_SourceImageId;

        internal Pose pose => m_Pose;

        internal Vector2 size => m_Size;

        internal TrackingState trackingState => m_TrackingState;

        PolySpatialImage(
            TrackableId trackableId,
            Guid sourceImageId,
            Pose pose,
            Vector2 size,
            TrackingState trackingState,
            IntPtr intPtr)
        {
            m_TrackableId = trackableId;
            m_SourceImageId = sourceImageId;
            m_Pose = pose;
            m_Size = size;
            m_TrackingState = trackingState;
            m_NativePtr = intPtr;
        }

        static TrackableId ToTrackableId(PolySpatialXRTrackableID? trackableId)
        {
            return trackableId == null ? TrackableId.invalidId : new TrackableId(trackableId.Value.subId1, trackableId.Value.subId2);
        }

        internal PolySpatialImage(PolySpatialARTrackedImage arPlaneInfo)
        {
            var serializableGuid = new SerializableGuid(arPlaneInfo.referenceImageGUIDLow, arPlaneInfo.referenceImageGUIDHigh);

            m_TrackableId = ToTrackableId(arPlaneInfo.trackingId);
            m_Pose = new Pose(arPlaneInfo.position ?? default, arPlaneInfo.rotation ?? default);
            m_Size = arPlaneInfo.size.HasValue ? arPlaneInfo.size.Value : default(Vector2);
            m_TrackingState = (TrackingState)arPlaneInfo.arImageTrackingState;
            m_SourceImageId = serializableGuid.guid;
            m_NativePtr = IntPtr.Zero;// TODO lookup from guid?  I don't think PolySpatialXRImageTrackingSubsystem has a native pointer.  Am I wrong?
        }

        // Generates a new string that describes the tracked images's properties, suitable for debugging purposes.
        public override string ToString()
        {
            return string.Format(
                "Plane:\n\ttrackableId: {0}\n\tpose: {1}\n\tsize: {2}\n\ttrackingState: {3}\n\tGuid: {4}",
                trackableId,
                pose,
                size,
                trackingState,
                guid);
        }

        public void Dispose()
        {
            // TODO release managed resources
        }

        public bool Equals(PolySpatialImage other)
        {
            return
                m_TrackableId.Equals(other.m_TrackableId) &&
                m_Pose.Equals(other.m_Pose) &&
                m_Size.Equals(other.m_Size) &&
                m_TrackingState == other.m_TrackingState &&
                m_SourceImageId == other.m_SourceImageId;
        }

        public override bool Equals(object obj) => (obj is PolySpatialImage other) && Equals(other);

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(
                m_Pose.GetHashCode(),
                m_Size.GetHashCode(),
                ((int)m_TrackingState).GetHashCode(),
                m_SourceImageId.GetHashCode()
            );

            return HashCode.Combine(m_TrackableId.GetHashCode(), hashCode);
        }

        public static bool operator ==(PolySpatialImage left, PolySpatialImage right) => left.Equals(right);

        public static bool operator !=(PolySpatialImage left, PolySpatialImage right) => !left.Equals(right);
    }
}
