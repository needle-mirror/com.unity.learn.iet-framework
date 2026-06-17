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
            SerializedProperty answerProp = property.FindPropertyRelative(nameof(FaqEntry.Answer));

            VisualElement question = new PropertyField(property.FindPropertyRelative(nameof(FaqEntry.Question)));

            Label answerLabel = new Label(nameof(FaqEntry.Answer));
            answerLabel.AddToClassList("faq-answer-label");

            TextField answer = new()
            {
                multiline = true,
            };
            answer.AddToClassList("faq-answer-text-field");
            answer.BindProperty(answerProp);

            propertyDrawer.Add(question);
            propertyDrawer.Add(answerLabel);
            propertyDrawer.Add(answer);

            return propertyDrawer;
        }
    }
}
