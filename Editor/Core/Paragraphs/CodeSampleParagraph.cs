using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A paragraph of a <see cref="TutorialPage"/> that includes a block of code, and a button to copy it in the clipboard.
    /// </summary>
    /// <inheritdoc cref="ParagraphBase" />
    public class CodeSampleParagraph : ParagraphBase
    {
        /// <summary>
        /// The code being displayed in the paragraph.
        /// </summary>
        [Tooltip("Code snippet displayed in the paragraph.")]
        [CodeSampleBlock] public string CodeSample;

        /// <inheritdoc />
        public override VisualElement GetDisplayRoot()
        {
            TemplateContainer root = UIUtils.LoadUXML("Paragraphs/CodeSample").CloneTree();
            UIUtils.Show("CodeSampleScrollView", root);
            Label codeSample = root.Q<Label>("CodeSample");

            VisualElement codeSampleScrollView = root.Q<VisualElement>("CodeSampleScrollView");
            VisualElement btn = new()
            {
                tooltip = Localization.Tr("CopyCodeTooltip")
            };
            btn.AddToClassList("code-sample-copy-button");

            VisualElement overlay = new();
            overlay.AddToClassList("code-sample-copied-notice");
            Label copyLabel = new(Localization.Tr("CodeCopiedWarning"));
            overlay.Add(copyLabel);

            // We need to bypass the normal Add by using hierarchy. Add because we want the button to be on
            // top right corner of the scrollview window, not its content (which can expand past the window)
            codeSampleScrollView.hierarchy.Add(btn);
            codeSampleScrollView.hierarchy.Add(overlay);

            btn.AddManipulator(new Clickable(OnCopyCodeClick));
            codeSample.text = CodeSampleUtils.HighlightCode(CodeSample);
            return root;

            void OnCopyCodeClick()
            {
                GUIUtility.systemCopyBuffer = CodeSample;
                overlay.style.opacity = 1;
                overlay.style.display = DisplayStyle.Flex;
                overlay.schedule.Execute(GoBack).StartingIn(1000);
            }

            void GoBack()
            {
                // Copied text fades out again
                overlay.style.opacity = 0;
                overlay.schedule.Execute(FinaliseAnimation).StartingIn(250);
            }

            void FinaliseAnimation()
            {
                // Panel is not displayed, meaning the button is clickable again
                overlay.style.display = DisplayStyle.None;
            }
        }
    }
}
