using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Holds all data of the Table Of Content View that the controller needs to access or that should be exposed
    /// </summary>
    [Serializable]
    internal class TableOfContentModel : IModel
    {
        internal static List<TutorialContainer> CategoriesOfProjectDuringTests = new();
        internal IEnumerable<TutorialContainer> RootCategoriesOfProject;
        internal TutorialContainer CurrentContainer;
        internal bool FetchedTutorialStates { get; private set; }

        /// <inheritdoc />
        public event Action StateChanged;

        /// <inheritdoc />
        public void OnStart() { }

        /// <inheritdoc />
        public void OnStop() { }

        /// <inheritdoc />
        public void RestoreState([NotNull] IWindowCache cache)
        {
            StateChanged?.Invoke();
        }

        /// <inheritdoc />
        public void SaveState([NotNull] IWindowCache cache) { }

        /// <summary>
        /// Fetches statuses for all known tutorials from the web API
        /// </summary>
        internal void FetchAllTutorialStates()
        {
            if (TutorialFrameworkModel.s_AreTestsRunning)
            {
                Debug.Log("Not fetching tutorial states since tests are running");
                FetchedTutorialStates = true;
                return;
            }
            TutorialWindow.Instance.StartCoroutine(FetchAllTutorialStatesRoutine());
        }

        private IEnumerator FetchAllTutorialStatesRoutine()
        {
            FetchedTutorialStates = false;

            int attemptsLeft = 3;
            string userId = userId = UnityConnectSession.instance.GetUserId();

            while (attemptsLeft > 0
            && (userId.IsNullOrEmpty() || userId == UnityConnectSession.k_NotSignedInUserUsername))
            {
                attemptsLeft--;
                yield return new EditorWaitForSeconds(1f);
                userId = UnityConnectSession.instance.GetUserId();
            }

            if (userId.IsNullOrEmpty() || userId == UnityConnectSession.k_NotSignedInUserUsername)
            {
                Debug.LogWarning("User not signed in. Please sign in if you want your tutorials progress to be tracked");
                UpdateLocalCompletionStatusOfAllTutorials(new List<GenesisHelper.TutorialProgressStatus>());
                yield break;
            }

            GenesisHelper.GetAllTutorials(UpdateLocalCompletionStatusOfAllTutorials);
        }

        private void UpdateLocalCompletionStatusOfAllTutorials(List<GenesisHelper.TutorialProgressStatus> tutorialsCompletionData)
        {
            IEnumerable<Tutorial> allTutorials = TutorialEditorUtils.FindAssets<Tutorial>()
                                                      .Where(t => t.ProgressTrackingEnabled);
            foreach (Tutorial tutorial in allTutorials)
            {
                GenesisHelper.TutorialProgressStatus completionDataOfTtutorial = tutorialsCompletionData.FirstOrDefault(tcd => tcd.lessonId == tutorial.LessonId);
                if (completionDataOfTtutorial == default)
                {
                    tutorial.CompletedByUser = false;
                }
                else
                {
                    tutorial.CompletedByUser = completionDataOfTtutorial.status == "Finished";
                }
                //UnityEngine.Debug.Log($"{tutorial.LessonId} is {(tutorial.CompletedByUser ? string.Empty : "not")} completed");
            }
            FetchedTutorialStates = true;
        }
    }
}
