using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A paragraph of a <see cref="TutorialPage"/> used to display a series of instructions.
    /// The block will initially show a blue icon, then a green checkmark when the instructions are performed.
    /// The success state is evaluated using the paragraph's <see cref="Criterias"/>.
    /// </summary>
    /// <inheritdoc cref="ParagraphBase" />
    public class InstructionsParagraph : ParagraphBase
    {
        /// <summary>
        /// The title displayed at the top of the paragraph.
        /// </summary>
        [Tooltip("Title displayed at the top of the paragraph.")]
        public LocalizableString Title = "Instructions";

        /// <summary>
        /// The text that makes the paragraph.
        /// </summary>
        [Tooltip("Body text of the paragraph.")]
        [LocalizableTextArea(3, 15)] public LocalizableString Text;

        [SerializeField]
        [Tooltip("The state in which the criteria of the page are be considered as completed.")]
        internal CompletionType m_CriteriaCompletion = CompletionType.CompletedWhenAllAreTrue;

        [Tooltip("Criteria that must be satisfied to complete this paragraph.")]
        [SerializeField] internal TypedCriterionCollection m_Criteria = new();
        private readonly List<TypedCriterion> m_CriteriaBuffer = new();

        private VisualTreeAsset m_DefaultUI;
        private VisualElement m_Root;

        private IList<TypedCriterion> CriteriaList
        {
            get
            {
                m_Criteria.GetItems(m_CriteriaBuffer);
                return m_CriteriaBuffer.ToArray();
            }
        }

        /// <summary>
        /// The completion criteria if this paragraph's type is Instruction.
        /// </summary>
        public IEnumerable<TypedCriterion> Criteria => CriteriaList;

        /// <inheritdoc />
        public override bool IsCompleted()
        {
            bool allMandatory = m_CriteriaCompletion == CompletionType.CompletedWhenAllAreTrue;
            bool result = allMandatory;

            foreach (TypedCriterion typedCriterion in m_Criteria)
            {
                Criterion criterion = typedCriterion.Criterion;
                if (criterion != null)
                {
                    if (!allMandatory && criterion.IsCompleted)
                    {
                        result = true;
                        break;
                    }

                    if (allMandatory && !criterion.IsCompleted)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override bool CanMask() => true;

        /// <inheritdoc />
        public override bool HasCriteria() => true;

        /// <inheritdoc />
        public override TypedCriterionCollection Criterias() => m_Criteria;

        /// <inheritdoc />
        public override void OnCriterionUpdated()
        {
            UIUtils.ShowOrHide("green", m_Root, IsCompleted());
            UIUtils.ShowOrHide("imgCheckmark", m_Root, IsCompleted());
            UIUtils.ShowOrHide("blue", m_Root, !IsCompleted());
            UIUtils.ShowOrHide("imgArrow", m_Root, !IsCompleted());
        }

        /// <inheritdoc />
        public override VisualElement GetDisplayRoot()
        {
            //TODO : manage to define default uxml without having to load it every time
            m_Root = UIUtils.LoadUXML("Paragraphs/Instruction").CloneTree();

            UIUtils.Show("InstructionContainer", m_Root);
            if (string.IsNullOrEmpty(Title))
            {
                UIUtils.Hide("InstructionTitle", m_Root);
            }
            else
            {
                UIUtils.Show("InstructionTitle", m_Root);
            }

            UIUtils.SetupLabel("InstructionTitle", Title, m_Root, false);

            Label paragraphLabel = new(Text);
            // Ensure we got word wrapping
            paragraphLabel.style.whiteSpace = WhiteSpace.Normal;
            m_Root.Q("InstructionDescription").Add(paragraphLabel);

            OnCriterionUpdated();

            return m_Root;
        }

        internal override void Validate()
        {
            Title = POFileUtils.SanitizeString(Title);
            Text = POFileUtils.SanitizeString(Text);
        }
    }
}
