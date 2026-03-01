using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomPropertyDrawer(typeof(ParagraphBase), true)]
    internal class ParagraphBaseDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();

            ParagraphBase paragraph = (ParagraphBase)property.objectReferenceValue;
            SerializedObject serializedObject = new(paragraph);
            UIUtils.DrawPropertiesExcluding(container, serializedObject, paragraph.CanMask() ? new []{"m_Script"} : new []{"m_Script", "m_MaskingSettings"});

            // Post-fixes
            if(paragraph.CanMask())
            {
                PropertyField maskingField = container.Q<PropertyField>("PropertyField:m_MaskingSettings");
                maskingField.BringToFront();
            }

            return container;
        }
    }
}
