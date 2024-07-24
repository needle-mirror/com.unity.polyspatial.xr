using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialXRLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            // GUID below should match the meta for link.xml
            return FileUtil.GetPhysicalPath(AssetDatabase.GUIDToAssetPath("14e5ab04b0e5548438bff8e724401a1a"));
        }
    }
}
