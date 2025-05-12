#if HAS_XR_INTERACTION_TOOLKIT
using Unity.Collections;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Unity.PolySpatial.Shell.InputDevices
{
    /// <summary>
    /// Receive events from XRI and translate them into PolySpatialInputEvents.
    /// </summary>
    internal class XRInteractableProxy : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
    {
        PolySpatialInstanceID m_ColliderID;
        PolySpatialInstanceID m_VolumeID;
        Transform m_VolumeParent;

        internal PolySpatialInstanceID ColliderID => m_ColliderID;

        internal void Initialize(PolySpatialInstanceID colliderID, PolySpatialInstanceID volumeID, Transform volumeParent)
        {
            Debug.Assert(colliderID.IsValid());
            Debug.Assert(volumeID.IsValid());
            m_ColliderID = colliderID;
            m_VolumeID = volumeID;
            m_VolumeParent = volumeParent;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (CreateSpatialPointerEvent(args.interactorObject,  PolySpatialPointerPhase.Began, out var spatialPointerEvent))
            {
                using (var events = ConstructEvents(spatialPointerEvent))
                {
                    PushEvents(events);
                }
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (CreateSpatialPointerEvent(args.interactorObject,  PolySpatialPointerPhase.Ended, out var spatialPointerEvent))
            {
                using (var events = ConstructEvents(spatialPointerEvent))
                {
                    PushEvents(events);
                }
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            switch (updatePhase)
            {
                case XRInteractionUpdateOrder.UpdatePhase.Fixed:
                    break;
                case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                {
                    if (interactorsSelecting.Count == 0)
                        break;
                    using (var events = new NativeList<PolySpatialPointerEvent>(interactorsSelecting.Count, Allocator.Temp))
                    {
                        for (var i = 0; i < interactorsSelecting.Count; ++i)
                        {
                            if (CreateSpatialPointerEvent(interactorsSelecting[i], PolySpatialPointerPhase.Moved, out var spatialPointerEvent))
                                events.Add(spatialPointerEvent);
                        }

                        PushEvents(events.AsArray());
                    }
                    break;
                }
                case XRInteractionUpdateOrder.UpdatePhase.Late:
                    break;
            }
        }

        unsafe void PushEvents(NativeArray<PolySpatialPointerEvent> events)
        {
            var hostId = m_ColliderID.hostId;
            var type = PolySpatialInputType.Pointer;
            PolySpatialCore.CommandHandlerGraph.MulticastHostHandler?.HostCommand(PolySpatialHostCommand.InputEvent, &type, &hostId, events.AsSpan());
        }

        NativeArray<PolySpatialPointerEvent> ConstructEvents(PolySpatialPointerEvent pointerEvent)
        {
            var events = new NativeArray<PolySpatialPointerEvent>(1, Allocator.Temp);
            events[0] = pointerEvent;
            return events;
        }

        bool CreateSpatialPointerEvent(IXRSelectInteractor interactor, PolySpatialPointerPhase phase, out PolySpatialPointerEvent pointerEvent)
        {
            pointerEvent = default;
            switch (interactor)
            {
                case XRRayInteractor rayInteractor:
                    if (!rayInteractor.TryGetHitInfo(out var worldPosition, out var worldNormal, out var positionInLine, out var isValidTarget) || !isValidTarget)
                        return false;
                    var rayInteractorTransform = rayInteractor.transform;
                    pointerEvent = new PolySpatialPointerEvent
                    {
                        targetId = m_ColliderID,
                        volumeId = m_VolumeID,
                        interactionId = rayInteractor.GetInstanceID(),
                        phase = phase,
                        interactionPosition = worldPosition,
                        inputDevicePosition = rayInteractorTransform.position,
                        inputDeviceRotation = rayInteractorTransform.rotation,
                        interactionRayOrigin = rayInteractor.rayOriginTransform.position,
                        interactionRayDirection = rayInteractor.rayOriginTransform.forward,
                        kind = PolySpatialPointerKind.IndirectPinch
                    };
                    break;
                case NearFarInteractor nearFarInteractor:
                    if (nearFarInteractor is not IXRRayProvider rayProvider)
                        return false;
                    var nearFarInteractorTransform = nearFarInteractor.transform;
                    pointerEvent = new PolySpatialPointerEvent
                    {
                        targetId = m_ColliderID,
                        volumeId = m_VolumeID,
                        interactionId = nearFarInteractor.GetInstanceID(),
                        phase = phase,
                        interactionPosition = rayProvider.rayEndPoint,
                        inputDevicePosition = nearFarInteractorTransform.position,
                        inputDeviceRotation = nearFarInteractorTransform.rotation,
                    };
                    // nearFarInteractor.selectionRegion sometimes returns none when it probably shouldn't, so determine like this
                    var region = nearFarInteractor.interactionAttachController.hasOffset ? NearFarInteractor.Region.Far : NearFarInteractor.Region.Near;
                    switch (region)
                    {
                        default:
                        case NearFarInteractor.Region.None:
                            return false;
                        case NearFarInteractor.Region.Near:
                            pointerEvent.interactionRayOrigin = nearFarInteractor.nearInteractionCaster.effectiveCastOrigin.position;
                            pointerEvent.interactionRayDirection = nearFarInteractor.nearInteractionCaster.effectiveCastOrigin.forward;
                            pointerEvent.kind = PolySpatialPointerKind.DirectPinch;
                            break;
                        case NearFarInteractor.Region.Far:
                            pointerEvent.interactionRayOrigin = nearFarInteractor.farInteractionCaster.effectiveCastOrigin.position;
                            pointerEvent.interactionRayDirection = nearFarInteractor.farInteractionCaster.effectiveCastOrigin.forward;
                            pointerEvent.kind = PolySpatialPointerKind.IndirectPinch;
                            break;
                    }
                    break;
                case XRPokeInteractor pokeInteractor:
                    if (!pokeInteractor.pokeStateData.Value.meetsRequirements)
                        return false;
                    var pokeInteractorTransform = pokeInteractor.transform;
                    var pokeInteractorAttachTransform = pokeInteractor.GetAttachTransform(null);
                    pointerEvent = new PolySpatialPointerEvent
                    {
                        targetId = m_ColliderID,
                        volumeId = m_VolumeID,
                        interactionId = pokeInteractor.GetInstanceID(),
                        phase = phase,
                        interactionPosition = pokeInteractor.pokeStateData.Value.pokeInteractionPoint,
                        inputDevicePosition = pokeInteractorTransform.position,
                        inputDeviceRotation = pokeInteractorTransform.rotation,
                        interactionRayOrigin = pokeInteractorAttachTransform.position,
                        interactionRayDirection = pokeInteractorAttachTransform.forward,
                        kind = PolySpatialPointerKind.Touch
                    };
                    break;
                default:
                    return false;
            }

            if (m_VolumeParent != null)
            {
                var worldToVolume = m_VolumeParent.worldToLocalMatrix;
                var worldToVolumeRotation = Quaternion.Inverse(m_VolumeParent.rotation);

                pointerEvent.interactionPosition = worldToVolume.MultiplyPoint3x4(pointerEvent.interactionPosition);
                pointerEvent.inputDevicePosition = worldToVolume.MultiplyPoint3x4(pointerEvent.inputDevicePosition);
                pointerEvent.interactionRayOrigin = worldToVolume.MultiplyPoint3x4(pointerEvent.interactionRayOrigin);
                pointerEvent.interactionRayDirection = worldToVolumeRotation * pointerEvent.interactionRayDirection;
                pointerEvent.inputDeviceRotation = worldToVolumeRotation * pointerEvent.inputDeviceRotation;
            }

            return true;
        }
    }
}
#endif
