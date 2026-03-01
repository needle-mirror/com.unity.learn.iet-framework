using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    ///<summary>
    /// PropertyDrawer for <see cref="LocalizableString"/>
    /// </summary>
    /// <inheritdoc cref="PropertyDrawer"/>
    [CustomPropertyDrawer(typeof(LocalizableString))]
    public class LocalizableStringPropertyDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement propElement = new();
            propElement.style.flexDirection = FlexDirection.Row;

            PropertyField propertyField = new(property.FindPropertyRelative(LocalizableString.PropertyPath), property.displayName);
            propertyField.AddToClassList("unity-property-field");
            propertyField.AddToClassList("unity-property-field__inspector-property");

            propertyField.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            return propertyField;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            PropertyField propertyField = (PropertyField)evt.currentTarget;
            propertyField.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            Image flagIcon = new();
            flagIcon.AddToClassList("inspector-localization-icon");
            flagIcon.tooltip = Localization.Tr(LocalizationKeys.k_LocalizableStringIconTooltip);

            Label label = propertyField.Q<Label>();

            label.parent.Add(flagIcon);
            flagIcon.PlaceInFront(label);
        }
    }

    ///<summary>
    /// PropertyDrawer for <see cref="LocalizableTextAreaAttribute"/>
    /// </summary>
    /// <inheritdoc cref="PropertyDrawer"/>
    [CustomPropertyDrawer(typeof(LocalizableTextAreaAttribute))]
    public class LocalizableTextAreaAttributePropertyDrawer : LocalizableStringPropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement baseInspector = base.CreatePropertyGUI(property);
            baseInspector.RegisterCallbackOnce<GeometryChangedEvent>(OnPropertyCreated);

            return baseInspector;
        }

        private void OnPropertyCreated(GeometryChangedEvent evt)
        {
            VisualElement target = (VisualElement)evt.currentTarget;

            TextField textField = target.Q<TextField>();
            textField.multiline = true;
            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;

            TextInputBaseField<string> inputField = textField.Q<TextInputBaseField<string>>();
            inputField.style.whiteSpace = WhiteSpace.Normal;
            inputField.style.maxHeight = 100;

            // TextInputBaseField<string> inputField = target.Q<TextInputBaseField<string>>();
            // inputField.style.minHeight = 200;
        }
    }
}
