#if HAS_XR_INTERACTION_TOOLKIT
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Shell.InputDevices;
using UnityEngine;

namespace Unity.PolySpatial.XR.Internals
{
    // Ensure it subscribes before PolySpatialNetworkAppHostBase OnEnable
    [DefaultExecutionOrder(-10)]
    class XRPostUnityBackendCommandHandler : MonoBehaviour, IPolySpatialCommandHandler
    {
        PolySpatialUnityBackend m_UnityBackend;

        void Awake()
        {
            PolySpatialNetworkAppHostBase.OnEnableCommandHandlers += AddCommandHandler;
        }

        void AddCommandHandler(PolySpatialNetworkAppHostBase appHost, IPolySpatialLocalBackend backend)
        {
            if (backend is PolySpatialUnityBackend unityBackend)
            {
                m_UnityBackend = unityBackend;
                unityBackend.NextHandler = this;
            }
        }

        public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
        {
            switch (cmd)
            {
                case PolySpatialCommand.CreateOrUpdateCollider:
                {
                    foreach (var change in PolySpatialArgs.ExtractChangeListFromArgs<PolySpatialColliderData>(argCount, argValues, argSizes))
                    {
                        var id = change.objectData.instanceId;
                        var volume = m_UnityBackend.SceneGraph.GetVolumeForIndex(id.hostVolumeIndex);
                        var entity = volume.IdToEntity[id.id];
                        var interactableProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                        interactableProxy.Initialize(id, volume.VolumeId);
                    }
                    break;
                }
                case PolySpatialCommand.CreateOrUpdateCanvasRenderer:
                {
                    foreach (var change in PolySpatialArgs.ExtractChangeListSerializedFromArgs<PolySpatialCanvasRendererData>(argCount, argValues, argSizes))
                    {
                        if (!change.engineData.clipRect.HasValue)
                            continue;

                        var id = change.objectData.instanceId;
                        var volume = m_UnityBackend.SceneGraph.GetVolumeForIndex(id.hostVolumeIndex);
                        var entity = volume.IdToEntity[id.id];
                        var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                        eventSystemProxy.Initialize(id, volume.VolumeId);
                    }
                    break;
                }
                case PolySpatialCommand.CreateOrUpdateUIGraphic:
                {
                    foreach (var change in PolySpatialArgs.ExtractChangeListFromArgs<PolySpatialUIGraphicData>(argCount, argValues, argSizes))
                    {
                        if (!change.engineData.raycastTarget)
                            continue;

                        var id = change.objectData.instanceId;
                        var volume = m_UnityBackend.SceneGraph.GetVolumeForIndex(id.hostVolumeIndex);
                        var entity = volume.IdToEntity[id.id];
                        var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                        eventSystemProxy.Initialize(id, volume.VolumeId);
                    }
                    break;
                }
            }
        }
    }
}
#endif
