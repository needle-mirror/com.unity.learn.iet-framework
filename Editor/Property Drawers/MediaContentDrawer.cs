using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Property Drawer for MediaContent properties
    /// </summary>
    [CustomPropertyDrawer(typeof(MediaContent))]
    public class MediaContentDrawer : PropertyDrawer
    {
        private PropertyField m_ImageField;
        private PropertyField m_ClipField;
        private PropertyField m_UrlField;
        private PropertyField m_LoopField;
        private PropertyField m_AutoStartField;

        /// <summary>
        /// Create the UIElement for the given SerializedProperty
        /// </summary>
        /// <param name="property">The SerializedProperty for which to create the elements</param>
        /// <returns>The root of the UIElements hierarchy created</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            root.AddToClassList("unity-base-field");
            root.style.flexDirection = FlexDirection.Column;
            root.style.marginRight = -1;
            root.style.paddingRight = 2;

            SerializedProperty contentTypeProp = property.FindPropertyRelative("m_ContentType");

            PropertyField typeField = new(contentTypeProp);
            typeField.RegisterValueChangeCallback(TypeSwitched);
            root.Add(typeField);

            // Properties ---

            VisualElement propertiesContainer = new();
            propertiesContainer.AddToClassList("indented-property");
            root.Add(propertiesContainer);

            m_ImageField = new PropertyField(property.FindPropertyRelative("m_Image"));
            propertiesContainer.Add(m_ImageField);

            m_ClipField = new PropertyField(property.FindPropertyRelative("m_VideoClip"));
            propertiesContainer.Add(m_ClipField);

            m_UrlField = new PropertyField(property.FindPropertyRelative("m_Url"));
            propertiesContainer.Add(m_UrlField);

            m_LoopField = new PropertyField(property.FindPropertyRelative("m_Loop"));
            propertiesContainer.Add(m_LoopField);

            m_AutoStartField = new PropertyField(property.FindPropertyRelative("m_AutoStart"));
            propertiesContainer.Add(m_AutoStartField);

            MediaContent.MediaContentType sourceType = (MediaContent.MediaContentType)contentTypeProp.enumValueIndex;
            UpdateVisibilities(sourceType);

            return root;
        }

        private void TypeSwitched(SerializedPropertyChangeEvent evt)
        {
            MediaContent.MediaContentType sourceType = (MediaContent.MediaContentType)evt.changedProperty.enumValueIndex;
            UpdateVisibilities(sourceType);
        }

        private void UpdateVisibilities(MediaContent.MediaContentType contentType)
        {
            m_ImageField.style.display = contentType == MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
            m_ClipField.style.display = contentType == MediaContent.MediaContentType.VideoClip ? DisplayStyle.Flex : DisplayStyle.None;
            m_UrlField.style.display = contentType == MediaContent.MediaContentType.VideoUrl ? DisplayStyle.Flex : DisplayStyle.None;

            m_AutoStartField.style.display = contentType != MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
            m_LoopField.style.display = contentType != MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
