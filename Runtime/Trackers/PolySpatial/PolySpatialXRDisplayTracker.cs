using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial.XR.Internals
{
    class PolySpatialXRDisplayTracker
    {
        XRDisplaySubsystem m_DisplaySubsystem;

        // The data we've sent for the display.
        PolySpatialXRDisplayData m_Data = new();

        internal void Update()
        {
            if (m_DisplaySubsystem == null)
            {
                m_DisplaySubsystem =
                    XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
                if (m_DisplaySubsystem == null)
                    return;
            }
            // We need a camera (any camera) to retrieve the render and culling parameters.
            var camera = Camera.main;
            if (camera == null)
                return;

            var dirty = false;
            if (m_Data.running != m_DisplaySubsystem.running)
            {
                m_Data.running = m_DisplaySubsystem.running;
                dirty = true;
            }

            var displayIsTransparent = !m_DisplaySubsystem.displayOpaque;
            if (m_Data.displayIsTransparent != displayIsTransparent)
            {
                m_Data.displayIsTransparent = displayIsTransparent;
                dirty = true;
            }

            var renderPassCount = m_DisplaySubsystem.GetRenderPassCount();
            var cullingPassCount = 0;
            if (m_Data.renderPasses?.Count != renderPassCount)
            {
                m_Data.renderPasses = CreateDefaultConstructedArray<PolySpatialXRRenderPassData>(renderPassCount);
                dirty = true;
            }
            for (var i = 0; i < renderPassCount; ++i)
            {
                m_DisplaySubsystem.GetRenderPass(i, out var renderPass);
                var renderPassData = m_Data.renderPasses[i];

                if (renderPassData.cullingPassIndex != renderPass.cullingPassIndex)
                {
                    renderPassData.cullingPassIndex = renderPass.cullingPassIndex;
                    dirty = true;
                }
                cullingPassCount = Math.Max(cullingPassCount, renderPass.cullingPassIndex + 1);

                var renderTargetDesc = renderPass.renderTargetDesc;
                var renderTargetData = renderPassData.renderTarget;

                PolySpatialXRRenderTextureFormat colorFormat;
                PolySpatialXRRenderTargetFlags flags = 0;
                switch (renderTargetDesc.graphicsFormat)
                {
                    case GraphicsFormat.R8G8B8A8_SRGB:
                        flags |= PolySpatialXRRenderTargetFlags.SRGB;
                        colorFormat = PolySpatialXRRenderTextureFormat.RGBA32;
                        break;

                    case GraphicsFormat.R8G8B8A8_UNorm or GraphicsFormat.R8G8B8A8_SNorm:
                    case GraphicsFormat.R8G8B8A8_UInt or GraphicsFormat.R8G8B8A8_SInt:
                        colorFormat = PolySpatialXRRenderTextureFormat.RGBA32;
                        break;

                    case GraphicsFormat.B8G8R8A8_SRGB:
                        flags |= PolySpatialXRRenderTargetFlags.SRGB;
                        colorFormat = PolySpatialXRRenderTextureFormat.BGRA32;
                        break;

                    case GraphicsFormat.B8G8R8A8_UNorm or GraphicsFormat.B8G8R8A8_SNorm:
                    case GraphicsFormat.B8G8R8A8_UInt or GraphicsFormat.B8G8R8A8_SInt:
                        colorFormat = PolySpatialXRRenderTextureFormat.BGRA32;
                        break;

                    case GraphicsFormat.R5G6B5_UNormPack16:
                        colorFormat = PolySpatialXRRenderTextureFormat.RGB565;
                        break;

                    case GraphicsFormat.R16G16B16A16_SFloat:
                        colorFormat = PolySpatialXRRenderTextureFormat.R16G16B16A16_SFloat;
                        break;

                    case GraphicsFormat.A2R10G10B10_UNormPack32 or GraphicsFormat.A2R10G10B10_UIntPack32:
                    case GraphicsFormat.A2R10G10B10_SIntPack32 or GraphicsFormat.A2R10G10B10_XRUNormPack32:
                        colorFormat = PolySpatialXRRenderTextureFormat.RGBA1010102;
                        break;

                    case GraphicsFormat.A2R10G10B10_XRSRGBPack32:
                        flags |= PolySpatialXRRenderTargetFlags.SRGB;
                        colorFormat = PolySpatialXRRenderTextureFormat.RGBA1010102;
                        break;

                    case GraphicsFormat.A2B10G10R10_UNormPack32 or GraphicsFormat.A2B10G10R10_UIntPack32:
                    case GraphicsFormat.A2B10G10R10_SIntPack32:
                        colorFormat = PolySpatialXRRenderTextureFormat.BGRA1010102;
                        break;

                    case GraphicsFormat.B10G11R11_UFloatPack32:
                        colorFormat = PolySpatialXRRenderTextureFormat.R11G11B10_UFloat;
                        break;

                    case GraphicsFormat.R10G10B10_XRUNormPack32:
                        colorFormat = PolySpatialXRRenderTextureFormat.BGR101010;
                        break;

                    case GraphicsFormat.R10G10B10_XRSRGBPack32:
                        flags |= PolySpatialXRRenderTargetFlags.SRGB;
                        colorFormat = PolySpatialXRRenderTextureFormat.BGR101010;
                        break;

                    case GraphicsFormat.None:
                        colorFormat = PolySpatialXRRenderTextureFormat.None;
                        break;

                    default:
                        Logging.LogWarning(
                            LogCategory.XR, "Unsupported render target format: " + renderTargetDesc.graphicsFormat);
                        colorFormat = PolySpatialXRRenderTextureFormat.None;
                        break;
                }

                PolySpatialXRDepthTextureFormat depthFormat = renderTargetDesc.depthBufferBits switch
                {
                    0 => PolySpatialXRDepthTextureFormat.None,
                    16 => PolySpatialXRDepthTextureFormat.Use16bit,
                    _ => PolySpatialXRDepthTextureFormat.Use24bitOrGreater,
                };

                dirty |=
                    PolySpatialUtils.TryUpdateEnumValue(colorFormat, ref renderTargetData.colorFormat) |
                    PolySpatialUtils.TryUpdateEnumValue(depthFormat, ref renderTargetData.depthFormat) |
                    PolySpatialUtils.TryUpdateValue(renderTargetDesc.width, ref renderTargetData.width) |
                    PolySpatialUtils.TryUpdateValue(renderTargetDesc.height, ref renderTargetData.height) |
                    PolySpatialUtils.TryUpdateValue(
                        renderTargetDesc.volumeDepth, ref renderTargetData.textureArrayLength) |
                    PolySpatialUtils.TryUpdateEnumValue(flags, ref renderTargetData.flags);

                renderPassData.renderTarget = renderTargetData;

                var renderParameterCount = renderPass.GetRenderParameterCount();
                if (renderPassData.renderParameters?.Count != renderParameterCount)
                {
                    renderPassData.renderParameters = new PolySpatialXRRenderParameterData[renderParameterCount];
                    dirty = true;
                }
                for (var j = 0; j < renderParameterCount; ++j)
                {
                    renderPass.GetRenderParameter(camera, j, out var renderParameter);
                    var renderParameterData = renderPassData.renderParameters[j];

                    // Inverting the view matrix gives us cameraToWorldMatrix * eyeToCameraMatrix, which we then
                    // premultiply by worldToCameraMatrix to get eyeToCameraMatrix, from which we extract the pose.
                    var eyeToCameraMatrix = camera.worldToCameraMatrix * renderParameter.view.inverse;
                    var deviceAnchorToEyePose = new Pose(eyeToCameraMatrix.GetPosition(), eyeToCameraMatrix.rotation);

                    // Use looser-than-default comparison for the pose, since computing it from the view matrix
                    // introduces imprecision.
                    if (!(MathExtensions.ApproximatelyEqual(
                            deviceAnchorToEyePose.position, renderParameterData.deviceAnchorToEyePose.position, 0.001f) &&
                        MathExtensions.ApproximatelyEqual(
                            deviceAnchorToEyePose.rotation, renderParameterData.deviceAnchorToEyePose.rotation)))
                    {
                        renderParameterData.deviceAnchorToEyePose = deviceAnchorToEyePose;
                        dirty = true;
                    }

                    // The projection matrix is also imprecise.
                    if (!MathExtensions.ApproximatelyEqual(
                            renderParameter.projection, renderParameterData.projection, 0.001f))
                    {
                        renderParameterData.projection = renderParameter.projection;
                        dirty = true;
                    }

                    dirty |=
                        PolySpatialUtils.TryUpdateValue(
                            renderParameter.textureArraySlice, ref renderParameterData.textureArraySlice) |
                        PolySpatialUtils.TryUpdateValue(
                            renderParameter.viewport, ref renderParameterData.viewport);

                    renderPassData.renderParameters[j] = renderParameterData;
                }
            }

            if (m_Data.cullingPasses?.Count != cullingPassCount)
            {
                m_Data.cullingPasses = new PolySpatialXRCullingPassData[cullingPassCount];
                dirty = true;
            }
            for (var i = 0; i < cullingPassCount; ++i)
            {
                m_DisplaySubsystem.GetCullingParameters(camera, i, out var cullingPass);
                var cullingPassData = m_Data.cullingPasses[i];

                // Inverting the view matrix gives us cameraToWorldMatrix * eyeToCameraMatrix, which we then
                // premultiply by worldToCameraMatrix to get eyeToCameraMatrix, from which we extract the pose.
                var eyeToCameraMatrix = camera.worldToCameraMatrix * cullingPass.stereoViewMatrix.inverse;
                var deviceAnchorToCullingPose = new Pose(eyeToCameraMatrix.GetPosition(), eyeToCameraMatrix.rotation);

                // Use looser-than-default comparison for the pose, since computing it from the view matrix
                // introduces imprecision.
                if (!(MathExtensions.ApproximatelyEqual(
                        deviceAnchorToCullingPose.position, cullingPassData.deviceAnchorToCullingPose.position, 0.001f) &&
                    MathExtensions.ApproximatelyEqual(
                        deviceAnchorToCullingPose.rotation, cullingPassData.deviceAnchorToCullingPose.rotation)))
                {
                    cullingPassData.deviceAnchorToCullingPose = deviceAnchorToCullingPose;
                    dirty = true;
                }

                // The projection matrix is also imprecise.
                if (!MathExtensions.ApproximatelyEqual(
                        cullingPass.stereoProjectionMatrix, cullingPassData.projection, 0.001f))
                {
                    cullingPassData.projection = cullingPass.stereoProjectionMatrix;
                    dirty = true;
                }

                dirty |=
                    PolySpatialUtils.TryUpdateValue(
                        cullingPass.stereoSeparationDistance, ref cullingPassData.separation);

                m_Data.cullingPasses[i] = cullingPassData;
            }

            if (dirty)
                HostCommandHelper.SendXRDisplayData(m_Data);
        }

        static T[] CreateDefaultConstructedArray<T>(int length) where T : new()
        {
            return Enumerable.Range(0, length).Select(_ => new T()).ToArray();
        }

        internal void StartSession(PolySpatialHostID hostID)
        {
            // Send current data immediately on session start, in addition to whenever it changes.
            if (m_Data.running)
                HostCommandHelper.SendXRDisplayData(m_Data);
        }
    }
}