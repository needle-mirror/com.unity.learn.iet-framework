using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// PropertyDrawer for <see cref="FaqEntry"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(FaqEntry))]
    public class FaqEntryDrawer : PropertyDrawer
    {
        /// <summary>
        /// Creates the VisualElement representing the FAQ item.
        /// </summary>
        /// <param name="property">The SerializedProperty that will be drawn.</param>
        /// <returns>The VisualElement that represents the control.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement propertyDrawer = new();

            VisualElement question = new PropertyField(property.FindPropertyRelative(nameof(FaqEntry.Question)));
            VisualElement answer = new PropertyField(property.FindPropertyRelative(nameof(FaqEntry.Answer)));

            propertyDrawer.Add(question);
            propertyDrawer.Add(answer);

            return propertyDrawer;
        }
    }
}
