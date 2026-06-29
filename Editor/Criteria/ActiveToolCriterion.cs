using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking that a specific Editor Tool is selected.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class ActiveToolCriterion : Criterion
    {
        /// <summary>
        /// The Tool we wish to test for.
        /// </summary>
        public Tool TargetTool { get => m_TargetTool;
            set => m_TargetTool = value;
        }
        [Tooltip("Editor tool that must be active for the criterion to complete.")]
        [SerializeField] private Tool m_TargetTool;

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            UpdateCompletion();

            EditorApplication.update += UpdateCompletion;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            EditorApplication.update -= UpdateCompletion;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True if complete, false otherwise</returns>
        protected override bool EvaluateCompletion()
        {
            return Tools.current == m_TargetTool;
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            Tools.current = m_TargetTool;
            return true;
        }
    }
}
