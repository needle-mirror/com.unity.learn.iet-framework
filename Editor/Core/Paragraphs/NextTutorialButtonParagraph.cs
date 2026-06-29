using UnityEngine;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A <see cref="ButtonParagraph"/> that starts another <see cref="Tutorial"/> when clicked.
    /// </summary>
    /// <inheritdoc cref="ButtonParagraph" />
    public class NextTutorialButtonParagraph : ButtonParagraph
    {
        /// <summary> The tutorial to start when the button is clicked. </summary>
        [Tooltip("Tutorial started when the button is clicked.")]
        public Tutorial NextTutorial;

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (NextTutorial == null)
            {
                Debug.LogError($"{nameof(NextTutorialButtonParagraph)}: {nameof(NextTutorial)} is null. Assign the tutorial to switch to.", this);
                return;
            }

            TutorialWindow.BroadcastEvent(new TutorialStartRequestedEvent(NextTutorial, null));
        }
    }
}
