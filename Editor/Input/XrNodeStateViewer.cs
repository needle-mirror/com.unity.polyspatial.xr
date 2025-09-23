#if POLYSPATIAL_INTERNAL
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace Unity.PolySpatial.XR.Internals.Input
{
    public class XrNodeStateViewer : EditorWindow
    {
        [MenuItem("Window/PolySpatial/Xr Node State Viewer")]
        private static void ShowWindow()
        {
            var window = GetWindow<XrNodeStateViewer>();
            window.titleContent = new GUIContent("Xr Node State Viewer");
            window.Show();
        }

        void Update()
        {
            // This is a HORRIBLE ui. Maybe someone will make a better one later.
            rootVisualElement.Clear();
            var nodeStates = new List<XRNodeState>();
            UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);
            if (nodeStates.Count == 0)
            {
                rootVisualElement.Add(new Label("No states"));
                return;
            }

            foreach (var nodeState in nodeStates)
            {
                rootVisualElement.Add(new Label($"Node Type: {nodeState.nodeType}"));
                rootVisualElement.Add(new Label($"Tracked: {nodeState.tracked}"));
                if (nodeState.TryGetPosition(out var position))
                {
                    rootVisualElement.Add(new Label($"Position: {position}"));
                }
                else
                {
                    rootVisualElement.Add(new Label($"No Position Available"));
                }

                if (nodeState.TryGetRotation(out var rotation))
                {
                    rootVisualElement.Add(new Label($"Rotation: {rotation.eulerAngles}"));
                }
                else
                {
                    rootVisualElement.Add(new Label($"No Rotation Available"));
                }
            }
        }


        private void CreateGUI()
        {
        }
    }
}
#endif
