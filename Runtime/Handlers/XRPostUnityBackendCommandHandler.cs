#if HAS_XR_INTERACTION_TOOLKIT
using Unity.PolySpatial.Internals;
using Unity.PolySpatial.Shell.InputDevices;

namespace Unity.PolySpatial.XR.Internals
{
    class XRPostUnityBackendCommandHandler : IPolySpatialChainableCommandHandler
    {
        public IPolySpatialCommandHandler NextHandler { get; set; }

        PolySpatialUnityBackend m_UnityBackend;

        internal XRPostUnityBackendCommandHandler(PolySpatialUnityBackend unityBackend)
        {
            m_UnityBackend = unityBackend;
        }

        public unsafe void HandleCommand(PolySpatialCommandHeader cmdHeader, int argCount, void** argValues, int* argSizes)
        {
            switch (cmdHeader.Command)
            {
                case PolySpatialCommand.CreateOrUpdateCollider:
                {
                    var viewSubgraph = m_UnityBackend.SceneGraph.GetViewSubgraph(cmdHeader.viewSubgraphIndex);
                    foreach (var change in PolySpatialArgs.ExtractChangeListFromArgs<PolySpatialColliderData>(argCount, argValues, argSizes))
                    {
                        var id = change.objectData.entityId;
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.GetEntity(id);
                            var interactableProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                            interactableProxy.Initialize(
                                id, viewSubgraph.VolumeView.ViewId, viewSubgraph.ConnectionID,
                                viewSubgraph.RootGameObject.transform.parent);
                        }
                    }
                    break;
                }
                case PolySpatialCommand.CreateOrUpdateCanvasRenderer:
                {
                    var viewSubgraph = m_UnityBackend.SceneGraph.GetViewSubgraph(cmdHeader.viewSubgraphIndex);
                    foreach (var change in PolySpatialArgs.ExtractChangeListSerializedFromArgs<PolySpatialCanvasRendererData>(argCount, argValues, argSizes))
                    {
                        if (!change.engineData.clipRect.HasValue)
                            continue;

                        var id = change.objectData.entityId;
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.GetEntity(id);
                            var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                                eventSystemProxy.Initialize(
                                id, viewSubgraph.VolumeView.ViewId, viewSubgraph.ConnectionID,
                                viewSubgraph.RootGameObject.transform.parent);
                        }
                    }
                    break;
                }
                case PolySpatialCommand.CreateOrUpdateUIGraphic:
                {
                    var viewSubgraph = m_UnityBackend.SceneGraph.GetViewSubgraph(cmdHeader.viewSubgraphIndex);
                    foreach (var change in PolySpatialArgs.ExtractChangeListFromArgs<PolySpatialUIGraphicData>(argCount, argValues, argSizes))
                    {
                        if (!change.engineData.raycastTarget)
                            continue;

                        var id = change.objectData.entityId;
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.GetEntity(id);
                            var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                            eventSystemProxy.Initialize(
                                id, viewSubgraph.VolumeView.ViewId, viewSubgraph.ConnectionID,
                                viewSubgraph.RootGameObject.transform.parent);
                        }
                    }
                    break;
                }
            }

            NextHandler.HandleCommand(cmdHeader, argCount, argValues, argSizes);
        }

        internal static XRPostUnityBackendCommandHandler Create(CommandHandlerGraph.HandlerCreationContext context)
        {
            if (context.HasNetworkAppHost && context.CommandHandlerGraph.LocalBackend is PolySpatialUnityBackend unityBackend)
            {
                return new XRPostUnityBackendCommandHandler(unityBackend);
            }

            return null;
        }
    }
}
#endif
