using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// Base class of all paragraphs in a Tutorial Page.
    /// Subclassed to implement specific paragraph types containing text, media, code, buttons, or whatever you need.
    /// </summary>
    public abstract class ParagraphBase : ScriptableObject
    {
        /// <summary>
        /// The masking settings for this paragraph.
        /// </summary>
        internal MaskingSettings MaskingSettings => m_MaskingSettings;

        [Tooltip("Masking and highlighting overlay applied while this paragraph is active.")]
        [SerializeField] private MaskingSettings m_MaskingSettings = new();

        /// <summary>
        /// Creates the VisualElement representing the paragraph for usage in the Tutorials window.
        /// To customise how the Paragraph appears in the Inspector of its TutorialPage SO, use a custom PropertyDrawer.
        /// </summary>
        /// <returns>The VisualElement that contains the paragraph.</returns>
        public virtual VisualElement GetDisplayRoot()
        {
            return new Label("The UI for this Paragraph type has not implemented.");
        }

        /// <summary>
        /// Returns whether the paragraph has been completed.
        /// </summary>
        /// <returns>The paragraph completion state.</returns>
        public virtual bool IsCompleted()
        {
            return true;
        }

        /// <summary>
        /// Override to return true when inheriting from this class, if the Paragraph type can mask the editor.
        /// MaskSetting will be used for that paragraph type.
        /// </summary>
        /// <returns>If true, this paragraph provide MaskSettings to be applied.</returns>
        public virtual bool CanMask() => false;

        /// <summary>
        /// Whether the paragraph type has criterias that need to be satisfied to complete the instructions.
        /// </summary>
        /// <returns>If the paragraph has criterias at all.</returns>
        public virtual bool HasCriteria() => false;

        /// <summary>
        /// List of criteria to satisfy in order to complete the paragraph.
        /// </summary>
        /// <returns>The criteria list.</returns>
        public virtual TypedCriterionCollection Criterias()
        {
            return null;
        }

        /// <summary>
        /// Callback that fires when one of the criteria has been updated.
        /// </summary>
        public virtual void OnCriterionUpdated()
        {

        }

        /// <summary>
        /// Used to ensure that the data contained in the paragraph is valid.
        /// This can range from cleanup of invalid characters to fixing the format of URLs,
        /// and should be implemented by inheriting classes if needed.
        /// </summary>
        internal virtual void Validate()
        {
        }
    }
}
