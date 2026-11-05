using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialXRMeshTracker : IDisposable
    {
        PolySpatialXRMeshManager m_MeshManager;

        internal void InitializeXRMeshes()
        {
            m_MeshManager = PolySpatialXRMeshManager.instance;

            if (m_MeshManager == null)
            {
                Logging.LogError(LogCategory.XR, "No PolySpatialXRMeshManager found in scene.");
                return;
            }

            m_MeshManager.meshesChanged += MeshManagerOnMeshesChanged;

            // Send over a snapshot of the current set of meshes before connection.
            // Only call after meshesChanged is set.
            m_MeshManager.SendCurrentMeshes();
        }

        // This is invoked in `Update` from PolySpatialXRMeshManager where there is a change to the set of meshes that has been detected.
        void MeshManagerOnMeshesChanged(PolySpatialXRMeshesChanged meshChangeData)
        {
            HostCommandHelper.SendXRMeshData(meshChangeData);
            foreach (var mesh in meshChangeData.addOrUpdatedArray)
            {
                mesh.vertices.Value.Dispose();
                mesh.normals.Value.Dispose();
                mesh.indices16?.Dispose();
                mesh.indices32?.Dispose();
            }
        }

        internal void EndSession()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_MeshManager != null)
                m_MeshManager.meshesChanged -= MeshManagerOnMeshesChanged;
        }
    }
}
