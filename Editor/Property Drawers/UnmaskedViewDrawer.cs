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
            VisualElement root = new()
            {
                style =
                {
                    marginLeft = 3,
                    marginRight = 3
                }
            };

            const int propsContainersMargin = 10;

            // Shared Field(s)
            SerializedProperty selectorType = property.FindPropertyRelative(k_SelectorTypePath);
            PropertyField typeSelectorField = new(selectorType);
            root.Add(typeSelectorField);

            // GUI View-only Fields
            VisualElement guiViewPropsContainer = new(){ name = "GUIViewPropsContainer", style = { marginLeft = propsContainersMargin } };

            PropertyField viewTypeField = new(property.FindPropertyRelative(k_ViewTypePath));
            guiViewPropsContainer.Add(viewTypeField);

            root.Add(guiViewPropsContainer);

            // Editor Window-only Fields
            VisualElement editorWindowPropsContainer = new(){ name = "EditorWindowPropsContainer", style = { marginLeft = propsContainersMargin }};

            PropertyField editorWindowTypeField = new(property.FindPropertyRelative(k_EditorWindowTypePath));
            editorWindowPropsContainer.Add(editorWindowTypeField);

            PropertyField highlightFocusProp = new(property.FindPropertyRelative(k_EditorWindowHighlightFocus));
            editorWindowPropsContainer.Add(highlightFocusProp);

            PropertyField alternativeEditorWindowTypeField = new(property.FindPropertyRelative(k_AlternateEditorWindowTypesPath));
            editorWindowPropsContainer.Add(alternativeEditorWindowTypeField);

            root.Add(editorWindowPropsContainer);

            UpdateUniqueFieldsVisibility((UnmaskedView.SelectorType)selectorType.intValue);
            typeSelectorField.RegisterValueChangeCallback(evt =>
            {
                UnmaskedView.SelectorType viewType = (UnmaskedView.SelectorType)evt.changedProperty.intValue;
                UpdateUniqueFieldsVisibility(viewType);
            });

            // Shared Field(s)
            SerializedProperty unmaskType = property.FindPropertyRelative(k_UnmaskTypePath);
            root.Add(new PropertyField(unmaskType));

            SerializedProperty maskSizeModifier = property.FindPropertyRelative(k_MaskSizeModifierPath);
            root.Add(new PropertyField(maskSizeModifier));

            ListView listControl = GetListControlVisualElement(property.FindPropertyRelative(k_UnmaskedControlsPath));
            root.Add(listControl);

            return root;

            void UpdateUniqueFieldsVisibility(UnmaskedView.SelectorType viewType)
            {
                UIUtils.ShowOrHide(editorWindowPropsContainer, viewType == UnmaskedView.SelectorType.EditorWindow);
                UIUtils.ShowOrHide(guiViewPropsContainer, viewType == UnmaskedView.SelectorType.GUIView);
            }
        }

        private ListView GetListControlVisualElement(SerializedProperty prop)
        {
            ListView listView = new()
            {
                name = prop.displayName,
                showAddRemoveFooter = true,
                showBorder = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            listView.AddToClassList("inspector-list");

            listView.makeHeader += () =>
            {
                Label label = new("Unmasked Controls");
                label.AddToClassList("inspector-list-header");
                return label;
            };

            listView.makeItem = () =>
            {
                PropertyField element = new();
                element.AddToClassList("inspector-list-element");
                return element;
            };

            listView.bindItem = (element, i) =>
            {
                PropertyField e = element as PropertyField;
                e.BindProperty(prop.GetArrayElementAtIndex(i));
            };

            listView.BindProperty(prop);

            return listView;
        }
    }
}
