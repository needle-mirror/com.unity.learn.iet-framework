#region

using UnityEditor;

#endregion

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Tests if a build has started.
    /// </summary>
    // TODO revisit this code, BuildPlayerWindow.RegisterBuildPlayerHandler works only when
    // building from the default build dialog, hence IPreprocessBuildWithReport + SessionState used also.
    public class BuildStartedCriterion : Criterion
    {
        bool BuildStarted
        {
            get => SessionState.GetBool("BuildStartedCriterion.BuildStarted", false);
            set => SessionState.SetBool("BuildStartedCriterion.BuildStarted", value);
        }

        /// <summary>
        /// Used for BuildPlayerWindow.RegisterBuildPlayerHandler.
        /// </summary>
        /// <param name="options">The BuildPlayerOption of the requested build</param>
        public void BuildPlayerCustomHandler(BuildPlayerOptions options)
        {
            BuildStarted = true;
            BuildPipeline.BuildPlayer(options);
        }

        /// <summary>
        /// Starts testing of the criterion.
        /// </summary>
        public override void StartTesting()
        {
            base.StartTesting();
            BuildStarted = false;
            UpdateCompletion();
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerCustomHandler);
            EditorApplication.update += UpdateCompletion;
        }

        /// <summary>
        /// Stops testing of the criterion.
        /// </summary>
        public override void StopTesting()
        {
            base.StopTesting();
            BuildPlayerWindow.RegisterBuildPlayerHandler(null);
            EditorApplication.update -= UpdateCompletion;
        }

        /// <summary>
        /// Evaluates if the criterion is completed.
        /// </summary>
        /// <returns>True if the build have started, false otherwise</returns>
        protected override bool EvaluateCompletion()
        {
            return BuildStarted;
        }

        /// <summary>
        /// Auto-completes the criterion.
        /// </summary>
        /// <returns>True if the auto-completion succeeded.</returns>
        public override bool AutoComplete()
        {
            return true;
        }
    }
}
