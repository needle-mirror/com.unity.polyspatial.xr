using Unity.XR.CoreUtils.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.XR.Capabilities
{
    /// <summary>
    /// Class that represents a Bubblegum capability profile.
    /// </summary>
    class PolySpatialXRVRCapabilityProfile : CapabilityProfile, ICapabilityModifier
    {
        [SerializeField]
        CapabilityDictionary m_Capabilities;

        public bool TryGetCapabilityValue(string capabilityKey, out bool capabilityValue) =>
            m_Capabilities.TryGetValue(capabilityKey, out capabilityValue);
    }
}
