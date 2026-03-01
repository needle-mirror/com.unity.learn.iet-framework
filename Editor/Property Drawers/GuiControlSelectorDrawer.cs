using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(GuiControlSelector))]
    internal class GuiControlSelectorDrawer : PropertyDrawer
    {
        private const string k_SelectorTypePath = nameof(GuiControlSelector.m_SelectorMatchType);
        // TODO use nameof()
        private const string k_SelectorModePath = "m_SelectorMode";
        private const string k_GUIContentPath = "m_GUIContent";
        private const string k_ControlNamePath = "m_ControlName";
        private const string k_PropertyPathPath = "m_PropertyPath";
        private const string k_TargetTypePath = "m_TargetType";
        private const string k_GUIStyleNamePath = "m_GUIStyleName";
        private const string k_ObjectReferencePath = "m_ObjectReference";
        private const string k_VisualElementNamePath = nameof(GuiControlSelector.m_VisualElementName);
        private const string k_VisualElementClassNamePath = nameof(GuiControlSelector.m_VisualElementClassName);
        private const string k_VisualElementTypeNamePath = nameof(GuiControlSelector.m_VisualElementTypeName);

        // For visual element picker
        private List<EditorWindow> registeredWindows = new();
        private List<VisualElement> guiViewVisualElements = new();
        private EventCallback<MouseDownEvent> pickerCallback;

        private static readonly GUIContent k_PickButtonContent = new(
            Localization.Tr(LocalizationKeys.k_GuiControlSelectorButtonPickElement),
            Localization.Tr(LocalizationKeys.k_GuiControlSelectorButtonPickElementTooltip)
        );

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty selectorMode = property.FindPropertyRelative(k_SelectorModePath);
            float height = EditorGUI.GetPropertyHeight(selectorMode);
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_SelectorTypePath));

            switch ((GuiControlSelector.Mode)selectorMode.intValue)
            {
                case GuiControlSelector.Mode.GuiContent:
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_GUIContentPath), true);
                    break;
                case GuiControlSelector.Mode.NamedControl:
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_ControlNamePath), true);
                    break;
                case GuiControlSelector.Mode.GuiStyleName:
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_GUIStyleNamePath), true);
                    break;
                case GuiControlSelector.Mode.Property:
                    height +=
                        EditorGUIUtility.standardVerticalSpacing +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_TargetTypePath), true) +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_PropertyPathPath), true);
                    break;
                case GuiControlSelector.Mode.ObjectReference:
                    height += EditorGUIUtility.standardVerticalSpacing
                           + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_ObjectReferencePath), true);
#if UNITY_2021_1_OR_NEWER
                    height += EditorGUIUtility.singleLineHeight;
#endif
                    break;
                case GuiControlSelector.Mode.VisualElement:
                    height +=
                        EditorGUIUtility.standardVerticalSpacing +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_VisualElementClassNamePath), true) +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_VisualElementNamePath), true) +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_VisualElementTypeNamePath), true) +
                        EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    break;
                default:
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    break;
            }
            return height;
        }

        private void BeginPicking(SerializedProperty property)
        {
            // Look through all Editor Windows and GUIViews
            registeredWindows = Resources.FindObjectsOfTypeAll<EditorWindow>().ToList();
            guiViewVisualElements = GUIViewProxy.FindAllGuiViewVisualElements();
            pickerCallback = evt => PickCurrentHoveredElement(evt.target as VisualElement, evt, property);

            foreach (EditorWindow targetWindow in registeredWindows)
            {
                if (targetWindow != null)
                {
                    targetWindow.rootVisualElement.RegisterCallback(pickerCallback, TrickleDown.TrickleDown);
                }
            }

            foreach (VisualElement targetVis in guiViewVisualElements)
            {
                if (targetVis != null)
                {
                    targetVis.RegisterCallback(pickerCallback, TrickleDown.TrickleDown);
                }
            }
        }

        private void PickCurrentHoveredElement(VisualElement element, MouseDownEvent evt, SerializedProperty property)
        {
            // Prefer picking something distinguishable with a name specified.
            if (element.name.IsNullOrEmpty())
            {
                const int numParentsToSearch = 10;
                // Try going up in the hierachy until one with a name is found
                VisualElement elem = element;
                for (int i = 0; i < numParentsToSearch; i++)
                {
                    if (elem == null)
                        break;

                    if (!elem.name.IsNullOrEmpty())
                    {
                        element = elem;
                        break;
                    }

                    elem = element.parent;
                }
            }

            property.FindPropertyRelative(k_VisualElementNamePath).stringValue = element.name;
            property.FindPropertyRelative(k_VisualElementClassNamePath).stringValue = element.GetClasses().LastOrDefault();
            property.FindPropertyRelative(k_VisualElementTypeNamePath).stringValue = element.GetType().ToString();
            // Apply modifications before serializedObject.Update(); call in TutorialPageEditor would dismiss them.
            property.serializedObject.ApplyModifiedProperties();

            evt.StopPropagation();

            EndPicking();
        }

        private void EndPicking()
        {
            foreach (EditorWindow window in registeredWindows)
            {
                window.rootVisualElement.UnregisterCallback(pickerCallback, TrickleDown.TrickleDown);
            }

            foreach (VisualElement targetVis in guiViewVisualElements)
            {
                if (targetVis != null)
                {
                    targetVis.UnregisterCallback(pickerCallback, TrickleDown.TrickleDown);
                }
            }

            pickerCallback = null;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (pickerCallback != null && Event.current.keyCode == KeyCode.Escape)
            {
                EndPicking();
            }

            SerializedProperty selectorType = property.FindPropertyRelative(k_SelectorTypePath);
            position.height = EditorGUI.GetPropertyHeight(selectorType);
            EditorGUI.PropertyField(position, selectorType);
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty selectorMode = property.FindPropertyRelative(k_SelectorModePath);
            position.height = EditorGUI.GetPropertyHeight(selectorMode);
            EditorGUI.PropertyField(position, selectorMode);
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

            GuiControlSelector.Mode mode = (GuiControlSelector.Mode)selectorMode.intValue;
            if (mode == GuiControlSelector.Mode.VisualElement)
            {
                if (GUI.Button(position, k_PickButtonContent, EditorStyles.miniButton))
                {
                    BeginPicking(property);
                }
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            }

            SerializedProperty selectorData = null;
            switch (mode)
            {
                case GuiControlSelector.Mode.GuiContent:
                    selectorData = property.FindPropertyRelative(k_GUIContentPath);
                    break;
                case GuiControlSelector.Mode.NamedControl:
                    selectorData = property.FindPropertyRelative(k_ControlNamePath);
                    break;
                case GuiControlSelector.Mode.Property:
                    SerializedProperty targetType = property.FindPropertyRelative(k_TargetTypePath);
                    position.height = EditorGUI.GetPropertyHeight(targetType);
                    EditorGUI.PropertyField(position, targetType);
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                    selectorData = property.FindPropertyRelative(k_PropertyPathPath);
                    break;
                case GuiControlSelector.Mode.GuiStyleName:
                    selectorData = property.FindPropertyRelative(k_GUIStyleNamePath);
                    break;
                case GuiControlSelector.Mode.ObjectReference:
                    selectorData = property.FindPropertyRelative(k_ObjectReferencePath);
                    break;
                case GuiControlSelector.Mode.VisualElement:
                    SerializedProperty className = property.FindPropertyRelative(k_VisualElementClassNamePath);
                    position.height = EditorGUI.GetPropertyHeight(className);
                    EditorGUI.PropertyField(position, className);
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    SerializedProperty typeName = property.FindPropertyRelative(k_VisualElementTypeNamePath);
                    position.height = EditorGUI.GetPropertyHeight(typeName);
                    EditorGUI.PropertyField(position, typeName);
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    selectorData = property.FindPropertyRelative(k_VisualElementNamePath);
                    break;
            }
            if (selectorData != null)
            {
                position.height = EditorGUI.GetPropertyHeight(selectorData, true) + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, selectorData, true);
                position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            }
            else
            {
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(
                    position,
                    string.Format("No drawing implemented yet for selector mode {0}", (GuiControlSelector.Mode)selectorMode.intValue),
                    MessageType.Error
                );
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            SerializedProperty selectorType = property.FindPropertyRelative(k_SelectorTypePath);
            PropertyField selectorTypeField = new(selectorType);
            root.Add(selectorTypeField);

            SerializedProperty selectorMode = property.FindPropertyRelative(k_SelectorModePath);
            PropertyField selectorModeField = new(selectorMode);
            root.Add(selectorModeField);

            Button pickButton = new() { text = k_PickButtonContent.text, tooltip = k_PickButtonContent.tooltip };
            pickButton.clicked += () => BeginPicking(property);
            root.Add(pickButton);
            SerializedProperty classNameProperty = property.FindPropertyRelative(k_VisualElementClassNamePath);
            PropertyField classNameField = new(classNameProperty);
            root.Add(classNameField);
            SerializedProperty typeNameProperty = property.FindPropertyRelative(k_VisualElementTypeNamePath);
            PropertyField typeNameField = new(typeNameProperty);
            root.Add(typeNameField);
            SerializedProperty visualElementNameProperty = property.FindPropertyRelative(k_VisualElementNamePath);
            PropertyField visualElementNameField = new(visualElementNameProperty);
            root.Add(visualElementNameField);

            SerializedProperty guiContentProperty = property.FindPropertyRelative(k_GUIContentPath);
            guiContentProperty.isExpanded = true;
            PropertyField guiContentField = new(guiContentProperty);
            root.Add(guiContentField);

            SerializedProperty controlNameProperty = property.FindPropertyRelative(k_ControlNamePath);
            PropertyField controlNameField = new(controlNameProperty);
            root.Add(controlNameField);

            SerializedProperty targetTypeProperty = property.FindPropertyRelative(k_TargetTypePath);
            PropertyField targetTypeField = new(targetTypeProperty);
            root.Add(targetTypeField);
            SerializedProperty propertyPathProperty = property.FindPropertyRelative(k_PropertyPathPath);
            PropertyField propertyPathField = new(propertyPathProperty);
            root.Add(propertyPathField);

            SerializedProperty guiStyleNameProperty = property.FindPropertyRelative(k_GUIStyleNamePath);
            PropertyField guiStyleNameField = new(guiStyleNameProperty);
            root.Add(guiStyleNameField);

            SerializedProperty objectReferenceProperty = property.FindPropertyRelative(k_ObjectReferencePath);
            PropertyField objectReferenceField = new(objectReferenceProperty);
            root.Add(objectReferenceField);

            selectorModeField.RegisterValueChangeCallback(evt => { ModeChanged(); });
            ModeChanged();

            return root;

            // To simplify not having to store the element somewhere, we use local function to avoid repetition
            void ModeChanged()
            {
                GuiControlSelector.Mode mode = (GuiControlSelector.Mode)selectorMode.intValue;
                ShowElement(pickButton, mode == GuiControlSelector.Mode.VisualElement);
                ShowElement(classNameField,mode == GuiControlSelector.Mode.VisualElement);
                ShowElement(typeNameField,mode == GuiControlSelector.Mode.VisualElement);
                ShowElement(visualElementNameField,mode == GuiControlSelector.Mode.VisualElement);

                ShowElement(guiContentField,mode == GuiControlSelector.Mode.GuiContent);

                ShowElement(controlNameField,mode == GuiControlSelector.Mode.NamedControl);

                ShowElement(targetTypeField,mode == GuiControlSelector.Mode.Property);
                ShowElement(propertyPathField,mode == GuiControlSelector.Mode.Property);

                ShowElement(guiStyleNameField,mode == GuiControlSelector.Mode.GuiStyleName);

                ShowElement(objectReferenceField,mode == GuiControlSelector.Mode.ObjectReference);
            }
        }

        private void ShowElement(VisualElement element, bool show)
        {
            element.style.display = show? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
