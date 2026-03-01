using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A paragraph of a <see cref="TutorialPage"/> used to display a block of text.
    /// </summary>
    /// <inheritdoc cref="ParagraphBase" />
    public class NarrativeParagraph : ParagraphBase
    {
        /// <summary> The text shown in the paragraph. </summary>
        [LocalizableTextArea(3, 15)] public LocalizableString Text;

        /// <inheritdoc />
        public override bool CanMask() => true;

        /// <inheritdoc />
        public override VisualElement GetDisplayRoot()
        {
            //TODO : manage to define default uxml without having to load it every time
            TemplateContainer root = UIUtils.LoadUXML("Paragraphs/Narrative").CloneTree();
            Label label = new(Text);
            label.style.whiteSpace = WhiteSpace.Normal; // Ensure we got word wrap
            root.Q("TutorialStepBox1").Add(label);

            return root;
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            Text = POFileUtils.SanitizeString(Text);
        }
    }
}
