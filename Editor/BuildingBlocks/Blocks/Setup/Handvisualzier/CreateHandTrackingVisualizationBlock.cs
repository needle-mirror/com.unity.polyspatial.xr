using System;
using UnityEditor.PackageManager.UI;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEngine;

namespace UnityEditor.PolySpatial.XR.BuildingBlocks
{
    class CreateHandTrackingVisualizationBlock : IBuildingBlock
    {
        const string k_Id = "Hand Tracking visualization";
        const string k_BuildingBlockPath = "GameObject/XR/Setup/" + k_Id;
        const string k_LightIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/Setup/Light/Handvisualizer.png";
        const string k_DarkIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/Setup/Dark/Handvisualizer.png";
        
        const int k_SectionPriority = 10;

        public string Id => k_Id;
        public string IconPath => EditorGUIUtility.isProSkin ? k_DarkIconPath : k_LightIconPath;
        
        const string k_PackageName = "com.unity.xr.hands";
        const string k_HandsSampleName = "HandVisualizer";
        const string k_PackageDisplayName = "XR Hands";
        const string k_ImportSampleTitle = "Importing Hands sample folder.";
        const string k_ImportSampleMessage = "The hands sample is going to be imported from the XR Hands package, press \"Ok\" to continue.";

        const string k_HandVisualizerPrefabPath = "Packages/com.unity.polyspatial.xr/Runtime/Hands/HandVisualizerRig.prefab";
        
        /// <inheritdoc cref="ExecuteBuildingBlock"/>
        public void ExecuteBuildingBlock() => InstantiateBuildingBlock();
        
        /// Each building block should have an accompanying MenuItem, we add them here.
        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => InstantiateBuildingBlock();
        
        static void InstantiateBuildingBlock()
        {
            var packageSamples = Sample.FindByPackage(k_PackageName, String.Empty);
            if (packageSamples == null)
            {
                Debug.LogError($"Couldn't find samples of the {k_PackageName} package for importing the Hands sample; aborting.");
                return;
            }

            var foundHandVisualizerSample = false;
            foreach (var packageSample in packageSamples)
            {
                if (packageSample.displayName != k_HandsSampleName)
                    continue;

                if (!packageSample.isImported)
                {
                    if (EditorUtility.DisplayDialog(k_ImportSampleTitle, k_ImportSampleMessage, "Ok", "Cancel"))
                    {
                        packageSample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                    else
                    {
                        return;
                    }
                }

                foundHandVisualizerSample = true;
                break;
            }
            
            if (!foundHandVisualizerSample)
            {
                Debug.LogError($"Couldn't find the {k_HandsSampleName} sample in the {k_PackageDisplayName} package; aborting.");
                return;
            }
            
            var handVisualizerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_HandVisualizerPrefabPath);
            var handVisualizerGO = PrefabUtility.InstantiatePrefab(handVisualizerPrefab);
            handVisualizerGO.name = k_Id;
        
            Selection.activeGameObject = handVisualizerGO as GameObject;
            Undo.RegisterCreatedObjectUndo (handVisualizerGO, "Created Hand Visualizer GO");
        }
    }
}