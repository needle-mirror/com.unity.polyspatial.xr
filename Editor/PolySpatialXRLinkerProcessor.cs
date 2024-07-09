using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialXRLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            // GUID below should match the meta for link.xml
            var assetPath = AssetDatabase.GUIDToAssetPath("14e5ab04b0e5548438bff8e724401a1a");

            Assert.IsFalse(string.IsNullOrEmpty(assetPath), "GUID for the meta for link.xml in PolySpatialXR can't be found.");

            return FileUtil.GetPhysicalPath(assetPath);
        }
    }
}
