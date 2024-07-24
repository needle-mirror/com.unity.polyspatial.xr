using System.Collections.Generic;
using UnityEngine.XR;
using LegacyMeshId = UnityEngine.XR.MeshId;

namespace Unity.PolySpatial.XR.Internals
{
    class MeshInfoComparer : IComparer<MeshInfo>
    {
        /// <summary>
        /// Mesh infos are stored last to first so that the dequeue operation is fast
        /// - &lt; 0 <paramref name="infoA"/> will appear before infoB in the list
        /// - 0 <paramref name="infoA"/> has the same priority as <paramref name="infoB"/>
        /// - &gt; 0 <paramref name="infoB"/> will appear before infoA in the list
        /// </summary>
        public int Compare(MeshInfo infoA, MeshInfo infoB)
        {
            // Prioritize 'added' over 'updated'
            if (infoA.ChangeState < infoB.ChangeState)
            {
                return 1;
            }
            else if (infoB.ChangeState < infoA.ChangeState)
            {
                return -1;
            }
            else
            {
                // If 'A' has a high priority, then we return a positive number
                // which puts A last in the list. This means we can dequeue
                // the next mesh to generate by taking the last element.
                return (infoA.PriorityHint - infoB.PriorityHint);
            }
        }
    }

    class MeshQueue
    {
        internal void EnqueueUnique(MeshInfo meshInfo)
        {
            if (m_MeshSet.Contains(meshInfo.MeshId))
            {
                UpdateExisting(meshInfo);
            }
            else
            {
                InsertNew(meshInfo);
            }
        }

        internal bool TryDequeue(IReadOnlyDictionary<LegacyMeshId, MeshInfo> generating, out MeshInfo meshInfo)
        {
            for (var i = m_Queue.Count - 1; i >= 0; --i)
            {
                meshInfo = m_Queue[i];
                if (!generating.ContainsKey(meshInfo.MeshId))
                {
                    m_Queue.RemoveAt(i);
                    m_MeshSet.Remove(meshInfo.MeshId);
                    return true;
                }
            }

            meshInfo = default;
            return false;
        }

        internal bool Remove(LegacyMeshId meshId)
        {
            // It is relatively rare to remove an existing mesh
            // (this means it was removed while awaiting generation).
            // So it most cases we should be able to early out.
            if (!m_MeshSet.Remove(meshId))
                return false;

            // Otherwise, perform a linear search and remove it.
            for (var i = 0; i < m_Queue.Count; ++i)
            {
                if (m_Queue[i].MeshId.Equals(meshId))
                {
                    m_Queue.RemoveAt(i);
                    break;
                }
            }

            return true;
        }

        void InsertNew(MeshInfo meshInfo)
        {
            var index = m_Queue.BinarySearch(meshInfo, m_MeshInfoComparer);
            if (index < 0)
                index = ~index;

            m_Queue.Insert(index, meshInfo);
            m_MeshSet.Add(meshInfo.MeshId);
        }

        void UpdateExisting(MeshInfo meshInfo)
        {
            for (var i = 0; i < m_Queue.Count; ++i)
            {
                var existing = m_Queue[i];
                if (existing.MeshId.Equals(meshInfo.MeshId))
                {
                    // Only need to do anything if they are not equal
                    if (existing.PriorityHint != meshInfo.PriorityHint)
                    {
                        existing.PriorityHint = meshInfo.PriorityHint;
                        m_Queue[i] = existing;
                        m_Queue.Sort(m_MeshInfoComparer);
                    }
                    break;
                }
            }
        }

        public void Clear()
        {
            m_Queue.Clear();
            m_MeshSet.Clear();
        }

        // This list is kept sorted according to MeshInfoComparer
        List<MeshInfo> m_Queue = new ();

        HashSet<LegacyMeshId> m_MeshSet = new ();

        MeshInfoComparer m_MeshInfoComparer = new ();
    }
}
