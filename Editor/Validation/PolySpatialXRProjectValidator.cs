#if UNITY_HAS_XR_VISIONOS
using Unity.PolySpatial;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.VisionOS;

namespace UnityEditor.PolySpatial.XR.Validation
{
    [InitializeOnLoad]
    static class PolySpatialXRProjectValidator
    {
        const string k_PolySpatialXRLoader = "Unity.PolySpatial.XR.Internals.PolySpatialXRLoader";
        const string k_XRSimulationLoader = "UnityEngine.XR.Simulation.SimulationLoader";
        const string k_CategoryFormat = "PolySpatial XR - {0}";

        const BuildTargetGroup k_VisionOSBuildTarget = BuildTargetGroup.VisionOS;
        const BuildTargetGroup k_StandaloneBuildTarget = BuildTargetGroup.Standalone;

        static PolySpatialXRProjectValidator()
        {
            var rules = new BuildValidationRule[]
            {
                new()
                {
                    Message = "Enable the `PolySpatial XR` Plug-in Provider, while using Play To Device, to have XR data sent from device to the editor.",
                    Category = string.Format(k_CategoryFormat, "Play To Device"),
                    Error = false,
                    CheckPredicate = () => PolySpatialUserSettings.instance.ConnectToPlayToDevice && IsLoaderEnabled(k_PolySpatialXRLoader),
                    FixIt = () => EnableLoader(k_PolySpatialXRLoader),
                    // PolySpatial XR should work in cases beyond just RealityKit, we include the RealityKit check in here because Play to Device only works with this mode
                    IsRuleEnabled = () => CheckAppMode(VisionOSSettings.AppMode.RealityKit) && PolySpatialUserSettings.instance.ConnectToPlayToDevice
                },
                new()
                {
                    Message = "Disable `XR Simulation` Plug-in Provider, while using Play To Device, the `PolySpatial XR` Provider will send XR data from the connected device.",
                    Category = string.Format(k_CategoryFormat, "Play To Device"),
                    Error = false,
                    CheckPredicate = () => PolySpatialUserSettings.instance.ConnectToPlayToDevice && !IsLoaderEnabled(k_XRSimulationLoader),
                    FixIt = () => DisableLoader(k_XRSimulationLoader),
                    // PolySpatial XR should work in cases beyond just RealityKit, we include the RealityKit check in here because Play to Device only works with this mode
                    IsRuleEnabled = () => CheckAppMode(VisionOSSettings.AppMode.RealityKit) && PolySpatialUserSettings.instance.ConnectToPlayToDevice
                },
            };

            BuildValidator.AddRules(k_VisionOSBuildTarget, rules);
            BuildValidator.AddRules(k_StandaloneBuildTarget, rules);
        }

        static bool IsLoaderEnabled(string loaderName)
        {
            var isAssigned = XRPackageMetadataStore.IsLoaderAssigned(loaderName, BuildTargetGroup.Standalone);
            return isAssigned;
        }

        static bool TryGetGeneralXRSettings(out XRGeneralSettingsPerBuildTarget generalSettings)
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out generalSettings);
            if (generalSettings == null)
            {
                var assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                if (assets.Length > 0)
                {
                    generalSettings = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), typeof(XRGeneralSettingsPerBuildTarget))
                        as XRGeneralSettingsPerBuildTarget;
                }
            }

            return generalSettings != null;
        }

        static void EnableLoader(string loaderName)
        {
            var didAssign = false;

            if (TryGetGeneralXRSettings(out var generalSettings))
            {
                var buildTargetSettings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                var pluginsSettings = buildTargetSettings.AssignedSettings;
                didAssign = XRPackageMetadataStore.AssignLoader(pluginsSettings, loaderName, BuildTargetGroup.Standalone);
            }

            if (!didAssign)
            {
                Debug.LogError("Unable to assign loader: " + loaderName);
            }
        }

        internal static void DisableLoader(string loaderName)
        {
            var didRemove = false;

            if (TryGetGeneralXRSettings(out var generalSettings))
            {
                var buildTargetSettings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                var pluginsSettings = buildTargetSettings.AssignedSettings;
                didRemove = XRPackageMetadataStore.RemoveLoader(pluginsSettings, loaderName, BuildTargetGroup.Standalone);
            }

            if (!didRemove)
            {
                Debug.LogError("Unable to remove loader: " + loaderName);
            }
        }

        static bool CheckAppMode(VisionOSSettings.AppMode mode)
        {
            return GetEditorSettings(out var editorSettings) && editorSettings.appMode == mode;
        }

        static bool GetEditorSettings(out VisionOSSettings editorSettings)
        {
            editorSettings = VisionOSSettings.currentSettings;
            return editorSettings != null;
        }
    }
}
#endif
