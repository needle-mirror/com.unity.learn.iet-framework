using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(UnmaskedView))]
    internal class UnmaskedViewDrawer : PropertyDrawer
    {
        private const string k_SelectorTypePath = nameof(UnmaskedView.m_SelectorType);
        private const string k_ViewTypePath = nameof(UnmaskedView.m_ViewType);
        private const string k_EditorWindowHighlightFocus = nameof(UnmaskedView.m_OpenAndFocus);
        private const string k_EditorWindowTypePath = nameof(UnmaskedView.m_EditorWindowType);
        private const string k_AlternateEditorWindowTypesPath = nameof(UnmaskedView.m_AlternateEditorWindowTypes);
        private const string k_UnmaskedControlsPath = nameof(UnmaskedView.m_UnmaskedControls);
        private const string k_UnmaskTypePath = nameof(UnmaskedView.m_MaskType);
        private const string k_MaskSizeModifierPath = nameof(UnmaskedView.m_MaskSizeModifier);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            root.AddToClassList("unmasked-view");

            // -- Shared Field(s)
            SerializedProperty selectorTypeProperty = property.FindPropertyRelative(k_SelectorTypePath);
            PropertyField typeSelectorField = new(selectorTypeProperty);
            SerializedProperty unmaskTypeProperty = property.FindPropertyRelative(k_UnmaskTypePath);
            PropertyField unmaskTypeField = new(unmaskTypeProperty);
            SerializedProperty maskSizeModifierProperty = property.FindPropertyRelative(k_MaskSizeModifierPath);
            PropertyField maskSizeField = new(maskSizeModifierProperty);
            UnmaskedControlsListView unmaskedControlsList = new(property.FindPropertyRelative(k_UnmaskedControlsPath));

            // -- Fields only visible for GUI View
            VisualElement guiViewPropsContainer = new(){ name = "GUIViewPropsContainer" };
            guiViewPropsContainer.AddToClassList("indented-property");

            PropertyField viewTypeField = new(property.FindPropertyRelative(k_ViewTypePath));
            guiViewPropsContainer.Add(viewTypeField);

            // -- Fields only visible for Editor Window
            VisualElement editorWindowPropsContainer = new(){ name = "EditorWindowPropsContainer" };
            editorWindowPropsContainer.AddToClassList("indented-property");

            PropertyField editorWindowTypeField = new(property.FindPropertyRelative(k_EditorWindowTypePath));
            PropertyField highlightFocusProp = new(property.FindPropertyRelative(k_EditorWindowHighlightFocus));
            PropertyField alternativeEditorWindowTypeField = new(property.FindPropertyRelative(k_AlternateEditorWindowTypesPath));

            editorWindowPropsContainer.Add(editorWindowTypeField);
            editorWindowPropsContainer.Add(highlightFocusProp);
            editorWindowPropsContainer.Add(alternativeEditorWindowTypeField);

            // Add all elements to root
            root.Add(typeSelectorField);
            root.Add(guiViewPropsContainer);
            root.Add(editorWindowPropsContainer);
            root.Add(unmaskTypeField);
            root.Add(maskSizeField);
            root.Add(unmaskedControlsList);

            UpdateUniqueFieldsVisibility((UnmaskedView.SelectorType)selectorTypeProperty.intValue);
            typeSelectorField.RegisterValueChangeCallback(evt =>
            {
                UnmaskedView.SelectorType viewType = (UnmaskedView.SelectorType)evt.changedProperty.intValue;
                UpdateUniqueFieldsVisibility(viewType);
            });

            return root;

            void UpdateUniqueFieldsVisibility(UnmaskedView.SelectorType viewType)
            {
                UIUtils.ShowOrHide(editorWindowPropsContainer, viewType == UnmaskedView.SelectorType.EditorWindow);
                UIUtils.ShowOrHide(guiViewPropsContainer, viewType == UnmaskedView.SelectorType.GUIView);
            }
        }
    }
}
