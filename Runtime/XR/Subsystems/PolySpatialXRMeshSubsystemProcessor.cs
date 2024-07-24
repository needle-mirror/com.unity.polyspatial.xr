using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    /// <summary>
    /// The Subsystem which runs on the PolySpatial Client side. It is Initialized by PolySpatialXRLoader (When the
    /// Play To Device XR Plug-In is selected. This class keeps track of all discovered meshes from over the network
    /// sent over from the Host and is queried by ARMeshManager ...
    ///
    /// ╔═════════════════════════════════════════╗     ╔════════════════════════════════════════════════════════════╗
    /// ║            PolySpatial HOST             ║     ║                      PolySpatial Client                    ║
    /// ╚═════════════════════════════════════════╝     ╚════════════════════════════════════════════════════════════╝
    ///
    ///           ┌───────────────┐                        ┌────────────────────────────┐
    ///           │XRMeshSubsystem│                        │    XRHostCommandHandler    │
    ///           └───────────────┘                        └────────────────────────────┘
    /// (implemented by ARKit or ARCore, etc.)                           │
    ///                  │                                   (set added, changed, remove)
    ///          (gets mesh data)                                        │
    ///                  │                                               V                                (on client side)
    ///                  V                                 ┌───────────────────────────────────┐           ┌─────────────┐
    ///     ┌────────────────────────┐                     │PolySpatialXRMeshSubsystemProcessor│           │ARMeshManager│
    ///     │PolySpatialXRMeshManager│ (on host side)      └───────────────────────────────────┘           └─────────────┘
    ///     └────────────────────────┘                                   │                                        ^
    ///                  │                                               │                                        │
    ///      (invokes meshesChanged event)                               │   ARMeshManager Calls                  │
    ///                  │                                               │     TryGetMeshInfos                    │
    ///                  V                                               └───────────────────────────────> TryGetMeshInfos
    ///      ┌────────────────────────┐
    ///      │PolySpatialARMeshTracker│
    ///      └────────────────────────┘
    ///                  │
    ///                  │
    ///                  V
    /// HostCommandHelper.SendARMeshData
    /// </summary>
    class PolySpatialXRMeshSubsystemProcessor : IDisposable
    {
        internal const string k_SubsystemId = "PolySpatialXR-Meshing";

        readonly Dictionary<TrackableId, IDisposable[]> m_NativeArrays = new();

        XRMeshSubsystem m_Subsystem;

        static PolySpatialXRMeshSubsystemProcessor s_Instance;

        internal static PolySpatialXRMeshSubsystemProcessor instance => s_Instance;

        static XRMeshSubsystem GetActiveSubsystemInstance()
        {
            XRMeshSubsystem activeSubsystem = null;

            // Query the currently active loader for the created subsystem, if one exists.
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var loader = XRGeneralSettings.Instance.Manager.activeLoader;
                if (loader != null)
                    activeSubsystem = loader.GetLoadedSubsystem<XRMeshSubsystem>();
                if (activeSubsystem != null && activeSubsystem.subsystemDescriptor.id != k_SubsystemId)
                    activeSubsystem = null;
            }

            return activeSubsystem;
        }

        /// <summary>
        /// Sets up the subsystem that adds, updates and removes meshes via communication with the native mesh provider.
        /// </summary>
        internal void Start()
        {
            s_Instance = this;

            m_Subsystem ??= GetActiveSubsystemInstance();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var nativeArrays in m_NativeArrays.Values)
            {
                Array.ForEach(nativeArrays, x => x.Dispose());
            }

            m_NativeArrays.Clear();
        }

        internal void ProcessMeshUpdates(PolySpatialXRMeshesChanged meshData)
        {
            foreach (var mesh in meshData.addOrUpdatedArray)
            {
                AddOrUpdateMesh(mesh);
            }

            foreach (var removedMesh in meshData.removedArray)
            {
                if (m_Subsystem != null && m_Subsystem.running)
                    RemoveMesh(removedMesh.subId1, removedMesh.subId2);

                var trackableId = new TrackableId(removedMesh.subId1, removedMesh.subId2);
                if (!m_NativeArrays.TryGetValue(trackableId, out var nativeArrays))
                    continue;

                Array.ForEach(nativeArrays, x => x.Dispose());
                m_NativeArrays.Remove(trackableId);
            }
        }

        void AddOrUpdateMesh(PolySpatialXRMesh mesh)
        {
            var vertices = new NativeArray<Vector3>(mesh.vertices.Value, Allocator.Persistent);

            // Will be zero length if no normals present
            var normals = new NativeArray<Vector3>(mesh.normals.Value, Allocator.Persistent);
            IDisposable indicesDisposable;

            if (mesh.shortIndices)
            {
                var indices = new NativeArray<ushort>(mesh.indices16.Value, Allocator.Persistent);

                unsafe
                {
                    AddMesh(mesh.meshID.Value.subId1,
                        mesh.meshID.Value.subId2,
                        vertices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(vertices),
                        normals.Length > 0 ? NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(normals) : null,
                        indices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(indices),
                        true,
                        mesh.position.Value.x,
                        mesh.position.Value.y,
                        mesh.position.Value.z,
                        mesh.rotation.Value.x,
                        mesh.rotation.Value.y,
                        mesh.rotation.Value.z,
                        mesh.rotation.Value.w,
                        mesh.scale.Value.x,
                        mesh.scale.Value.y,
                        mesh.scale.Value.z
                    );
                }

                indicesDisposable = indices;
            }
            else
            {
                var indices = new NativeArray<int>(mesh.indices32.Value, Allocator.Persistent);
                unsafe
                {
                    AddMesh(mesh.meshID.Value.subId1,
                        mesh.meshID.Value.subId2,
                        vertices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(vertices),
                        normals.Length > 0 ? NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(normals) : null,
                        indices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(indices),
                        false,
                        mesh.position.Value.x,
                        mesh.position.Value.y,
                        mesh.position.Value.z,
                        mesh.rotation.Value.x,
                        mesh.rotation.Value.y,
                        mesh.rotation.Value.z,
                        mesh.rotation.Value.w,
                        mesh.scale.Value.x,
                        mesh.scale.Value.y,
                        mesh.scale.Value.z
                    );
                }

                indicesDisposable = indices;
            }

            var trackableId = new TrackableId(mesh.meshID.Value.subId1, mesh.meshID.Value.subId2);

            if (m_NativeArrays.TryGetValue(trackableId, out var nativeArrays))
                Array.ForEach(nativeArrays, x => x.Dispose());

            m_NativeArrays[trackableId] = new[] {vertices, normals, indicesDisposable};
        }

        [DllImport("PolySpatialXRPlugin", EntryPoint = "PolySpatialXRSubsystem_AddOrUpdateMesh")]
        static extern unsafe void AddMesh(ulong id1, ulong id2, int numVertices, void* vertices, void* normals, int numTriangles, void* indices, bool shortIndices,
            float xPos, float yPos, float zPos,
            float xRot, float yRot, float zRot, float wRot,
            float xScale, float yScale, float zScale);

        [DllImport("PolySpatialXRPlugin", EntryPoint = "PolySpatialXRSubsystem_RemoveMesh")]
        static extern void RemoveMesh(ulong id1, ulong id2);
    }
}
