using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEditor.PolySpatial.XR
{
    /// <summary>
    /// Settings for PolySpatial XR Plug-In for the XR Plug-In Management System.
    /// </summary>
    [Serializable]
    [XRConfigurationData("Play To Device", k_SettingsKey)]
    class PolySpatialXRSettings : ScriptableObject
    {
        /// <summary>
        /// Configuration key for the settings.
        /// </summary>
        internal const string k_SettingsKey = "com.unity.polyspatial.xr.xr_settings";

        /// <summary>
        /// Get the instance of the <see cref="PolySpatialXRSettings"/>.
        /// </summary>
        public static PolySpatialXRSettings currentSettings => EditorBuildSettings.TryGetConfigObject(k_SettingsKey, out PolySpatialXRSettings settings) ? settings : null;
    }
}
