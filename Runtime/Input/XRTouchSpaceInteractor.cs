#if HAS_XR_INTERACTION_TOOLKIT
using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Serialization;


namespace Unity.PolySpatial.XR.Input
{
    /// <summary>
    /// Can subscribe to a WorldTouch event from the InputSystem and directly
    /// forward it to XRI interactable components via the ColliderId in the
    /// WorldTouchState struct. This evades re-raycasting inside the app
    /// to determine what collider was interacted with.
    /// </summary>
    public class XRTouchSpaceInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor
    {
        [FormerlySerializedAs("m_WorldTouch")]
        [SerializeField]
        InputActionReference m_SpatialPointer;

        SpatialPointerState m_SpatialPointerState;

        protected override void Start()
        {
            base.Start();
            InputSystemUtility.Subscribe(m_SpatialPointer, OnWorldTouchPerformed, OnWorldTouchCancelled);
        }

        protected override void OnDestroy()
        {
            InputSystemUtility.Unsubscribe(m_SpatialPointer, OnWorldTouchPerformed, OnWorldTouchCancelled);
            base.OnDestroy();
        }

        void OnWorldTouchPerformed(InputAction.CallbackContext context)
        {
            m_SpatialPointerState = context.ReadValue<SpatialPointerState>();
            transform.position = m_SpatialPointerState.interactionPosition;
            transform.rotation = m_SpatialPointerState.inputDeviceRotation;
        }

        void OnWorldTouchCancelled(InputAction.CallbackContext context)
        {
            m_SpatialPointerState = context.ReadValue<SpatialPointerState>();
        }

        public override bool isSelectActive
        {
            get
            {
                switch (m_SpatialPointerState.phase)
                {
                    case SpatialPointerPhase.Began:
                    case SpatialPointerPhase.Moved:
                        return base.isSelectActive;
                    case SpatialPointerPhase.Ended:
                    case SpatialPointerPhase.None:
                    case SpatialPointerPhase.Cancelled:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool CanHover(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override void GetValidTargets(List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable> targets)
        {
            targets.Clear();
            switch (m_SpatialPointerState.phase)
            {
                case SpatialPointerPhase.None:
                    break;
                case SpatialPointerPhase.Began:
                case SpatialPointerPhase.Moved:
                case SpatialPointerPhase.Cancelled:
                case SpatialPointerPhase.Ended:
                    if (TryGetInteractable(m_SpatialPointerState.targetId, out var interactable))
                        targets.Add(interactable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static bool TryGetInteractable(long colliderId, out UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
        {
            // Must get GO but seems can get collider directly at some point once PolySpatialInstanceIds of components are stored
            var go = ObjectBridge.FindObjectFromInstanceID(colliderId) as GameObject;
            if (go == null)
            {
                interactable = null;
                return false;
            }

            interactable = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            return interactable != null;
        }
    }
}
#endif
