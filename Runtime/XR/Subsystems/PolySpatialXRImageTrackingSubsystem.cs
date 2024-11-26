using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace Unity.PolySpatial.XR.Internals.Subsystems
{
    [Preserve]
    class PolySpatialXRImageTrackingSubsystem : XRImageTrackingSubsystem
    {
        internal const string k_SubsystemId = "XRPolySpatial-ImageTracking";

        PolySpatialXRImageTrackingProvider PolySpatialProvider => provider as PolySpatialXRImageTrackingProvider;

        class PolySpatialXRImageTrackingProvider : Provider
        {
            const int k_InitialImageCapacity = 4;

            Dictionary<TrackableId, PolySpatialImage> m_AddedImages = new(k_InitialImageCapacity);
            Dictionary<TrackableId, PolySpatialImage> m_UpdatedImages = new(k_InitialImageCapacity);
            HashSet<TrackableId> m_RemovedImages = new(k_InitialImageCapacity);
            Dictionary<TrackableId, PolySpatialImage> m_AllImages = new();

            public override void Start()
            {
            }

            public override void Stop()
            {
                m_AllImages.Clear();
            }

            public override void Destroy()
            {
            }

            public override TrackableChanges<XRTrackedImage> GetChanges(XRTrackedImage defaultTrackedImage, Allocator allocator)
            {
                var addedImagesCount = m_AddedImages.Count;
                var updatedImagesCount = m_UpdatedImages.Count;
                var removedImagesCount = m_RemovedImages.Count;

                var changes = new TrackableChanges<XRTrackedImage>(addedImagesCount, updatedImagesCount, removedImagesCount, allocator);

                using (new ScopedProfiler("PolySpatialXRImageTrackingSubsystem.GetChanges"))
                {
                    if (addedImagesCount > 0)
                    {
                        var added = changes.added;

                        for (var i = 0; i < addedImagesCount; i++)
                        {
                            var image = m_AddedImages.ElementAt(i);
                            added[i] = image.Value.trackedImage;
                            m_AllImages[image.Key] = image.Value;
                        }

                        m_AddedImages.Clear();
                    }

                    if (updatedImagesCount > 0)
                    {
                        var updated = changes.updated;

                        for (var i = 0; i < updatedImagesCount; i++)
                        {
                            var image = m_UpdatedImages.ElementAt(i);
                            updated[i] = image.Value.trackedImage;
                            m_AllImages[image.Key] = image.Value;
                        }

                        m_UpdatedImages.Clear();
                    }

                    if (removedImagesCount > 0)
                    {
                        var removed = changes.removed;

                        for (var i = 0; i < removedImagesCount; i++)
                        {
                            var imageId = m_RemovedImages.ElementAt(i);
                            removed[i] = imageId;
                            m_AllImages.Remove(imageId);
                        }

                        m_RemovedImages.Clear();
                    }
                }

                return changes;
            }

            public override RuntimeReferenceImageLibrary CreateRuntimeLibrary(XRReferenceImageLibrary serializedLibrary)
            {
                // Do not try to send the image library if PolySpatial hasn't been initialized
                if (PolySpatialCore.UnitySimulation != null)
                    SendReferenceImageLibrary(serializedLibrary);

                return new PolySpatialXRImageDatabase(serializedLibrary);
            }

            public override int requestedMaxNumberOfMovingImages
            {
                get => 0;
                set { }
            }

            static void SendReferenceImageLibrary(XRReferenceImageLibrary serializedLibrary)
            {
                var numImages = serializedLibrary.count;
                if (numImages == 0)
                    return;

                PolySpatialReferenceImageLibrary referenceImageLibrary = new();
                referenceImageLibrary.referenceImages = new List<PolySpatialXRReferenceImage>();
                for (var i = 0; i < numImages; i++)
                {
                    var referenceImage = serializedLibrary[i];

                    var tex2d = referenceImage.texture;
                    if (tex2d == null)
                    {
                        Debug.LogWarning($"Texture is null for reference image {referenceImage.name} in {serializedLibrary.name}. Please ensure that "
                            + "a valid texture is assigned and that Keep Texture at Runtime is checked.", serializedLibrary);

                        continue;
                    }

                    if (tex2d.format != TextureFormat.BGRA32)
                    {
                        tex2d = ConvertTextureToBGRA32(tex2d);
                    }

                    ConversionHelpers.ToPolySpatialTextureData(tex2d, (texDesc, texBytes) =>
                    {
                        PolySpatialXRReferenceImage polySpatialRefImage = new()
                        {
                            guidLow = GUIDLongLow(referenceImage.guid),
                            guidHigh = GUIDLongHigh(referenceImage.guid),
                            textureGuidLow = GUIDLongLow(referenceImage.textureGuid),
                            textureGuidHigh = GUIDLongHigh(referenceImage.textureGuid),
                            specifySize = referenceImage.specifySize,
                            size = referenceImage.size,
                            name = referenceImage.name,
                            textureDesc = texDesc,
                            textureData = new NativeArray<byte>(texBytes, Allocator.Persistent)
                        };

                        referenceImageLibrary.referenceImages.Add(polySpatialRefImage);
                    });
                }

                PolySpatialCore.UnitySimulation.NextHandler.SerializedCommand(PolySpatialCommand.CreateOrUpdateReferenceImageLibrary, referenceImageLibrary);
                referenceImageLibrary.referenceImages.Clear();
            }

            public override RuntimeReferenceImageLibrary imageLibrary
            {
                set
                {
                    if (value == null)
                    {
                        Stop();
                    }
                    else if (value is PolySpatialXRImageDatabase database)
                    {
                    }
                    else
                    {
                        throw new ArgumentException($"{value.GetType().Name} is not a valid VisionOS image library.");
                    }
                }
            }

            public void TryAddARTrackedImage(PolySpatialImage image)
            {
                m_AddedImages[image.trackableId] = image;
                m_UpdatedImages.Remove(image.trackableId);
            }

            public void TryUpdateARTrackedImage(PolySpatialImage image)
            {
                if (!m_AllImages.ContainsKey(image.trackableId))
                {
                    // For the 2nd connection we are not seeing a created come in, ARFoundations will get confused and
                    // eat all updates until a create comes in.  This converts the first update to a create if we haven't
                    // started tracking this image yet.
                    TryAddARTrackedImage(image);
                    return;
                }

                // Make sure there isn't already a removed event
                if (!m_RemovedImages.Contains(image.trackableId))
                {
                    // If the image added hasn't been processed yet, update the added values with these values
                    if (m_AddedImages.ContainsKey(image.trackableId))
                    {
                        m_UpdatedImages.Remove(image.trackableId);
                        m_AddedImages[image.trackableId] = image;
                    }
                    else
                    {
                        m_UpdatedImages[image.trackableId] = image;
                    }
                }
            }

            public void TryRemoveARTrackedImage(PolySpatialImage image)
            {
                // make sure the image has already been previously added before trying to remove it
                if (m_AllImages.ContainsKey(image.trackableId))
                {
                    m_RemovedImages.Add(image.trackableId);
                }

                m_AddedImages.Remove(image.trackableId);
                m_UpdatedImages.Remove(image.trackableId);
            }
        }

        internal static ulong GUIDLongLow(Guid input)
        {
            byte[] bytes = input.ToByteArray();
            return BitConverter.ToUInt64(bytes, 0);
        }

        internal static ulong GUIDLongHigh(Guid input)
        {
            byte[] bytes = input.ToByteArray();
            return BitConverter.ToUInt64(bytes, 8);
        }

        // This has the bonus of letting us support more texture formats than the native image tracker supports.
        internal static Texture2D ConvertTextureToBGRA32(Texture2D originalTexture)
        {
            var tempRT = RenderTexture.GetTemporary(originalTexture.width, originalTexture.height);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(originalTexture, tempRT);

            var previous = RenderTexture.active;
            RenderTexture.active = tempRT;

            var newTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.BGRA32, false);

            // Copy the pixels from the RenderTexture to the new Texture
            newTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            newTexture.Apply();

            // Reset active RenderTexture
            RenderTexture.active = previous;

            return newTexture;
        }

        internal void TryAddARTrackedImage(PolySpatialImage plane)
        {
            PolySpatialProvider?.TryAddARTrackedImage(plane);
        }

        internal void TryUpdateARTrackedImage(PolySpatialImage plane)
        {
            PolySpatialProvider?.TryUpdateARTrackedImage(plane);
        }

        internal void TryRemoveARTrackedImage(PolySpatialImage plane)
        {
            PolySpatialProvider?.TryRemoveARTrackedImage(plane);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void RegisterDescriptor()
        {
            var cinfo = new XRImageTrackingSubsystemDescriptor.Cinfo
            {
                id = k_SubsystemId,
                providerType = typeof(PolySpatialXRImageTrackingProvider),
                subsystemTypeOverride = typeof(PolySpatialXRImageTrackingSubsystem),
                supportsMovingImages = true,
                requiresPhysicalImageDimensions = true,
                supportsMutableLibrary = true,
                supportsImageValidation = true
            };

            XRImageTrackingSubsystemDescriptor.Register(cinfo);
        }
    }
}
