using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A utility class for UI Elements and Icons.
    /// </summary>
    internal static class UIUtils
    {
        internal static readonly string s_UIResourcesPath = $"Packages/{FrameworkSettings.k_PackageName}/Editor/UI";
        internal const string s_IconsPath = "Images/Icons/";
        internal const string s_AuthoringPath = "Authoring/";

        /// <summary> Loads an asset from the common UI resource folder. </summary>
        /// <typeparam name="T">type fo the file to load</typeparam>
        /// <param name="filename">name of the file</param>
        /// <returns>A reference to the loaded file</returns>
        internal static T LoadUIAsset<T>(string filename) where T : UnityObject => AssetDatabase.LoadAssetAtPath<T>($"{s_UIResourcesPath}/{filename}");

        /// <summary> Loads a generic icon from the icons folder. For Asset type-specific icons, use <see cref="LoadIconForAssetType"/>.</summary>
        /// <param name="filename">The filename, including extension. No path is needed.</param>
        /// <param name="customiseByTheme">If true, the "d_" prefix is added automatically when the dark Editor theme is on.</param>
        /// <param name="authoringIcon">Whether the icon lives in the /Authoring sub-folder.</param>
        /// <returns>The loaded icon.</returns>
        internal static Texture2D LoadIcon(string filename, bool customiseByTheme = false, bool authoringIcon = false)
        {
            string authoringPath = authoringIcon ? s_AuthoringPath : "";
            string themePrefix = customiseByTheme && EditorGUIUtility.isProSkin ? "d_" : "";
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{s_UIResourcesPath}/{s_IconsPath}{authoringPath}{themePrefix}{filename}");
        }

        /// <summary> Loads an icon for an asset type (Container, TutorialContainer, MaskingPreset, etc.) from the icon subfolder /AssetTypes.
        /// The "d_" prefix is added automatically based on the current Editor UI theme. </summary>
        /// <param name="assetType">The asset type to load an icon for. Specify as a Type, not string.</param>
        /// <returns>The loaded icon, specific for the current Editor theme (light or dark).</returns>
        internal static Texture2D LoadIconForAssetType(Type assetType)
        {
            string editorPrefix = EditorGUIUtility.isProSkin ? "d_" : "";
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{s_UIResourcesPath}/{s_IconsPath}AssetTypes/{editorPrefix}{assetType.Name}_Icon.png");
        }

        internal static Button SetupButton(string buttonName, Action onClickAction, bool isEnabled, VisualElement parent, string text = "", string tooltip = "", bool showIfEnabled = true, bool localize = false)
        {
            Button button = parent.Query<Button>(buttonName);
            button.SetEnabled(isEnabled);
            button.clickable = new Clickable(() => onClickAction?.Invoke());
            button.text = localize ? Localization.Tr(text) : text;
            button.tooltip = string.IsNullOrEmpty(tooltip) ? button.text : tooltip;

            if (showIfEnabled && isEnabled)
            {
                Show(button);
            }

            return button;
        }

        internal static Label SetupLabel(string labelName, string text, VisualElement parent, bool localize, Manipulator manipulator = null)
        {
            Label label = parent.Query<Label>(labelName);
            label.text = localize ? Localization.Tr(text) : text;
            if (manipulator != null)
            {
                label.AddManipulator(manipulator);
            }

            return label;
        }

        internal static Foldout SetupFoldout(string name, string text, VisualElement parent, bool localize, bool open)
        {
            Foldout foldout = parent.Query<Foldout>(name);
            foldout.text = localize ? Localization.Tr(text) : text;
            foldout.value = open;
            return foldout;
        }

        internal static EnumField SetupEnumField<T>(string enumName, string text, EventCallback<ChangeEvent<Enum>> onValueChanged, VisualElement parent, T defaultValue, bool localize) where T : Enum
        {
            EnumField uxmlField = parent.Q<EnumField>(enumName);
            uxmlField.label = localize ? Localization.Tr(text) : text;
            uxmlField.Init(defaultValue);
            uxmlField.value = defaultValue;
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static EnumFlagsField SetupEnumFlagField<T>(string enumName, string text, EventCallback<ChangeEvent<Enum>> onValueChanged, VisualElement parent, T defaultValue, bool localize) where T : Enum
        {
            EnumFlagsField uxmlField = parent.Q<EnumFlagsField>(enumName);
            uxmlField.label = localize ? Localization.Tr(text) : text;
            uxmlField.Init(defaultValue);
            uxmlField.value = defaultValue;
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static void SetupObjectField<T>(string fieldName, EventCallback<ChangeEvent<UnityObject>> onValueChanged, VisualElement parent, T defaultValue) where T : UnityObject
        {
            ObjectField spriteField = parent.Q<ObjectField>(fieldName);
            spriteField.objectType = typeof(T);
            spriteField.value = defaultValue;
            spriteField.RegisterCallback(onValueChanged);
        }

        internal static Toggle SetupToggle(string name, string label, string text, bool defaultValue, EventCallback<ChangeEvent<bool>> onValueChanged, VisualElement parent, bool localize)
        {
            Toggle uxmlField = parent.Q<Toggle>(name);
            uxmlField.label = localize ? Localization.Tr(label) : label;
            uxmlField.text = localize ? Localization.Tr(text) : text;
            uxmlField.value = defaultValue;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static ToolbarSearchField SetupToolbarSearchField(string name, EventCallback<ChangeEvent<string>> onValueChanged, VisualElement parent)
        {
            ToolbarSearchField uxmlField = parent.Q<ToolbarSearchField>(name);
            uxmlField.value = string.Empty;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static IntegerField SetupIntegerField(string name, int value, EventCallback<ChangeEvent<int>> onValueChanged, VisualElement parent)
        {
            IntegerField uxmlField = parent.Q<IntegerField>(name);
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static TextField SetupStringField(string name, string localizationKey, string value, EventCallback<ChangeEvent<string>> onValueChanged, VisualElement parent, bool localize)
        {
            TextField uxmlField = parent.Q<TextField>(name);
            uxmlField.label = localize ? Localization.Tr(localizationKey) : localizationKey;
            uxmlField.value = value;
            uxmlField.SetEnabled(true);
            uxmlField.RegisterCallback(onValueChanged);
            return uxmlField;
        }

        internal static void ShowOrHide(VisualElement element, bool show)
        {
            if (show) Show(element);
            else Hide(element);
        }

        internal static void ShowOrHide(string elementName, VisualElement parent, bool show)
        {
            if (show)
            {
                Show(elementName, parent);
                return;
            }
            Hide(elementName, parent);
        }

        internal static void Hide(string elementName, VisualElement parent)
        {
            Hide(parent.Query<VisualElement>(elementName));
        }

        internal static void Show(string elementName, VisualElement parent)
        {
            Show(parent.Query<VisualElement>(elementName));
        }

        internal static void Hide(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        internal static void Show(VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        internal static VisualTreeAsset LoadUXML(string fileName)
        {
            string path = $"{s_UIResourcesPath}/Uxmls/{fileName}.uxml";
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        internal static void LoadEditorThemeStyleSheet(out StyleSheet styleSheet, VisualElement target)
        {
            string theme = EditorGUIUtility.isProSkin ? "_Dark" : "_Light";
            string themedStyleSheet = $"{s_UIResourcesPath}/Stylesheets/Styles_TutorialWindow{theme}.uss";
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(themedStyleSheet);
            target.styleSheets.Add(styleSheet);
        }

        internal static void LoadCommonStyleSheet(VisualElement target)
        {
            string commonStyleSheetFilePath = $"{s_UIResourcesPath}/Stylesheets/Styles_TutorialWindow.uss";
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(commonStyleSheetFilePath);
            target.styleSheets.Add(styleSheet);
        }

        internal static void RemoveStyleSheet(StyleSheet styleSheet, VisualElement target)
        {
            if (!styleSheet) { return; }

            if (!target.styleSheets.Contains(styleSheet)) { return; }

            target.styleSheets.Remove(styleSheet);
        }

        /// <summary>
        /// Draws a UI Toolkit Inspector for properties of a SerializedObject, one by one, excluding the one specified in propsToIgnore.
        /// It uses the visibility of SerializedProperties to decide what to draw.
        /// </summary>
        internal static void DrawPropertiesExcluding(VisualElement root, SerializedObject serializedObject,
            string[] propsToIgnore)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (propsToIgnore.Contains(iterator.name)) continue;

                PropertyField propertyField = new(iterator) { name = $"PropertyField:{iterator.name}" };
                propertyField.BindProperty(iterator);
                root.Add(propertyField);
            }
        }

        /// <summary>
        /// Draws a UI Toolkit Inspector for all properties of a SerializedObject,
        /// and then it hides the ones specified in propsToHide by removing them from the visual hierarchy.
        /// </summary>
        internal static void DrawInspectorExcluding(VisualElement container, SerializedObject serializedObject, UnityEditor.Editor editor,
            string[] propsToHide)
        {
            InspectorElement.FillDefaultInspector(container, serializedObject, editor);
            foreach (string propertyToRemove in propsToHide)
            {
                container.Q<PropertyField>($"PropertyField:{propertyToRemove}").RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Draw a UI Toolkit Inspector for only the specified properties.
        /// </summary>
        internal static void DrawPropertiesFor(VisualElement container, SerializedObject serializedObject,
            string[] propsToDraw)
        {
            foreach (string propertyPath in propsToDraw)
            {
                SerializedProperty serializedProp = serializedObject.FindProperty(propertyPath);
                container.Add(new PropertyField(serializedProp));
            }
        }

        internal static VisualElement CreateTextAreaElement(string label, SerializedProperty property)
        {
            VisualElement root = new();
            Label labelElement = new(label);
            TextField textArea = new()
            {
                multiline = true,
                style =
                {
                    paddingRight = 6,
                    marginRight = 0,
                    whiteSpace = WhiteSpace.Normal,
                }
            };
            textArea.BindProperty(property);

            ScrollView scrollView = new(ScrollViewMode.Vertical)
            {
                style =
                {
                    minHeight = 20
                }
            };
            scrollView.contentContainer.Add(textArea);

            root.Add(labelElement);
            root.Add(scrollView);

            return root;
        }
    }


    /// <summary>
    /// Represents a MouseManipulator that allows a visual element to react when left clicked
    /// </summary>
    internal class LeftClickManipulator : MouseManipulator
    {
        private Action<VisualElement> m_OnClick;
        private bool m_Active;

        /// <summary>
        /// Initializes and returns an instance of LeftClickManipulator.
        /// </summary>
        /// <param name="OnClick">The default callback that will be triggered when the element is clicked</param>
        public LeftClickManipulator(Action<VisualElement> OnClick)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_OnClick = OnClick;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <summary>
        /// Called when the mouse is clicked on the target, when the user starts pressing the button
        /// </summary>
        /// <param name="e"></param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Called when the mouse is clicked on the target, when the user stops pressing the button
        /// </summary>
        /// <param name="e"></param>
        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e)) { return; }

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();

            if (m_OnClick == null) { return; }
            m_OnClick.Invoke(target);
        }
    }
}
