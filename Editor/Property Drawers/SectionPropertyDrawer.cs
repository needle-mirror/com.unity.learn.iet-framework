using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// PropertyDrawer for <see cref="TutorialContainer.Section"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TutorialContainer.Section))]
    public class SectionPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty _typeProperty;
        private VisualElement _tutorialField;
        private VisualElement _metadataField;
        private VisualElement _urlField;

        /// <summary>
        /// Creates the VisualElement representing the Tutorial Section.
        /// </summary>
        /// <param name="property">The SerializedProperty that will be drawn.</param>
        /// <returns>The VisualElement that represents the control.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement drawer = new();
            drawer.AddToClassList("inspector-list-element-basic");

            _typeProperty = property.FindPropertyRelative(nameof(TutorialContainer.Section.Type));

            VisualElement headingField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Heading)));
            VisualElement textField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Text)));
            VisualElement imageField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Image)));
            VisualElement typeField = new PropertyField(_typeProperty);
            _tutorialField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Tutorial)));
            _urlField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Url)));
            _metadataField = new PropertyField(property.FindPropertyRelative(nameof(TutorialContainer.Section.Metadata)));

            typeField.TrackPropertyValue(_typeProperty, _ => UpdateExtraFieldsVisibility());

            drawer.Add(headingField);
            drawer.Add(textField);
            drawer.Add(imageField);
            drawer.Add(typeField);

            // Indented properties
            VisualElement propertiesContainer = new();
            propertiesContainer.AddToClassList("indented-property");
            drawer.Add(propertiesContainer);

            propertiesContainer.Add(_tutorialField);
            propertiesContainer.Add(_urlField);
            propertiesContainer.Add(_metadataField);

            UpdateExtraFieldsVisibility();

            return drawer;
        }

        private void UpdateExtraFieldsVisibility()
        {
            switch ((TutorialContainer.SectionType)_typeProperty.enumValueIndex)
            {
                case TutorialContainer.SectionType.Tutorial:
                    UIUtils.Show(_tutorialField);
                    UIUtils.Hide(_metadataField);
                    UIUtils.Hide(_urlField);
                    break;
                case TutorialContainer.SectionType.ExternalLink:
                    UIUtils.Hide(_tutorialField);
                    UIUtils.Show(_metadataField);
                    UIUtils.Show(_urlField);
                    break;
            }
        }
    }
}
