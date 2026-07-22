using Unity.XR.CoreUtils.Capabilities;
using UnityEngine;

namespace Unity.PolySpatial.XR.Capabilities
{
    /// <summary>
    /// Class that represents a PolySpatial Metal capability profile.
    /// </summary>
    class PolySpatialMetalCapabilityProfile : CapabilityProfile, ICapabilityModifier
    {
        [SerializeField]
        CapabilityDictionary m_Capabilities;

        public bool TryGetCapabilityValue(string capabilityKey, out bool capabilityValue) =>
            m_Capabilities.TryGetValue(capabilityKey, out capabilityValue);
    }
}
