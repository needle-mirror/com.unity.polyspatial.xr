using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Unity.PolySpatial.XR.Input
{
    /// <summary>
    /// Can subscribe to a WorldTouch event from the InputSystem and directly
    /// forward it to XRI interactable components via the ColliderId in the
    /// WorldTouchState struct. This evades re-raycasting inside the app
    /// to determine what collider was interacted with.
    /// </summary>
    public class XRTouchSpaceInteractor : XRBaseInteractor
    {
        [SerializeField]
        InputActionReference m_WorldTouch;

        [SerializeField]
        InputActionReference m_Touch;

        WorldTouchState m_WorldPositionState;
        TouchState m_TouchState;

        protected override void Start()
        {
            base.Start();
            InputSystemUtility.Subscribe(m_WorldTouch, OnWorldTouchPerformed, OnWorldTouchCancelled);
            InputSystemUtility.Subscribe(m_Touch, OnTouchPerformed, OnTouchCancelled);
        }

        protected override void OnDestroy()
        {
            InputSystemUtility.Unsubscribe(m_WorldTouch, OnWorldTouchPerformed, OnWorldTouchCancelled);
            InputSystemUtility.Unsubscribe(m_Touch, OnTouchPerformed, OnTouchCancelled);
            base.OnDestroy();
        }

        void OnWorldTouchPerformed(InputAction.CallbackContext context)
        {
            m_WorldPositionState = context.ReadValue<WorldTouchState>();
            transform.position = m_WorldPositionState.worldPosition;
        }

        void OnWorldTouchCancelled(InputAction.CallbackContext context)
        {
            m_WorldPositionState = context.ReadValue<WorldTouchState>();
        }

        void OnTouchPerformed(InputAction.CallbackContext context)
        {
            m_TouchState = context.ReadValue<TouchState>();
        }

        void OnTouchCancelled(InputAction.CallbackContext context)
        {
            m_TouchState = context.ReadValue<TouchState>();
        }

        public override bool isSelectActive
        {
            get
            {
                switch (m_TouchState.phase)
                {
                    case TouchPhase.Began:
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        return base.isSelectActive;
                    case TouchPhase.Canceled:
                    case TouchPhase.Ended:
                    case TouchPhase.None:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();
            switch (m_TouchState.phase)
            {
                case TouchPhase.None:
                    break;
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                case TouchPhase.Canceled:
                case TouchPhase.Ended:
                    if (TryGetInteractable(m_WorldPositionState.colliderId, out var interactable))
                        targets.Add(interactable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        bool TryGetInteractable(int colliderId, out XRBaseInteractable interactable)
        {
            // Must get GO but seems can get collider directly at some point once PolySpatialInstanceIds of components are stored
            var go = ObjectBridge.FindObjectFromInstanceID(colliderId) as GameObject;
            if (go == null)
            {
                interactable = null;
                return false;
            }

            interactable = go.GetComponent<XRBaseInteractable>();
            return interactable != null;
        }
    }
}
