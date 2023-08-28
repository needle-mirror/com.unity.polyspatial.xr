using Unity.PolySpatial.XR.Input;

namespace UnityEditor.PolySpatial.XR.Input
{
    [CustomEditor(typeof(XRTouchSpaceInteractor))]
    public class XRTouchSpaceInteractorEditor : Editor
    {
        SerializedProperty m_TouchProperty;
        SerializedProperty m_WorldTouchProperty;

        void OnEnable()
        {
            m_TouchProperty = serializedObject.FindProperty("m_Touch");
            m_WorldTouchProperty = serializedObject.FindProperty("m_WorldTouch");
        }

        public override void OnInspectorGUI()
        {
            // I am hiding everything but WorldTouch and Touch fields as I don't
            // see why someone would want to edit the others in this
            // use case as this is essentially like an input passthrough
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_TouchProperty);
            EditorGUILayout.PropertyField(m_WorldTouchProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
