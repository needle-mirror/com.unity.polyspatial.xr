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
                    foreach (var change in PolySpatialArgs.ExtractChangeListFromArgs<PolySpatialColliderData>(argCount, argValues, argSizes))
                    {
                        var id = change.objectData.instanceId;
                        var viewSubgraph = m_UnityBackend.SceneGraph.ViewSubgraphs[id.viewSubgraphIndex];
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.IidToEntity[id.id];
                            var interactableProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                            interactableProxy.Initialize(id, viewSubgraph.VolumeView.ViewId, viewSubgraph.RootGameObject.transform.parent);
                        }
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
                        var viewSubgraph = m_UnityBackend.SceneGraph.ViewSubgraphs[id.viewSubgraphIndex];
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.IidToEntity[id.id];
                            var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                                eventSystemProxy.Initialize(id, viewSubgraph.VolumeView.ViewId, viewSubgraph.RootGameObject.transform.parent);
                        }
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
                        var viewSubgraph = m_UnityBackend.SceneGraph.ViewSubgraphs[id.viewSubgraphIndex];
                        if (viewSubgraph.VolumeView != null)
                        {
                            var entity = viewSubgraph.IidToEntity[id.id];
                            var eventSystemProxy = entity.UnitySceneGraphGameObject.GetOrAddBackingComponent<XRInteractableProxy>();
                            eventSystemProxy.Initialize(id, viewSubgraph.VolumeView.ViewId, viewSubgraph.RootGameObject.transform.parent);
                        }
                    }
                    break;
                }
            }

            NextHandler.HandleCommand(cmdHeader, argCount, argValues, argSizes);
        }

        [CommandHandlerCreationCallback(stage: CommandHandlerGraph.Stage.PostLocalBackend)]
        static XRPostUnityBackendCommandHandler Create(CommandHandlerGraph.HandlerCreationContext context)
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
