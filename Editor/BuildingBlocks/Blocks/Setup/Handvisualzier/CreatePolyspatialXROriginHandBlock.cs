using System;
using System.IO;
using UnityEditor.PackageManager.UI;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEngine;

namespace UnityEditor.PolySpatial.XR.BuildingBlocks
{
    class CreatePolySpatialXROriginHandBlock : IBuildingBlock
    {
        const string k_Id = "XR Origin Hands PolySpatial";
        const string k_BuildingBlockPath = "GameObject/XR/Setup/" + k_Id;
        const string k_LightIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/Setup/Light/XROriginHandsPolySpatial.png";
        const string k_DarkIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/Setup/Dark/XROriginHandsPolySpatial.png";
        
        const int k_SectionPriority = 10;

        public string Id => k_Id;
        public string IconPath => EditorGUIUtility.isProSkin ? k_DarkIconPath : k_LightIconPath;
        
        const string k_XRIPackageName = "com.unity.xr.interaction.toolkit";
        const string k_HandsPackageName = "com.unity.xr.hands";
        const string k_StarterAssetsSamplesName = "Starter Assets";
        const string k_HandsInteractionDemoSamplesName = "Hands Interaction Demo";
        const string k_HandVisualizerSamplesName = "HandVisualizer";
        const string k_ImportSampleTitle = "Importing Starter Assets, Hands interaction and Hand Visualizer samples.";
        const string k_ImportSampleMessage = "This building block requires you to import samples: \n {0} \n Press \"Ok\" to continue.";

        /// <inheritdoc cref="ExecuteBuildingBlock"/>
        public void ExecuteBuildingBlock() => InstantiateBuildingBlock();
        
        /// Each building block should have an accompanying MenuItem, we add them here.
        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => InstantiateBuildingBlock();

        static void InstantiateBuildingBlock()
        {
            var xriPackageSamples = Sample.FindByPackage(k_XRIPackageName, String.Empty);
            var handsPackageSamples = Sample.FindByPackage(k_HandsPackageName, String.Empty);

            if (xriPackageSamples == null || handsPackageSamples == null)
            {
                Debug.LogError($"Couldn't find samples of the {k_XRIPackageName} / {k_HandsPackageName} packages while trying to instantiate XR Origin Hands PolySpatial; aborting.");
                return;
            }

            var starterAssetsSample = new Sample();
            var handsInteractionDemoSample = new Sample();
            var handVisualizerSample = new Sample();
            
            foreach (var packageSample in xriPackageSamples)
            {
                if (packageSample.displayName == k_StarterAssetsSamplesName)
                    starterAssetsSample = packageSample;
                else if (packageSample.displayName == k_HandsInteractionDemoSamplesName)
                    handsInteractionDemoSample = packageSample;
            }

            foreach (var packageSample in handsPackageSamples)
            {
                if (packageSample.displayName == k_HandVisualizerSamplesName)
                    handVisualizerSample = packageSample;
            }
            
            if (string.IsNullOrEmpty(starterAssetsSample.displayName) ||
                string.IsNullOrEmpty(handsInteractionDemoSample.displayName) ||
                string.IsNullOrEmpty(handVisualizerSample.displayName))
            {
                Debug.LogError($"Couldn't find samples folders for {k_StarterAssetsSamplesName}, {k_HandVisualizerSamplesName} and/or {k_HandsInteractionDemoSamplesName}; aborting.");
                return;
            }

            var samplesToImport = "";
            var needsToImportStarterAssetsSample = !starterAssetsSample.isImported;
            var needsToImportHandsInteractionDemoSample = !handsInteractionDemoSample.isImported;
            var needsToImportHandVisualizerSample = !handVisualizerSample.isImported;
            
            if(needsToImportStarterAssetsSample)
                samplesToImport += $"{starterAssetsSample.displayName}\n";
            if(needsToImportHandsInteractionDemoSample)
                samplesToImport += $"{handsInteractionDemoSample.displayName}\n";
            if(needsToImportHandVisualizerSample)
                samplesToImport += $"{handVisualizerSample.displayName}\n";
            
            if (needsToImportStarterAssetsSample || needsToImportHandsInteractionDemoSample || needsToImportHandVisualizerSample)
            {
                if (EditorUtility.DisplayDialog(k_ImportSampleTitle,
                        String.Format(k_ImportSampleMessage,samplesToImport),
                        "Ok", "Cancel"))
                {
                    if(needsToImportStarterAssetsSample)
                        starterAssetsSample.Import(Sample.ImportOptions.OverridePreviousImports);
                    if(needsToImportHandsInteractionDemoSample)
                        handsInteractionDemoSample.Import(Sample.ImportOptions.OverridePreviousImports);
                    if(needsToImportHandVisualizerSample)
                        handVisualizerSample.Import(Sample.ImportOptions.OverridePreviousImports);
                }
                else
                {
                    return;
                }
            }
            
            var fullPathToPrefab = handsInteractionDemoSample.importPath + "/Runtime/Prefabs/XR Origin Hands PolySpatial Variant.prefab";
            var pathToPrefab = "Assets/" + Path.GetRelativePath(Application.dataPath, fullPathToPrefab);

            var xrOriginHandsPolySpatialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathToPrefab);
            if (xrOriginHandsPolySpatialPrefab == null)
            {
                Debug.LogError($"Couldn't find the XR Origin Hands prefab at {pathToPrefab}; aborting.");
                return;
            }
            
            var xrOriginHandsPolySpatialGO = (GameObject) PrefabUtility.InstantiatePrefab(xrOriginHandsPolySpatialPrefab);
            xrOriginHandsPolySpatialGO.name = k_Id;

         

            Selection.activeGameObject = xrOriginHandsPolySpatialGO;
            Undo.RegisterCreatedObjectUndo(xrOriginHandsPolySpatialGO, "Created XR Origin Hands PolySpatial GameObject");
        }
    }
}