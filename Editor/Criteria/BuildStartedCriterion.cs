using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Needed by BuildStartedCriterion.
    /// </summary>
    public abstract class PreprocessBuildCriterion : Criterion, IPreprocessBuildWithReport
    {
        /// <summary>
        /// See UnityEditor.Build.IPreprocessBuildWithReport.callbackOrder.
        /// </summary>
        public int callbackOrder => 0;
        /// <summary>
        /// See UnityEditor.Build.IPreprocessBuildWithReport.OnPreprocessBuild.
        /// </summary>
        /// <param name="report">The BuildReport from the requested Build</param>
        public abstract void OnPreprocessBuild(BuildReport report);
    }

    /// <summary>
    /// Tests if a build has started.
    /// </summary>
    // TODO revisit this code, BuildPlayerWindow.RegisterBuildPlayerHandler works only when
    // building from the default build dialog, hence IPreprocessBuildWithReport + SessionState used also.
    public class BuildStartedCriterion : PreprocessBuildCriterion
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

        /// <summary>
        /// UnityEditor.Build.IPreprocessBuildWithReport override, do not call.
        /// </summary>
        /// <param name="report">The BuildReport from the requested Build</param>
        public override void OnPreprocessBuild(BuildReport report)
        {
            BuildStarted = true;
        }
    }
}
