using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
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
            var rootElement = new VisualElement();
            rootElement.AddToClassList("unity-base-field");
            rootElement.style.flexDirection = FlexDirection.Column;

            var propertyLabel = new Label(property.displayName);
            propertyLabel.AddToClassList("unity-property-field");
            rootElement.Add(propertyLabel);

            var propertiesContainer = new VisualElement();
            rootElement.Add(propertiesContainer);

            propertiesContainer.style.paddingLeft = 6;

            var typeProperty = property.FindPropertyRelative("m_ContentType");
            var typeField = new PropertyField();
            typeField.BindProperty(typeProperty);
            typeField.RegisterValueChangeCallback(TypeSwitched);
            propertiesContainer.Add(typeField);

            m_ImageField = new PropertyField();
            m_ImageField.BindProperty(property.FindPropertyRelative("m_Image"));
            propertiesContainer.Add(m_ImageField);

            m_ClipField = new PropertyField();
            m_ClipField.BindProperty(property.FindPropertyRelative("m_VideoClip"));
            propertiesContainer.Add(m_ClipField);

            m_UrlField = new PropertyField();
            m_UrlField.BindProperty(property.FindPropertyRelative("m_Url"));
            propertiesContainer.Add(m_UrlField);

            m_LoopField = new PropertyField();
            m_LoopField.BindProperty(property.FindPropertyRelative("m_Loop"));
            propertiesContainer.Add(m_LoopField);

            m_AutoStartField = new PropertyField();
            m_AutoStartField.BindProperty(property.FindPropertyRelative("m_AutoStart"));
            propertiesContainer.Add(m_AutoStartField);

            MediaContent.MediaContentType sourceType = (MediaContent.MediaContentType)typeProperty.enumValueIndex;
            UpdateVisibilities(sourceType);

            return rootElement;
        }

        void TypeSwitched(SerializedPropertyChangeEvent evt)
        {
            MediaContent.MediaContentType sourceType = (MediaContent.MediaContentType)evt.changedProperty.enumValueIndex;
            UpdateVisibilities(sourceType);
        }

        void UpdateVisibilities(MediaContent.MediaContentType contentType)
        {
            m_ImageField.style.display = contentType == MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
            m_ClipField.style.display = contentType == MediaContent.MediaContentType.VideoClip ? DisplayStyle.Flex : DisplayStyle.None;
            m_UrlField.style.display = contentType == MediaContent.MediaContentType.VideoUrl ? DisplayStyle.Flex : DisplayStyle.None;

            m_AutoStartField.style.display = contentType != MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
            m_LoopField.style.display = contentType != MediaContent.MediaContentType.Image ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// IMGUI version of the drawer, called when the given SerializedProperty is part of IMGUI code
        /// </summary>
        /// <param name="position">The Rect in which to draw the property</param>
        /// <param name="property">The SerializedProperty for which to draw controls</param>
        /// <param name="label">The label before the property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var contentTypeProp = property.FindPropertyRelative("m_ContentType");
            var contentType = (MediaContent.MediaContentType)contentTypeProp.enumValueIndex;

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, label);
            current.y += current.height;

            current.x += 16;
            current.width -= 16;

            EditorGUI.PropertyField(current, contentTypeProp);
            current.y += current.height;

            switch (contentType)
            {
                case MediaContent.MediaContentType.Image:
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_Image"));
                    current.y += current.height;
                    break;
                case MediaContent.MediaContentType.VideoClip:
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_VideoClip"));
                    current.y += current.height;
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_AutoStart"));
                    current.y += current.height;
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_Loop"));
                    current.y += current.height;
                    break;
                case MediaContent.MediaContentType.VideoUrl:
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_Url"));
                    current.y += current.height;
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_AutoStart"));
                    current.y += current.height;
                    EditorGUI.PropertyField(current, property.FindPropertyRelative("m_Loop"));
                    current.y += current.height;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Return the height the property will fill in the UI (used by the OnGUI version)
        /// </summary>
        /// <param name="property">The SerializedProperty for which to return the height</param>
        /// <param name="label">The label appearing before the property controls</param>
        /// <returns>The height in pixel this drawer needs to draw the controls</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var contentTypeProp = property.FindPropertyRelative("m_ContentType");
            var contentType = (MediaContent.MediaContentType)contentTypeProp.enumValueIndex;

            //label + content type dropdown,
            // - image 1 slot for image asset,
            // - video 1 slot for url/clip, 1 for auto start, 1 for loop = 3
            int lines = 2 + (contentType == MediaContent.MediaContentType.Image ? 1 : 3);

            return EditorGUIUtility.singleLineHeight * lines;
        }
    }
}
