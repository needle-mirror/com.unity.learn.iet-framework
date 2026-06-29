using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Criterion for checking a specific Play Mode state.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class PlayModeStateCriterion : Criterion
    {
        private enum PlayModeState
        {
            Playing,
            NotPlaying
        }

        [Tooltip("Play Mode state that must be active for the criterion to complete.")]
        [SerializeField] private PlayModeState m_RequiredPlayModeState = PlayModeState.Playing;

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            UpdateCompletion();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    UpdateCompletion();
                    break;
            }
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True the Play Mode Criterion is satisfied</returns>
        protected override bool EvaluateCompletion()
        {
            switch (m_RequiredPlayModeState)
            {
                case PlayModeState.NotPlaying:
                    return !EditorApplication.isPlaying;

                case PlayModeState.Playing:
                    return EditorApplication.isPlaying;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            EditorApplication.isPlaying = m_RequiredPlayModeState == PlayModeState.Playing;

            return true;
        }
    }
}
