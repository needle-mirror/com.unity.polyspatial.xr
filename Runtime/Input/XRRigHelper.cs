using System.Collections.Generic;
using UnityEngine;

namespace Unity.PolySpatial.Input
{
    /// <summary>
    /// Utility component that allows us to enable/disable
    /// rig objects
    /// </summary>
    internal class XRRigHelper : MonoBehaviour
    {
        public GameObject LeftController;
        public GameObject RightController;

        public GameObject LeftHand;
        public GameObject RightHand;

        bool m_EnabledState = true;

        Dictionary<GameObject, bool> m_StateAtLaunch = new ();


        void SetObjectActiveState(GameObject obj, bool expectedState)
        {
            if (obj != null && obj.activeSelf != expectedState)
                obj.SetActive(expectedState);
        }

        void SetObjectsActiveState(bool expectedState)
        {
            SetObjectActiveState(LeftController, expectedState);
            SetObjectActiveState(RightController, expectedState);
            SetObjectActiveState(LeftHand, expectedState);
            SetObjectActiveState(RightHand, expectedState);
        }

        void OnDisable()
        {
            ResetObjects();
        }

        void OnDestroy()
        {
            ResetObjects();
        }

        void FixedUpdate()
        {
            // The intent here is to make sure that
            // objects set disabled are kept disabled.
            // Things like Hands may get re-enabled by
            // XR Interaction, causing ghosting effects
            // if connected to a sim.
            if (!m_EnabledState)
                SetObjectsActiveState(false);
        }

        internal void DisableObjects()
        {
            m_EnabledState = false;

            m_StateAtLaunch.Clear();

            if (LeftController)
                m_StateAtLaunch[LeftController] = LeftController.activeSelf;
            if (RightController)
                m_StateAtLaunch[RightController] = RightController.activeSelf;
            if (LeftHand)
                m_StateAtLaunch[LeftHand] = LeftHand.activeSelf;
            if (RightHand)
                m_StateAtLaunch[RightHand] = RightHand.activeSelf;
        }

        internal void ResetObjects()
        {
            m_EnabledState = true;
            foreach (var kvp in m_StateAtLaunch)
                SetObjectActiveState(kvp.Key, kvp.Value);
        }
    }
}
