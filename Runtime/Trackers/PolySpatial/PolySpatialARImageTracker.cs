using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityObject = UnityEngine.Object;

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialARImageTracker : IDisposable
    {
        bool m_HostConnected;

        PolySpatialHostID m_PolySpatialHostID;

        ARTrackedImageManager m_TrackedImageManager;

        bool m_LibraryUpdateInProgress;

        JobHandle m_LibraryUpdateJob;

        Dictionary<SerializableGuid, XRReferenceImagePayload> m_XRReferenceImageLookup = new();

        ulong m_ImageCounter = 1;

        void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            if (!m_HostConnected)
                return;

            PolySpatialARTrackedImageArray trackedImageArray = new ();
            trackedImageArray.images = new List<PolySpatialARTrackedImage>();

            foreach (var image in eventArgs.removed)
            {
                var output = CreatePolySpatialARTrackedImage(image.Value, ARTrackedImageOperation.Removed);
                if (output != null)
                    trackedImageArray.images.Add(output);
            }

            foreach (var image in eventArgs.added)
            {
                var output = CreatePolySpatialARTrackedImage(image, ARTrackedImageOperation.Created);
                if (output != null)
                    trackedImageArray.images.Add(output);
            }

            foreach (var image in eventArgs.updated)
            {
                var output = CreatePolySpatialARTrackedImage(image, ARTrackedImageOperation.Updated);
                if (output != null)
                    trackedImageArray.images.Add(output);
            }

            if (trackedImageArray.images.Count > 0)
            {
                HostCommandHelper.UpdateARTrackedImage(trackedImageArray, m_PolySpatialHostID);
                trackedImageArray.images.Clear();
            }
        }

        PolySpatialARTrackedImage CreatePolySpatialARTrackedImage(ARTrackedImage image, ARTrackedImageOperation operation)
        {
            var imageTransform = image.transform;

            // Image validation wont let us attach the image guid so we have to look it up from the texture guid which it
            // will let us send.
            var textureGuid = image.referenceImage.textureGuid;
            var serializableTextureGuid = new SerializableGuid(textureGuid);

            m_XRReferenceImageLookup.TryGetValue(serializableTextureGuid, out var referenceImageData);

            if (referenceImageData == null)
            {
                Debug.LogError("Unable to lookup reference image with guid: " + serializableTextureGuid);
                return null;
            }

            return new()
            {
                operation = operation,
                trackingId = new()
                {
                    subId1 = image.trackableId.subId1,
                    subId2 = image.trackableId.subId2
                },
                arImageTrackingState = (ARImageTrackingState)image.trackingState,
                position = imageTransform.localPosition,
                rotation = imageTransform.localRotation,
                size = image.size,
                referenceImageGUIDLow = referenceImageData.guidLow,
                referenceImageGUIDHigh = referenceImageData.guidHigh,
                referenceImageTextureGUIDLow = referenceImageData.textureGuidLow,
                referenceImageTextureGUIDHigh = referenceImageData.textureGuidHigh
            };
        }

        internal void InitializeARImageTracker(PolySpatialHostID hostID)
        {
            m_PolySpatialHostID = hostID;

            var imageTrackerManagers = UnityObject.FindObjectsByType<ARTrackedImageManager>(FindObjectsSortMode.None);

            if (imageTrackerManagers.Length == 0)
                return;

            if (imageTrackerManagers.Length > 1)
                Logging.LogWarning(LogCategory.XR, "Multiple ARTrackedImageManager found in scene, using the first one");

            m_TrackedImageManager = imageTrackerManagers[0];

            if (m_TrackedImageManager == null)
            {
                Logging.LogError(LogCategory.XR, "ARTrackedImageManager is null");
                return;
            }

            m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);

            m_HostConnected = true;
        }

        internal void EndConnection()
        {
            m_HostConnected = false;
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
        }

        public void Dispose()
        {
            if (m_TrackedImageManager != null)
                m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);
        }

        internal class XRReferenceImagePayload
        {
            internal ulong guidLow;
            internal ulong guidHigh;
            internal ulong textureGuidLow;
            internal ulong textureGuidHigh;
            internal string name;

            internal XRReferenceImagePayload(ulong guidLowInput, ulong guidHighInput, ulong textureGuidLowInput, ulong textureGuidHighInput, string nameInput)
            {
                guidLow = guidLowInput;
                guidHigh = guidHighInput;
                textureGuidLow = textureGuidLowInput;
                textureGuidHigh = textureGuidHighInput;
                name = nameInput;
            }
        }

        internal void AddTrackedImage(PolySpatialXRReferenceImage image)
        {
            if (m_TrackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
            {
                var texture = FullUpdateTextureAsset(image.textureDesc, image.textureData.Value);

                // Note ScheduleAddImageWithValidationJob will throw:
                // ArgumentException: referenceImage.guid must be empty (all zeroes).
                // if we try to pass a non empty guid in.
                // But we need to preserve that information to pull it out on the other side,
                // so we are encoding all the information we need and referencing it from the only piece the data that
                // comes out the other end which is the textureGuid.
                var referenceImageData = new XRReferenceImagePayload(image.guidLow, image.guidHigh, image.textureGuidLow,
                    image.textureGuidHigh, image.name);

                var currentImageId = m_ImageCounter++;
                var textureGuid = new SerializableGuid(currentImageId, 0);

                // Note there is a 1 in 2^128 chance of a collision here
                m_XRReferenceImageLookup[textureGuid] = referenceImageData;

                var referenceImage = new XRReferenceImage(
                    SerializableGuid.empty,
                    textureGuid,
                    image.size,
                    image.name,
                    texture);

                var textureData = new NativeArray<byte>(image.textureData.Value, Allocator.Persistent);

                var referenceImageJobState = mutableLibrary.ScheduleAddImageWithValidationJob(
                    textureData,
                    new Vector2Int(image.textureDesc.width, image.textureDesc.height),
                    texture.format,
                    referenceImage);

                new DeallocateJob { data = textureData }.Schedule(referenceImageJobState.jobHandle);
            }
        }

        struct DeallocateJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeArray<byte> data;

            public void Execute() { }
        }

        internal void CreateOrUpdateReferenceImageLibrary(PolySpatialReferenceImageLibrary referenceImageLibrary)
        {
            if (m_TrackedImageManager == null)
                return;

            if (!m_TrackedImageManager.descriptor.supportsMutableLibrary)
            {
                Logging.LogError(LogCategory.XR, "Host does not support a mutable runtime reference image library.");
                return;
            }

            // If there is an existing library clobber it and build a new one.
            // There doesn't appear to be a way in the existing API to only update an existing entry.
            var cleanLibrary = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            m_TrackedImageManager.referenceLibrary = m_TrackedImageManager.subsystem.CreateRuntimeLibrary(cleanLibrary);

            if (m_TrackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
            {
                var scheduleAddImageJob = new JobHandle();
                var referenceImages = referenceImageLibrary.referenceImages;
                for (var i = 0; i < referenceImages.Count; i++)
                {
                    var image = referenceImages[i];
                    var textureGuid = new SerializableGuid(image.textureGuidLow, image.textureGuidHigh);

                    var texture = FullUpdateTextureAsset(image.textureDesc, image.textureData.Value);

                    // Note ScheduleAddImageWithValidationJob will throw:
                    // ArgumentException: referenceImage.guid must be empty (all zeroes).
                    // if we try to pass a non empty guid in.
                    var referenceImage = new XRReferenceImage(
                        SerializableGuid.empty,
                        textureGuid,
                        image.size,
                        image.name,
                        texture);

                    // The Validator won't let me pass in a guid for the image, so couple it with the texture guid,
                    // so we can look it up when we go back to the Host.
                    var referenceImageData = new XRReferenceImagePayload(image.guidLow, image.guidHigh, image.textureGuidLow,
                        image.textureGuidHigh, image.name);

                    m_XRReferenceImageLookup[textureGuid] = referenceImageData;

                    var textureData = new NativeArray<byte>(image.textureData.Value, Allocator.Persistent);

                    var referenceImageJobState = mutableLibrary.ScheduleAddImageWithValidationJob(
                        textureData,
                        new Vector2Int(image.textureDesc.width, image.textureDesc.height),
                        texture.format,
                        referenceImage,
                        scheduleAddImageJob);

                    scheduleAddImageJob = referenceImageJobState.jobHandle;
                    scheduleAddImageJob = new DeallocateJob { data = textureData }.Schedule(scheduleAddImageJob);
                }

                // If the reference library is empty, which it should be on P2D Host, the tracked image manager will disable itself
                // We need to enable it once we have added the image library.
                if (referenceImages.Count > 0)
                {
                    m_LibraryUpdateInProgress = true;
                    m_LibraryUpdateJob = scheduleAddImageJob;
                }
            }
        }

        Texture2D FullUpdateTextureAsset(in PolySpatialTextureData data, NativeArray<byte> pixelData)
        {
            var texture = new Texture2D(1, 1,
                GraphicsFormat.B8G8R8A8_UNorm,
                TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);

            var width = data.width;
            var height = data.height;
            var mipCount = data.mipCount;
            var unityGraphicsFormat = (GraphicsFormat)data.unityGraphicsFormat;
            var textureFormat = GraphicsFormatUtility.GetTextureFormat(unityGraphicsFormat);

            if (texture.width != width || texture.height != height || texture.mipmapCount != mipCount || texture.format != textureFormat)
            {
                texture.Reinitialize(width, height, unityGraphicsFormat, mipCount != 1);
                texture.Apply();
            }

            UpdateTextureFilterWrapModes(texture, data.filterMode, data.wrapModeU, data.wrapModeV, data.wrapModeW);

            var dstPtr = texture.GetRawTextureData<byte>();
            if ((ulong)dstPtr.Length != data.imageSize)
            {
                Debug.LogError($"Native texture size mismatch, data is {data.imageSize} bytes, butGetRawTextureData expects {dstPtr.Length} bytes");
                return null;
            }

            dstPtr.CopyFrom(pixelData);
            texture.Apply();

            return texture;
        }

        void UpdateTextureFilterWrapModes(
            Texture texture, PolySpatialTextureFilterMode filterMode, PolySpatialTextureWrapMode wrapModeU,
            PolySpatialTextureWrapMode wrapModeV, PolySpatialTextureWrapMode? wrapModeW = null)
        {
            texture.filterMode = ConversionHelpers.ToUnityTextureFilterMode(filterMode);
            texture.wrapModeU = ConversionHelpers.ToUnityTextureWrapMode(wrapModeU);
            texture.wrapModeV = ConversionHelpers.ToUnityTextureWrapMode(wrapModeV);

            if (wrapModeW.HasValue)
                texture.wrapModeW = ConversionHelpers.ToUnityTextureWrapMode(wrapModeW.Value);
        }

        internal void Update()
        {
            if (m_LibraryUpdateInProgress)
            {
                if (m_LibraryUpdateJob.IsCompleted)
                {
                    // Jobs can't have references otherwise I'd just do this in the libraryUpdateJob Execute
                    // The Tracked Image Manager will disable when it doesn't have a library which will be the case
                    // for the P2D host.  We need to enable it again after we have populated a library.
                    m_TrackedImageManager.enabled = true;
                    m_LibraryUpdateInProgress = false;
                }
            }
        }
    }
}
