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

        const string k_CoachUITapToPlaceIconPathDark = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/UIAndLayout/Dark/TapToPlaceCoachUI.png";
        const string k_CoachUITapToPlaceIconPathLight = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/UIAndLayout/Light/TapToPlaceCoachUI.png";
        const string k_CoachingUITapToPlacePrefabPath = "Packages/com.unity.polyspatial.xr/Runtime/CoachingUI/Prefabs/CoachingUITapToPlaceContent.prefab";
        const string k_CoachUITapToPlaceBBlockMenuPath = k_CoachUIBBlocksMenuPath + k_SectionId + "/" + k_CoachUITapToPlaceBBlockName;
        const string k_CoachUITapToPlaceBBlockName = "Tap to Place Coach UI";
        static PrefabCreatorBuildingBlock s_TapToPlaceBBlock;

        const string k_CoachUIScanSurfaceIconPathDark = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/UIAndLayout/Dark/ScanSurfaceCoachUI.png";
        const string k_CoachUIScanSurfaceIconPathLight = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/Blocks/UIAndLayout/Light/ScanSurfaceCoachUI.png";
        const string k_CoachUIScanSurfacePrefabPath = "Packages/com.unity.polyspatial.xr/Runtime/CoachingUI/Prefabs/CoachingUIScanForSurfaces.prefab";
        const string k_CoachUIScanForSurfaceBBlockMenuPath = k_CoachUIBBlocksMenuPath + k_SectionId + "/" + k_CoachUIScanSurfaceBBlockName;
        const string k_CoachUIScanSurfaceBBlockName = "Scan Surface Coach UI";
        static PrefabCreatorBuildingBlock s_ScanSurfaceBBlock;

        [MenuItem(k_CoachUIScanForSurfaceBBlockMenuPath, true)]
        static bool ExecuteScanForSurfaceMenuItemValidation()
        {
            if (s_ScanSurfaceBBlock == null)
                InitializeBlocks();
            return s_ScanSurfaceBBlock.IsEnabled;
        }

        [MenuItem(k_CoachUIScanForSurfaceBBlockMenuPath, false, 1)]
        public static void ExecuteScanForSurfaceMenuItem(MenuCommand command)
        {
            s_ScanSurfaceBBlock.ExecuteBuildingBlock();
        }

        [MenuItem(k_CoachUITapToPlaceBBlockMenuPath, true)]
        static bool ExecuteTapToPlaceMenuItemValidation()
        {
            if(s_TapToPlaceBBlock == null)
                InitializeBlocks();
            return s_TapToPlaceBBlock.IsEnabled;
        }

        [MenuItem(k_CoachUITapToPlaceBBlockMenuPath, false, 1)]
        public static void ExecuteTapToPlaceMenuItem(MenuCommand command)
        {
            s_TapToPlaceBBlock.ExecuteBuildingBlock();
        }

        static void InitializeBlocks()
        {
            s_TapToPlaceBBlock = new PrefabCreatorBuildingBlock(k_CoachingUITapToPlacePrefabPath,
                k_CoachUITapToPlaceBBlockName,
                EditorGUIUtility.isProSkin ? k_CoachUITapToPlaceIconPathDark : k_CoachUITapToPlaceIconPathLight,
                true,
                k_CoachUITapToPlaceBBlockName);
            
            s_ScanSurfaceBBlock = new PrefabCreatorBuildingBlock(k_CoachingUITapToPlacePrefabPath,
                k_CoachUITapToPlaceBBlockName,
                EditorGUIUtility.isProSkin ? k_CoachUITapToPlaceIconPathDark : k_CoachUITapToPlaceIconPathLight,
                true,
                k_CoachUITapToPlaceBBlockName);
        }
        
        public IEnumerable<IBuildingBlock> GetBuildingBlocks()
        {
            var prefabBuildingBlocksList = new List<IBuildingBlock>();

            InitializeBlocks();

            prefabBuildingBlocksList.Add(s_TapToPlaceBBlock);
            prefabBuildingBlocksList.Add(s_ScanSurfaceBBlock);

            return prefabBuildingBlocksList;
        }
    }
}