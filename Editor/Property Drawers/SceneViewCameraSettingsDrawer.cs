using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(SceneViewCameraSettings))]
    internal class SceneViewCameraSettingsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement inspector = new();

            SerializedProperty enabledProp = property.FindPropertyRelative("m_Enabled");
            Toggle toggle = new(property.displayName);
            toggle.AddToClassList("unity-base-field__aligned");
            toggle.BindProperty(enabledProp);
            inspector.Add(toggle);

            VisualElement content = new();
            content.AddToClassList("indented-property");
            inspector.Add(content);

            content.Add(new PropertyField(property.FindPropertyRelative("m_CameraMode")));
            content.Add(new PropertyField(property.FindPropertyRelative("m_FocusMode")));

            VisualElement manualGroup = new();
            manualGroup.Add(new PropertyField(property.FindPropertyRelative("m_Orthographic")));
            manualGroup.Add(new PropertyField(property.FindPropertyRelative("m_Pivot")));

            SerializedProperty rotationProp = property.FindPropertyRelative("m_Rotation");
            Vector3Field rotationField = new(rotationProp.displayName);
            rotationField.AddToClassList("unity-base-field__aligned");
            rotationField.RegisterValueChangedCallback(evt =>
            {
                rotationProp.quaternionValue = Quaternion.Euler(evt.newValue);
                rotationProp.serializedObject.ApplyModifiedProperties();
            });
            rotationField.TrackPropertyValue(rotationProp, p =>
                rotationField.SetValueWithoutNotify(p.quaternionValue.eulerAngles));
            manualGroup.Add(rotationField);

            manualGroup.Add(new PropertyField(property.FindPropertyRelative("m_Size")));

            Button storeCurrentBtn = new(() =>
            {
                SceneView sceneView = EditorWindow.GetWindow<SceneView>();
                property.FindPropertyRelative("m_CameraMode").intValue = sceneView.in2DMode
                    ? (int)SceneViewCameraMode.SceneView2D
                    : (int)SceneViewCameraMode.SceneView3D;
                property.FindPropertyRelative("m_Orthographic").boolValue = sceneView.orthographic;
                property.FindPropertyRelative("m_Size").floatValue = sceneView.size;
                property.FindPropertyRelative("m_Pivot").vector3Value = sceneView.pivot;
                property.FindPropertyRelative("m_Rotation").quaternionValue = sceneView.rotation;
                property.serializedObject.ApplyModifiedProperties();
            }) { text = "Use Settings From Current Scene View" };
            manualGroup.Add(storeCurrentBtn);

            VisualElement frameObjectGroup = new();
            frameObjectGroup.Add(new PropertyField(property.FindPropertyRelative("m_FrameObject")));

            content.Add(manualGroup);
            content.Add(frameObjectGroup);

            inspector.TrackPropertyValue(enabledProp, _ => UpdateVisibility());
            inspector.TrackPropertyValue(property.FindPropertyRelative("m_FocusMode"), _ => UpdateVisibility());
            inspector.RegisterCallback<AttachToPanelEvent>(_ => UpdateVisibility());

            return inspector;

            void UpdateVisibility()
            {
                content.style.display = enabledProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                SceneViewFocusMode focusMode =
                    (SceneViewFocusMode)property.FindPropertyRelative("m_FocusMode").enumValueIndex;
                manualGroup.style.display =
                    focusMode == SceneViewFocusMode.Manual ? DisplayStyle.Flex : DisplayStyle.None;
                frameObjectGroup.style.display = focusMode == SceneViewFocusMode.FrameObject
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }
    }
}
