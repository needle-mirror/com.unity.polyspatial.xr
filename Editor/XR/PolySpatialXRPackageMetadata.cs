using System;
using System.Collections.Generic;
using Unity.PolySpatial.XR.Internals;
using UnityEngine;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace UnityEditor.PolySpatial.XR
{
    /// <summary>
    /// This implements the XR Package Interface which is required for the XR Plug-In Management system
    /// to include this XR implementation in the project settings as an XR Plug-in.
    /// </summary>
    class PolySpatialXRPackage : IXRPackage
    {
        class PolySpatialXRLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class PolySpatialXRPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }

        const string k_PolySpatialXRLoaderTypeName = "Unity.PolySpatial.XR.Internals.PolySpatialXRLoader";
        const string k_PolySpatialXRTooltip = "Stream XR data from Play To Device to the editor.";
        const string k_PolySpatialXRLoaderName = "PolySpatial XR";

        // Adds a tooltip over the PolySpatial XR Plug-in Provider
        [XRCustomLoaderUI(k_PolySpatialXRLoaderTypeName, BuildTargetGroup.Standalone)]
        class PolySpatialXRCustomLoaderUI : IXRCustomLoaderUI
        {
            GUIContent m_LabelContent;

            public bool IsLoaderEnabled { get; set; }
            public string[] IncompatibleLoaders => Array.Empty<string>();
            public float RequiredRenderHeight { get; private set; }
            public BuildTargetGroup ActiveBuildTargetGroup { get; set; }

            public void SetRenderedLineHeight(float height)
            {
                RequiredRenderHeight = height;
            }

            void SetUpLabelContentIfNeeded()
            {
                if (m_LabelContent != null)
                    return;

                m_LabelContent = new GUIContent(k_PolySpatialXRLoaderName,  k_PolySpatialXRTooltip);
            }

            public void OnGUI(Rect rect)
            {
                SetUpLabelContentIfNeeded();
                IsLoaderEnabled = EditorGUI.ToggleLeft(rect, m_LabelContent, IsLoaderEnabled);
            }
        }

        static IXRPackageMetadata s_Metadata = new PolySpatialXRPackageMetadata()
        {
            packageName = "PolySpatial XR",
            packageId = "com.unity.polyspatial.xr",
            settingsType = typeof(PolySpatialXRSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new PolySpatialXRLoaderMetadata()
                {
                    loaderName = k_PolySpatialXRLoaderName,
                    loaderType = typeof(PolySpatialXRLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>()
                    {
                        BuildTargetGroup.Standalone,
                    }
                },
            }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        // <summary>
        // Allows the package to configure new settings and/or port old settings to the instance passed in.
        // </summary>
        public bool PopulateNewSettingsInstance(ScriptableObject settings)
        {
            // On future Editor launches it will find them itself, but first time around we
            // need to explicitly add it
            if (settings is PolySpatialXRSettings polySpatialXRSettings)
                EditorBuildSettings.AddConfigObject(PolySpatialXRSettings.k_SettingsKey, polySpatialXRSettings, true);

            return true;
        }
    }
}
