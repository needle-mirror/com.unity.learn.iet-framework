using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A paragraph of a <see cref="TutorialPage"/> that renders as a clickable button.
    /// Subclasses implement <see cref="OnClick"/> to define the action taken when the button is pressed.
    /// </summary>
    /// <inheritdoc cref="ParagraphBase" />
    public abstract class ButtonParagraph : ParagraphBase
    {
        /// <summary> The label shown on the button. </summary>
        [Tooltip("Label shown on the button.")]
        public LocalizableString ButtonText;

        /// <inheritdoc />
        public sealed override VisualElement GetDisplayRoot()
        {
            Button button = new() { text = ButtonText };
            button.AddToClassList("tutorial-button-paragraph");
            button.clicked += OnClick;
            return button;
        }

        /// <summary> Called when the button is clicked. </summary>
        protected abstract void OnClick();
    }
}
