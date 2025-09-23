using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    sealed class PolySpatialXRImageDatabase : MutableRuntimeReferenceImageLibrary
    {
        XRReferenceImageLibrary m_XRReferenceImageLibrary;

        List<XRReferenceImage> m_ReferenceImageList = new();

        public PolySpatialXRImageDatabase(XRReferenceImageLibrary serializedLibrary)
        {
            m_XRReferenceImageLibrary = serializedLibrary;
        }

        protected override JobHandle ScheduleAddImageJobImpl(NativeSlice<byte> imageBytes, Vector2Int sizeInPixels, TextureFormat format, XRReferenceImage referenceImage, JobHandle inputDeps)
        {
            var tex2d = referenceImage.texture;

            if (tex2d.format != TextureFormat.BGRA32)
            {
                tex2d = PolySpatialXRImageTrackingSubsystem.ConvertTextureToBGRA32(tex2d);
            }

            ConversionHelpers.ToPolySpatialTextureData(tex2d, default, (texDesc, texBytes) =>
            {
                PolySpatialXRReferenceImage polySpatialRefImage = new()
                {
                    guidLow = PolySpatialXRImageTrackingSubsystem.GUIDLongLow(referenceImage.guid),
                    guidHigh = PolySpatialXRImageTrackingSubsystem.GUIDLongHigh(referenceImage.guid),
                    textureGuidLow = PolySpatialXRImageTrackingSubsystem.GUIDLongLow(referenceImage.textureGuid),
                    textureGuidHigh = PolySpatialXRImageTrackingSubsystem.GUIDLongHigh(referenceImage.textureGuid),
                    specifySize = referenceImage.specifySize,
                    size = referenceImage.size,
                    name = referenceImage.name,
                    textureDesc = texDesc,
                    textureData = new NativeArray<byte>(texBytes, Allocator.Persistent)
                };

                PolySpatialCore.UnitySimulation.NextHandler.SerializedCommand(PolySpatialCommand.AddTrackedImage, polySpatialRefImage);
            });

            m_ReferenceImageList.Add(referenceImage);

            return inputDeps;
        }

        // Mirror of what is supported in VisionOSImageDatabase
        public static readonly TextureFormat[] k_SupportedFormats =
        {
            TextureFormat.Alpha8,
            TextureFormat.R8,
            TextureFormat.R16,
            TextureFormat.RFloat,
            TextureFormat.RGB24,
            TextureFormat.RGBA32,
            TextureFormat.ARGB32,
            TextureFormat.BGRA32
        };

        public override int supportedTextureFormatCount => k_SupportedFormats.Length;

        protected override TextureFormat GetSupportedTextureFormatAtImpl(int index) => k_SupportedFormats[index];

        protected override XRReferenceImage GetReferenceImageAt(int index)
        {
            // There can be a combination of images from a reference library along with a set that are dynamically loaded.
            if (index >= m_XRReferenceImageLibrary.count)
            {
                var newIndex = index - m_XRReferenceImageLibrary.count;
                return m_ReferenceImageList[newIndex];
            }
            return m_XRReferenceImageLibrary[index];
        }

        public override int count => m_XRReferenceImageLibrary.count + m_ReferenceImageList.Count;
    }
}
