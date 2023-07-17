using System.Collections.Generic;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEngine;

namespace UnityEditor.PolySpatial.XR.BuildingBlocks
{
    [BuildingBlockItem(Priority = k_SectionPriority)]
    internal class CoachingUIBuildingBlockSection : IBuildingBlockSection
    {
        const int k_SectionPriority = 12;
        const string k_SectionId = "XR UI and Layout";
        public string SectionId => k_SectionId;

        const string k_SectionLightIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/SectionIcon/XRUIAndLayoutIconLight.png";
        const string k_SectionDarkIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/SectionIcon/XRUIAndLayoutIconDark.png";
        public string SectionIconPath => EditorGUIUtility.isProSkin ? k_SectionDarkIconPath : k_SectionLightIconPath;

        const string k_CoachUIBBlocksMenuPath = "GameObject/XR/";

        const string k_CoachingUITapToPlacePrefabPath = "Packages/com.unity.polyspatial.xr/Runtime/CoachingUI/Prefabs/CoachingUITapToPlaceContent.prefab";
        const string k_CoachUITapToPlaceBBlockMenuPath = k_CoachUIBBlocksMenuPath + k_SectionId + "/" + k_CoachUITapToPlaceBBlockName;
        const string k_CoachUITapToPlaceBBlockName = "Tap to Place Coach UI";
        static GameObject s_CoachUITapToPlacePrefab;
        static PrefabCreatorBuildingBlock s_TapToPlaceBBlock;

        const string k_CoachUIScanSurfacePrefabPath = "Packages/com.unity.polyspatial.xr/Runtime/CoachingUI/Prefabs/CoachingUIScanForSurfaces.prefab";
        const string k_CoachUIScanForSurfaceBBlockMenuPath = k_CoachUIBBlocksMenuPath + k_SectionId + "/" + k_CoachUIScanSurfaceBBlockName;
        const string k_CoachUIScanSurfaceBBlockName = "Scan Surface Coach UI";
        static GameObject s_CoachUIScanSurfacePrefab;
        static PrefabCreatorBuildingBlock s_ScanSurfaceBBlock;

        [MenuItem(k_CoachUIScanForSurfaceBBlockMenuPath, false, 1)]
        public static void ExecuteScanForSurfaceMenuItem(MenuCommand command)
        {
            if (s_CoachUIScanSurfacePrefab == null)
                GenerateScanForSurfaceBuildingBlock();
            s_ScanSurfaceBBlock.ExecuteBuildingBlock();
        }

        [MenuItem(k_CoachUITapToPlaceBBlockMenuPath, false, 1)]
        public static void ExecuteTapToPlaceMenuItem(MenuCommand command)
        {
            if (s_CoachUITapToPlacePrefab == null)
                GenerateTapToPlaceBuildingBlock();
            s_TapToPlaceBBlock.ExecuteBuildingBlock();
        }

        public IEnumerable<IBuildingBlock> GetBuildingBlocks()
        {
            var prefabBuildingBlocksList = new List<IBuildingBlock>();

            var tapToPlaceBuildingBlock = GenerateTapToPlaceBuildingBlock();
            if (tapToPlaceBuildingBlock != null)
                prefabBuildingBlocksList.Add(tapToPlaceBuildingBlock);

            var scanSurfaceBuildingBlock = GenerateScanForSurfaceBuildingBlock();
            if (scanSurfaceBuildingBlock != null)
                prefabBuildingBlocksList.Add(scanSurfaceBuildingBlock);

            return prefabBuildingBlocksList;
        }

        static PrefabCreatorBuildingBlock GenerateTapToPlaceBuildingBlock()
        {
            if (s_CoachUITapToPlacePrefab == null)
                s_CoachUITapToPlacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_CoachingUITapToPlacePrefabPath);

            if (s_CoachUITapToPlacePrefab != null)
            {
                s_TapToPlaceBBlock = new PrefabCreatorBuildingBlock(s_CoachUITapToPlacePrefab, k_CoachUITapToPlaceBBlockName, "");
                return s_TapToPlaceBBlock;
            }

            const string error = "Couldn't find Tap To Place prefab asset at " + k_CoachingUITapToPlacePrefabPath;
            Debug.LogError(error);
            return null;
        }

        static PrefabCreatorBuildingBlock GenerateScanForSurfaceBuildingBlock()
        {
            if (s_CoachUIScanSurfacePrefab == null)
                s_CoachUIScanSurfacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_CoachUIScanSurfacePrefabPath);

            if (s_CoachUIScanSurfacePrefab != null)
            {
                s_ScanSurfaceBBlock = new PrefabCreatorBuildingBlock(s_CoachUIScanSurfacePrefab, k_CoachUIScanSurfaceBBlockName, "");
                return s_ScanSurfaceBBlock;
            }

            const string error = "Couldn't find Scan for Surface prefab asset at " + k_CoachUIScanSurfacePrefabPath;
            Debug.LogError(error);
            return null;
        }
    }
}
