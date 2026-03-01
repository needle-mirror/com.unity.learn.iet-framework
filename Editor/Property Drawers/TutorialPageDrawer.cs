using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Custom property drawer for <see cref="TutorialPage"/> elements, for when they appear
    /// within a list in their container <see cref="Tutorial"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TutorialPage))]
    internal class TutorialPageDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            PropertyField propField = new(property);
            TutorialPage target = (TutorialPage)property.objectReferenceValue;

            propField.label = $"{target.IndexInTutorial}. {target.Title.Value}";

            propField.RegisterCallbackOnce<GeometryChangedEvent>(OnFieldReady);
            return propField;

            void OnFieldReady(GeometryChangedEvent evt)
            {
                PropertyField target = (PropertyField)evt.currentTarget;
                target.Q<VisualElement>(className: "unity-property-field__input").enabledSelf = false;
            }
        }
    }
}
