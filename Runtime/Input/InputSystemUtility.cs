using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.PolySpatial.XR.Input
{
    /// <summary>
    /// Utility class for working with the Unity Input System.
    /// </summary>
    public static class InputSystemUtility
    {
        /// <summary>
        /// Subscribes performed and cancelled callbacks to an InputActionReference.
        /// </summary>
        /// <param name="reference">The input action reference to subscribe to.</param>
        /// <param name="performed">The action to be called when the input action is made.</param>
        /// <param name="canceled">The action to be called when the input action is canceled.</param>
        public static void Subscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            Debug.Assert(action != null);
            if (performed != null)
                action.performed += performed;
            if (canceled != null)
                action.canceled += canceled;
        }

        /// <summary>
        /// Unsubscribes performed and cancelled callbacks from an InputActionReference.
        /// </summary>
        /// <param name="reference">The input action reference to unsubscribe from.</param>
        /// <param name="performed">The action to unsubscribe when the input action reference was performed.</param>
        /// <param name="canceled">The action to unsubscribe when the input action reference was canceled.</param>
        public static void Unsubscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            Debug.Assert(action != null);
            if (performed != null)
                action.performed -= performed;
            if (canceled != null)
                action.canceled -= canceled;
        }

        /// <summary>
        /// Gets a reference to the InputAction from an InputActionReference.
        /// </summary>
        /// <param name="actionReference">The input action reference to obtain the input action from.</param>
        /// <returns>The input action of the action reference.</returns>
        public static InputAction GetInputAction(InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}
