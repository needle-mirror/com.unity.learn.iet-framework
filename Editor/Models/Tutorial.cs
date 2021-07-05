using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;

using UnityObject = UnityEngine.Object;
using System.Linq;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// A generic event for signaling changes in a tutorial.
    /// Parameters: sender.
    /// </summary>
    [Serializable]
    public class TutorialEvent : UnityEvent<Tutorial>
    {
    }

    /// <summary>
    /// Raised when a page has been initiated.
    /// Parameters:
    /// - sender
    /// - current page
    /// - current page index
    /// </summary>
    [Serializable]
    public class PageInitiatedEvent : UnityEvent<Tutorial, TutorialPage, int>
    {
    }

    /// <summary>
    /// Raised when going back to the previous page.
    /// Parameters:
    /// - sender
    /// - the page we were on before beginning to go back
    /// </summary>
    [Serializable]
    public class GoingBackEvent : UnityEvent <Tutorial, TutorialPage>
    {
    }

    /// <summary>
    /// A container for tutorial pages which implement the tutorial's functionality.
    /// </summary>
    public class Tutorial : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The title shown in the window.
        /// </summary>
        [Header("Content")]
        public LocalizableString TutorialTitle;
        [SerializeField, HideInInspector]
        string m_TutorialTitle;

        /// <summary>
        /// Enables progress tracking and completion checkmarks for this tutorial. 
        /// </summary>
        public bool ProgressTrackingEnabled { get => m_ProgressTrackingEnabled; set => m_ProgressTrackingEnabled = value; }
        [SerializeField]
        bool m_ProgressTrackingEnabled;

        /// <summary>
        /// Lessond ID used for progress tracking. Typically there's no need to alter this value manually,
        /// instead use the ProgressTrackingEnabled property (a GUID will be generated automatically).
        /// </summary>
        public string LessonId { get => m_LessonId; set => m_LessonId = value; }
        [SerializeField]
        string m_LessonId = "";
        // Cache previous value in order to be restore it if tracking is toggled during the session
        string m_PreviousLessonId;

        void ClearLessonId()
        {
            m_PreviousLessonId = LessonId;
            LessonId = string.Empty;
        }

        void GenerateNewLessonId()
        {
            m_PreviousLessonId = LessonId;
            LessonId = Guid.NewGuid().ToString();
        }

        void RestorePreviousLessonId()
        {
            LessonId = m_PreviousLessonId;
        }

        void OnValidate()
        {
            if (!ProgressTrackingEnabled && LessonId.IsNotNullOrWhiteSpace())
            {
                ClearLessonId();
            }
            
            if (ProgressTrackingEnabled && LessonId.IsNullOrWhiteSpace())
            {
                if (m_PreviousLessonId.IsNullOrWhiteSpace())
                {
                    // Progress Tracking is enabled for the first time
                    GenerateNewLessonId();
                }
                else
                {
                    RestorePreviousLessonId();
                }
            }

            // Prevent duplicate lesson IDs
            if (ProgressTrackingEnabled &&
                TutorialEditorUtils.FindAssets<Tutorial>().Except(new[] { this }).Any(tutorial => tutorial.LessonId == LessonId))
            {
                string oldGuid = LessonId;
                LessonId = Guid.NewGuid().ToString();
                m_PreviousLessonId = LessonId; // prevent triggering the asset migration code in OnAfterDeserialize()
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Duplicate LessonId '{oldGuid}' within the project, generated a new one '{LessonId}'.");
            }
        }

        /// <summary>
        /// Tutorial version, arbitrary string, typically integers are used.
        /// </summary>
        public string Version { get => m_Version; set => m_Version = value; }
        [SerializeField]
        string m_Version = "0";

        [Header("Scene Data")]
        [Tooltip("A scene that is loaded for the tutorial. If None, a new scene will be created for the tutorial.")]
        [SerializeField]
        internal SceneAsset m_Scene = default;

        [SerializeField]
        SceneViewCameraSettings m_DefaultSceneCameraSettings = default;

        /// <summary>
        /// The layout used by the tutorial
        /// </summary>
        public UnityObject WindowLayout { get => m_WindowLayout; set => m_WindowLayout = value; }

        [SerializeField, Tooltip("Saved layouts can be found in the following directories:\n" +
            "Windows: %APPDATA%/Unity/<version>/Preferences/Layouts\n" +
            "macOS: ~/Library/Preferences/Unity/<version>/Layouts\n" +
            "Linux: ~/.config/Preferences/Unity/<version>/Layouts")]
        UnityObject m_WindowLayout;

        internal string WindowLayoutPath => AssetDatabase.GetAssetPath(m_WindowLayout);

        /// <summary>
        /// All the pages of this tutorial.
        /// </summary>
        public IEnumerable<TutorialPage> Pages => m_Pages;
        [SerializeField, FormerlySerializedAs("m_Steps")]
        internal TutorialPageCollection m_Pages = new TutorialPageCollection();

        AutoCompletion m_AutoCompletion;

        /// <summary>
        /// Is this tutorial being skipped currently.
        /// </summary>
        public bool Skipped { get; private set; }

        /// <summary>
        /// Raised when any tutorial is modified.
        /// </summary>
        public static TutorialEvent TutorialModified = new TutorialEvent();

        /// <summary>
        /// Raised when this tutorial is modified.
        /// </summary>
        [Header("Events")]
        public TutorialEvent Modified;

        /// <summary>
        /// Raised after this tutorial has been  initiated.
        /// </summary>
        public TutorialEvent Initiated;

        /// <summary>
        /// Raised after a page of this tutorial has been initiated.
        /// </summary>
        public PageInitiatedEvent PageInitiated;

        /// <summary>
        /// Raised when we are going back to the previous page.
        /// </summary>
        public GoingBackEvent GoingBack;

        /// <summary>
        /// Raised after this tutorial has been completed.
        /// </summary>
        /// <remarks>
        /// This even is raised repeatedly when, for example, going back and forth between the second-to-last and last page,
        /// and the last page has its possible criteria completed.
        /// </remarks>
        public TutorialEvent Completed;

        /// <summary>
        /// Raised when this tutorial is quit at any point.
        /// Quit is signaled always, whether the tutorial is quit by closing the tutorial,\
        /// completing the tutorial or by proceeding to the next tutorial.
        /// </summary>
        public TutorialEvent Quit;

        /// <summary>
        /// The current page index.
        /// </summary>
        public int CurrentPageIndex { get; private set; }

        /// <summary>
        /// Returns the current page.
        /// </summary>
        public TutorialPage CurrentPage =>
             m_Pages.Count == 0
                ? null
                : m_Pages[CurrentPageIndex = Mathf.Min(CurrentPageIndex, m_Pages.Count - 1)];

        /// <summary>
        /// The page count of the tutorial.
        /// </summary>
        public int PageCount => m_Pages.Count;

        /// <summary>
        /// Is the tutorial completed? A tutorial is considered to be completed when we have reached
        /// its last page and possible criteria on the last page are completed.
        /// </summary>
        public bool IsCompleted =>
            PageCount == 0 || (CurrentPageIndex >= PageCount - 2 && CurrentPage != null && CurrentPage.AreAllCriteriaSatisfied);

        /// <summary>
        /// Are we currently auto-completing?
        /// </summary>
        public bool IsAutoCompleting => m_AutoCompletion.running;

        /// <summary>
        /// A wrapper class for serialization purposes.
        /// </summary>
        [Serializable]
        public class TutorialPageCollection : CollectionWrapper<TutorialPage>
        {
            /// <summary> Creates and empty collection. </summary>
            public TutorialPageCollection() : base() {}
            /// <summary> Creates a new collection from existing items. </summary>
            /// <param name="items"></param>
            public TutorialPageCollection(IList<TutorialPage> items) : base(items) {}
        }

        Tutorial()
        {
            m_AutoCompletion = new AutoCompletion(this);
        }

        void OnEnable()
        {
            m_AutoCompletion.OnEnable();
        }

        void OnDisable()
        {
            m_AutoCompletion.OnDisable();
        }

        /// <summary>
        /// Starts auto-completion of this tutorial.
        /// </summary>
        public void StartAutoCompletion()
        {
            m_AutoCompletion.Start();
        }

        /// <summary>
        /// Stops auto-completion of this tutorial.
        /// </summary>
        public void StopAutoCompletion()
        {
            m_AutoCompletion.Stop();
        }

        /// <summary>
        /// Stops this tutorial, meaning its completion requirements are removed.
        /// </summary>
        public void StopTutorial()
        {
            if (CurrentPage != null)
                CurrentPage.RemoveCompletionRequirements();
        }

        /// <summary>
        /// Goes to the previous tutorial page.
        /// </summary>
        public void GoToPreviousPage()
        {
            if (CurrentPageIndex == 0)
                return;

            RaiseGoingBack(CurrentPage);
            CurrentPageIndex = Mathf.Max(0, CurrentPageIndex - 1);
            RaisePageInitiated(CurrentPage, CurrentPageIndex);
        }

        /// <summary>
        /// Attempts to go to the next tutorial page.
        /// </summary>
        /// <returns>true if we proceeded to the next page, false in any other case.</returns>
        public bool TryGoToNextPage()
        {
            if (!CurrentPage || !CurrentPage.AreAllCriteriaSatisfied && !CurrentPage.HasMovedToNextPage)
                return false;
            if (m_Pages.Count == CurrentPageIndex + 1)
            {
                // We're already on the last page
                RaiseCompleted();
                return false;
            }
            int newIndex = Mathf.Min(m_Pages.Count - 1, CurrentPageIndex + 1);
            if (newIndex != CurrentPageIndex)
            {
                if (CurrentPage != null)
                {
                    CurrentPage.OnPageCompleted();
                }
                CurrentPageIndex = newIndex;
                RaisePageInitiated(CurrentPage, CurrentPageIndex);
                // Did we reach the last page? It might have criteria also.
                if (m_Pages.Count == CurrentPageIndex + 1 && m_Pages[CurrentPageIndex].AreAllCriteriaSatisfied)
                {
                    RaiseCompleted();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModified()
        {
            TutorialModified?.Invoke(this);
            Modified?.Invoke(this);
        }

        void LoadScene()
        {
            // load scene
            if (m_Scene != null)
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_Scene));
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            // move scene view camera into place
            if (m_DefaultSceneCameraSettings != null && m_DefaultSceneCameraSettings.Enabled)
                m_DefaultSceneCameraSettings.Apply();
        }

        internal void LoadWindowLayout()
        {
            if (m_WindowLayout == null)
                return;

            var layoutPath = AssetDatabase.GetAssetPath(m_WindowLayout);
            TutorialManager.LoadWindowLayoutWorkingCopy(layoutPath);
        }

        internal void ResetProgress()
        {
            foreach (var page in m_Pages)
            {
                page?.ResetUserProgress();
            }
            CurrentPageIndex = 0;
            Skipped = false;
        }

        internal void Initiate()
        {
            LoadScene();
            RaiseTutorialInitiated();
            if (PageCount > 0)
                RaisePageInitiated(CurrentPage, CurrentPageIndex);
        }

        void RaiseTutorialInitiated()
        {
            Initiated?.Invoke(this);
        }

        void RaiseCompleted()
        {
            Completed?.Invoke(this);
        }

        internal void RaiseQuit()
        {
            Quit?.Invoke(this);
        }

        void RaisePageInitiated(TutorialPage page, int index)
        {
            page?.Initiate();
            PageInitiated?.Invoke(this, page, index);
        }

        void RaiseGoingBack(TutorialPage page)
        {
            page?.RemoveCompletionRequirements();
            GoingBack?.Invoke(this, page);
        }

        /// <summary>
        /// Skips to the last page of the tutorial.
        /// </summary>
        public void SkipToLastPage()
        {
            Skipped = true;
            CurrentPageIndex = PageCount - 1;
            RaisePageInitiated(CurrentPage, CurrentPageIndex);
        }

        /// <summary>
        /// Adds a page to the tutorial
        /// </summary>
        /// <param name="tutorialPage">The page to be added</param>
        public void AddPage(TutorialPage tutorialPage)
        {
            m_Pages.AddItem(tutorialPage);
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Migrate content from < 1.2.
            TutorialParagraph.MigrateStringToLocalizableString(ref m_TutorialTitle, ref TutorialTitle);
            // Migrate content from < 2.0.0-pre.5
            if (!ProgressTrackingEnabled && LessonId.IsNotNullOrWhiteSpace() && m_PreviousLessonId.IsNullOrWhiteSpace()
                && LessonId != m_PreviousLessonId)
            {
                m_PreviousLessonId = LessonId;
                ProgressTrackingEnabled = true;
            }
        }
    }
}
