using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using UnityEngine.Serialization;
using UnityEngine.XR;
using LegacyMeshId = UnityEngine.XR.MeshId;

namespace Unity.PolySpatial.XR.Internals
{
    /// <summary>
    /// This class should only ever be used in a P2D host app.
    /// This is modeled after ARMeshManager
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    class PolySpatialXRMeshManager : MonoBehaviour
    {
        [SerializeField]
        MeshFilter meshPrefab;

        float m_Density = 0.5f;

        /// <summary>
        /// The density of the generated mesh [0..1]. 1 will be densely tessellated,
        /// while 0 will have the lowest supported tessellation.
        /// </summary>
        public float density
        {
            set
            {
                if (value < 0f || value > 1f)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Mesh density must be between 0 and 1, inclusive.");

                m_Density = value;
                if (m_Subsystem != null)
                    m_Subsystem.meshDensity = m_Density;
            }
        }

        bool m_Normals = true;

        /// <summary>
        /// If `true`, requests a normal for each vertex in generated meshes.
        /// </summary>
        public bool normals
        {
            set => m_Normals = value;
        }

        bool m_Tangents;

        /// <summary>
        /// If `true`, requests a tangent for each vertex in generated meshes.
        /// </summary>
        public bool tangents
        {
            set => m_Tangents = value;
        }

        bool m_TextureCoordinates;

        /// <summary>
        /// If `true`, requests a texture coordinate for each vertex in generated meshes.
        /// </summary>
        public bool textureCoordinates
        {
            set => m_TextureCoordinates = value;
        }

        bool m_Colors;

        /// <summary>
        /// If `true`, requests a color value for each vertex in generated meshes.
        /// </summary>
        public bool colors
        {
            set => m_Colors = value;
        }

        int m_ConcurrentQueueSize = 4;

        static PolySpatialXRMeshManager s_Instance;

        internal static PolySpatialXRMeshManager instance  => s_Instance;

        /// <summary>
        /// The number of meshes to process concurrently. Meshes are processed on a background
        /// thread. Higher numbers will require additional CPU time.
        /// </summary>
        public int concurrentQueueSize
        {
            set => m_ConcurrentQueueSize = value;
        }

        /// <summary>
        /// Invoked whenever meshes have changed (been added, updated, or removed).
        /// </summary>
        internal event Action<PolySpatialXRMeshesChanged> meshesChanged;

        /// <summary>
        /// Destroys all generated meshes and ignores any pending meshes.
        /// </summary>
        public void DestroyAllMeshes()
        {
            m_Pending.Clear();
            m_Generating.Clear();
            foreach (var meshFilter in m_Meshes.Values)
            {
                if (meshFilter != null)
                    Destroy(meshFilter.gameObject);
            }
            m_Meshes.Clear();
        }

        // This is similar to GetComponentInParent but also considers inactive GameObjects, while GetComponentInParent
        // ignores GameObjects that are not activeInHierarchy.
        T GetComponentInParentIncludingInactive<T>() where T : Component
        {
            var parent = transform.parent;
            while (parent)
            {
                var component = parent.GetComponent<T>();
                if (component)
                    return component;

                parent = parent.parent;
            }

            return null;
        }

        XROrigin GetXROrigin() => GetComponentInParentIncludingInactive<XROrigin>();

        void SetBoundingVolume()
        {
            m_Subsystem.SetBoundingVolume(transform.localPosition, transform.localScale);
            transform.hasChanged = false;
        }

        void OnEnable()
        {
            if (GetXROrigin() == null)
            {
                enabled = false;
                throw new InvalidOperationException($"An {nameof(PolySpatialXRMeshManager)} must be a child of an {nameof(XROrigin)}.");
            }

            m_Subsystem ??= GetActiveSubsystemInstance();

            if (m_Subsystem != null)
            {
                m_Subsystem.meshDensity = m_Density;
                SetBoundingVolume();
                m_Subsystem.Start();
            }
            else
            {
                enabled = false;
            }
        }

        static XRMeshSubsystem GetActiveSubsystemInstance()
        {
            XRMeshSubsystem activeSubsystem = null;

            // Query the currently active loader for the created subsystem, if one exists.
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var loader = XRGeneralSettings.Instance.Manager.activeLoader;
                if (loader != null)
                {
                    activeSubsystem = loader.GetLoadedSubsystem<XRMeshSubsystem>();
                }
            }

            if (activeSubsystem == null)
            {
                Debug.LogWarning($"No active {typeof(XRMeshSubsystem).FullName} is available. Please ensure that a valid loader configuration exists in the XR project settings and that meshing is supported.");
            }

            return activeSubsystem;
        }

        void UpdateNormals()
        {
            // If normals were requested, compute the normals before invoking meshesChanged
            if (m_Normals)
            {
                foreach (var meshFilter in m_Added)
                {
                    var mesh = (meshFilter.sharedMesh != null) ? meshFilter.sharedMesh : meshFilter.mesh;

                    // Calculate normals if they weren't populated by the provider.
                    if (mesh.normals.Length == 0)
                        mesh.RecalculateNormals();
                }

                foreach (var meshFilter in m_Updated)
                {
                    var mesh = (meshFilter.sharedMesh != null) ? meshFilter.sharedMesh : meshFilter.mesh;

                    // Calculate normals if they weren't populated by the provider.
                    if (mesh.normals.Length == 0)
                        mesh.RecalculateNormals();
                }
            }
        }

        void SendUpdatesToTrackers()
        {
            var changeTracker = new PolySpatialXRMeshesChanged();
            changeTracker.addOrUpdatedArray = new List<PolySpatialXRMesh>();
            var combinedList = m_Added.Concat(m_Updated);
            foreach (var meshFilter in combinedList)
            {
                var meshId = m_MeshIDLookUp[meshFilter];
                var meshIdString = meshId.ToString();
                var (subId1, subId2) = ExtractSubIds(meshIdString);
                var trans = meshFilter.transform;
                var mesh = (meshFilter.sharedMesh != null) ? meshFilter.sharedMesh : meshFilter.mesh;
                var shortIndices = mesh.vertexCount < 65536;
                var indexes16List = new List<ushort>();
                var indexes32List = new List<int>();
                if (shortIndices)
                {
                    mesh.GetIndices(indexes16List, 0);
                }
                else
                {
                    mesh.GetIndices(indexes32List, 0);
                }

                changeTracker.addOrUpdatedArray.Add( new PolySpatialXRMesh()
                {
                    changeState = PolySpatialMeshChangeState.Added,
                    meshID = new TrackableID()
                    {
                        subId1 = subId1,
                        subId2 = subId2
                    },
                    position = trans.localPosition,
                    rotation = trans.localRotation,
                    scale = trans.localScale,
                    numVertices =  mesh.vertexCount,
                    vertices = new NativeArray<Vector3>(mesh.vertices.ToArray(), Allocator.Persistent),
                    normals = new NativeArray<Vector3>(mesh.normals.ToArray(), Allocator.Persistent),
                    numTriangles = mesh.triangles.Length / 3,
                    shortIndices = shortIndices,
                    indices16 = shortIndices ? new NativeArray<ushort>(indexes16List.ToArray(), Allocator.Persistent) : null,
                    indices32 = !shortIndices ? new NativeArray<int>(indexes32List.ToArray(), Allocator.Persistent) : null,
                });
            }

            changeTracker.removedArray = new List<TrackableID>();
            foreach (var meshId in m_RemovedMeshIds)
            {
                var meshIdString = meshId.ToString();
                var (subId1, subId2) = ExtractSubIds(meshIdString);
                changeTracker.removedArray.Add( new TrackableID()
                {
                    subId1 = subId1,
                    subId2 = subId2
                });
            }

            meshesChanged?.Invoke(changeTracker);
        }

        void Update()
        {
            if (m_Subsystem != null && m_Subsystem.running)
            {
                if (transform.hasChanged)
                    SetBoundingVolume();

                UpdateMeshInfos();

                Generate();
            }

            // Until a host connects and the tracker registers don't send data.
            if (meshesChanged != null)
            {
                // Invoke user callbacks
                try
                {
                    if (m_Added.Count + m_Updated.Count + m_Removed.Count > 0)
                    {
                        UpdateNormals();

                        SendUpdatesToTrackers();
                    }
                }
                finally
                {
                    // Make sure we clear the internal lists if user code throws an exception
                    m_Added.Clear();
                    m_Updated.Clear();

                    foreach (var meshFilter in m_Removed)
                    {
                        if (meshFilter != null)
                            Destroy(meshFilter.gameObject);
                    }

                    m_Removed.Clear();

                    m_MeshIDLookUp.Clear();
                    m_RemovedMeshIds.Clear();
                }
            }
        }

        static (ulong, ulong) ExtractSubIds(string meshIdString)
        {
            var parts = meshIdString.Split('-');

            if (parts.Length != 2)
                throw new InvalidOperationException("Invalid format");

            var subId1 = ulong.Parse(parts[0], System.Globalization.NumberStyles.HexNumber);
            var subId2 = ulong.Parse(parts[1], System.Globalization.NumberStyles.HexNumber);

            return (subId1, subId2);
        }

        void Generate()
        {
            var vertexAttributes = MeshVertexAttributes.None;
            if (m_Normals)
                vertexAttributes |= MeshVertexAttributes.Normals;
            if (m_Tangents)
                vertexAttributes |= MeshVertexAttributes.Tangents;
            if (m_TextureCoordinates)
                vertexAttributes |= MeshVertexAttributes.UVs;
            if (m_Colors)
                vertexAttributes |= MeshVertexAttributes.Colors;

            while ((m_Generating.Count < m_ConcurrentQueueSize) &&
                   m_Pending.TryDequeue(m_Generating, out var meshInfo))
            {
                var meshId = meshInfo.MeshId;
                var meshFilter = GetOrCreateMeshFilter(GetTrackableId(meshId));
                var meshCollider = meshFilter.GetComponent<MeshCollider>();
                var mesh = (meshFilter.sharedMesh != null) ? meshFilter.sharedMesh : meshFilter.mesh;

                m_Generating.Add(meshId, meshInfo);
                m_Subsystem.GenerateMeshAsync(
                    meshInfo.MeshId,
                    mesh,
                    meshCollider,
                    vertexAttributes,
                    m_OnMeshGeneratedDelegate,
                    MeshGenerationOptions.ConsumeTransform
                    );
            }
        }

        void OnMeshGenerated(MeshGenerationResult result)
        {
            if (!m_Generating.TryGetValue(result.MeshId, out var meshInfo))
                return;

            m_Generating.Remove(result.MeshId);

            if (result.Status != MeshGenerationStatus.Success)
                return;

            var meshTransform = GetOrUpdateMeshTransform(new MeshTransform(result.MeshId, result.Timestamp, result.Position, result.Rotation, result.Scale));

            if (!m_Meshes.TryGetValue(GetTrackableId(result.MeshId), out var meshFilter) || (meshFilter == null))
                return;

            SetMeshTransform(meshFilter.transform, meshTransform);

            //meshFilter.gameObject.SetActive(true);

            m_MeshIDLookUp[meshFilter] = result.MeshId;

            switch (meshInfo.ChangeState)
            {
                case MeshChangeState.Added:
                    m_Added.Add(meshFilter);
                    break;
                case MeshChangeState.Updated:
                    m_Updated.Add(meshFilter);
                    break;
                // Removed/unchanged meshes don't get generated.
            }
        }

        MeshTransform GetOrUpdateMeshTransform(MeshTransform meshTransform)
        {
            if (m_Transforms.TryGetValue(meshTransform.MeshId, out var currentTransform) && currentTransform.Timestamp > meshTransform.Timestamp)
                return currentTransform;

            m_Transforms[currentTransform.MeshId] = meshTransform;
            return meshTransform;
        }

        static void SetMeshTransform(Transform transform, in MeshTransform meshTransform)
        {
            transform.localPosition = meshTransform.Position;
            transform.localRotation = meshTransform.Rotation;
            transform.localScale = meshTransform.Scale;
        }

        void UpdateMeshInfos()
        {
            s_MeshInfos.Clear();
            if (m_Subsystem.TryGetMeshInfos(s_MeshInfos))
            {
                foreach (var meshInfo in s_MeshInfos)
                {
                    switch (meshInfo.ChangeState)
                    {
                        case MeshChangeState.Added:
                        case MeshChangeState.Updated:
                            m_Pending.EnqueueUnique(meshInfo);
                            break;

                        case MeshChangeState.Removed:
                            // Remove from processing queues
                            m_Pending.Remove(meshInfo.MeshId);
                            m_Generating.Remove(meshInfo.MeshId);
                            m_Transforms.Remove(meshInfo.MeshId);

                            // Add to list of removed meshes
                            var trackableId = GetTrackableId(meshInfo.MeshId);
                            if (m_Meshes.TryGetValue(trackableId, out var meshFilter))
                            {
                                m_Meshes.Remove(trackableId);
                                if (meshFilter != null)
                                {
                                    m_Removed.Add(meshFilter);
                                    m_RemovedMeshIds.Add(meshInfo.MeshId);
                                }
                            }

                            break;
                    }
                }
            }

            using var meshTransforms = m_Subsystem.GetUpdatedMeshTransforms(Allocator.Temp);
            foreach (var newMeshTransform in meshTransforms)
            {
                var meshTransform = GetOrUpdateMeshTransform(newMeshTransform);
                if (m_Meshes.TryGetValue(GetTrackableId(meshTransform.MeshId), out var filter) && filter != null)
                {
                    SetMeshTransform(filter.transform, meshTransform);
                }
            }
        }

        void OnDisable()
        {
            if (m_Subsystem != null && m_Subsystem.running)
                m_Subsystem.Stop();
        }

        void OnDestroy() => m_Subsystem = null;

        MeshFilter GetOrCreateMeshFilter(TrackableId trackableId)
        {
            // If the mesh filter is Destroyed by user code, then meshFilter will compare
            // equal with null. In that case, we want to recreate it.
            if (m_Meshes.TryGetValue(trackableId, out var meshFilter) && (meshFilter != null))
                return meshFilter;

            var origin = GetXROrigin();
            meshFilter = (origin == null) ?
                Instantiate(meshPrefab) :
                Instantiate(meshPrefab, origin.TrackablesParent);

            meshFilter.gameObject.name = $"Mesh {trackableId.ToString()}";

            // The GameObject should start life inactive until we've populated it
            meshFilter.gameObject.SetActive(false);

            m_Meshes[trackableId] = meshFilter;

            return meshFilter;
        }

        static unsafe TrackableId GetTrackableId(LegacyMeshId trackableId)
        {
            return *(TrackableId*)&trackableId;
        }

        internal static unsafe LegacyMeshId GetLegacyMeshId(TrackableId trackableId)
        {
            return *(LegacyMeshId*)&trackableId;
        }

        void Awake()
        {
            m_Added = new List<MeshFilter>();
            m_Updated = new List<MeshFilter>();
            m_Removed = new List<MeshFilter>();
            m_Pending = new MeshQueue();
            m_Generating = new Dictionary<LegacyMeshId, MeshInfo>();
            m_Meshes = new SortedList<TrackableId, MeshFilter>(s_TrackableIdComparer);
            m_MeshIDLookUp = new Dictionary<MeshFilter, MeshId>();
            m_RemovedMeshIds = new List<MeshId>();
            m_OnMeshGeneratedDelegate = OnMeshGenerated;

            s_Instance = this;
        }

        class TrackableIdComparer : IComparer<TrackableId>
        {
            public int Compare(TrackableId trackableIdA, TrackableId trackableIdB) => trackableIdA.subId1 == trackableIdB.subId1
                ? trackableIdA.subId2.CompareTo(trackableIdB.subId2)
                : trackableIdA.subId1.CompareTo(trackableIdB.subId1);
        }

        List<MeshFilter> m_Added;

        List<MeshFilter> m_Updated;

        List<MeshFilter> m_Removed;

        List<MeshId> m_RemovedMeshIds;

        Dictionary<MeshFilter, MeshId> m_MeshIDLookUp;

        MeshQueue m_Pending;

        Dictionary<LegacyMeshId, MeshInfo> m_Generating;

        SortedList<TrackableId, MeshFilter> m_Meshes;

        Dictionary<MeshId, MeshTransform> m_Transforms = new ();

        Action<MeshGenerationResult> m_OnMeshGeneratedDelegate;

        XRMeshSubsystem m_Subsystem;

        static TrackableIdComparer s_TrackableIdComparer = new ();

        static List<MeshInfo> s_MeshInfos = new ();
    }
}
