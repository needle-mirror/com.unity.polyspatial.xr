using System.Collections.Generic;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;

namespace UnityEditor.PolySpatial.XR.BuildingBlocks
{
    [BuildingBlockItem(Priority = k_SectionPriority)]
    internal class SetupBuildingBlockSectionBbgum : IBuildingBlockSection
    {
        const int k_SectionPriority = 10;
        const string k_SectionId = "Setup";
        public string SectionId => k_SectionId;

        const string k_SectionLightIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/SectionIcon/SetupIconLight.png";
        const string k_SectionDarkIconPath = "Packages/com.unity.polyspatial.xr/Editor/BuildingBlocks/Icons/SectionIcon/SetupIconDark.png";
        public string SectionIconPath => EditorGUIUtility.isProSkin ? k_SectionDarkIconPath : k_SectionLightIconPath;


        readonly IBuildingBlock[] m_SetupSectionBuildingBlocks = new IBuildingBlock[]
        {
            new CreateHandTrackingVisualizationBlock(), 
            new CreatePolySpatialXROriginHandBlock(),
        };
        
        public IEnumerable<IBuildingBlock> GetBuildingBlocks()
        {
            return m_SetupSectionBuildingBlocks;
        }
    }
}
