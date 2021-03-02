using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// A container for tutorial pages which implement the tutorial's functionality.
    /// </summary>
    public class Tutorial : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when any tutorial is modified.
        /// </summary>
        /// <remarks>
        /// Raised before tutorialPagesModified event.
        /// </remarks>
        public static event Action<Tutorial> TutorialModified; // TODO 2.0 merge the two Modified events?

        /// <summary>
        /// Raised when any page of any tutorial tutorial is modified.
        /// </summary>
        public static event Action<Tutorial> TutorialPagesModified;

        /// <summary>
        /// The title shown in the window.
        /// </summary>
        [Header("Content")]
        public LocalizableString TutorialTitle;
        [SerializeField, HideInInspector]
        string m_TutorialTitle;

        /// <summary>
        /// Lessond ID, arbitrary string, typically integers are used.
        /// </summary>
        public string LessonId { get => m_LessonId; set => m_LessonId = value; }
        [SerializeField]
        string m_LessonId = "";

        /// <summary>
        /// Tutorial version, arbitrary string, typically integers are used.
        /// </summary>
        public string Version { get => m_Version; set => m_Version = value; }
        [SerializeField]
        string m_Version = "0";

        [Header("Scene Data")]
        [SerializeField]
        internal SceneAsset m_Scene = default;

        [SerializeField]
        SceneViewCameraSettings m_DefaultSceneCameraSettings = default;

        /// <summary>
        /// The supported skip behavior types.
        /// </summary>
        public enum SkipTutorialBehaviorType
        {
            /// <summary>
            /// Same as exit behaviour.
            /// </summary>
            SameAsExitBehavior,
            /// <summary>
            /// Skip to the last page of the tutorial.
            /// </summary>
            SkipToLastPage,
        }

        /// <summary>
        /// How should the tutorial behave upon skipping.
        /// </summary>
        public SkipTutorialBehaviorType SkipTutorialBehavior { get => m_SkipTutorialBehavior; set => m_SkipTutorialBehavior = value; }
        [SerializeField]
        SkipTutorialBehaviorType m_SkipTutorialBehavior = SkipTutorialBehaviorType.SameAsExitBehavior;

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
        /// Raised when this tutorial is being initiated.
        /// </summary>
        public event Action TutorialInitiated;
        /// <summary>
        /// Raised when a page of this tutorial is being initiated.
        /// </summary>
        public event Action<TutorialPage, int> PageInitiated;
        /// <summary>
        /// Raised when we are going back to the previous page.
        /// </summary>
        public event Action<TutorialPage> GoingBack;
        /// <summary>
        /// Raised when this tutorial is completed.
        /// </summary>
        public event Action<bool> TutorialCompleted;

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
        /// Is the tutorial completed?
        /// </summary>
        public bool Completed =>
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

        /// <summary>
        /// Initializes the tutorial.
        /// </summary>
        public Tutorial()
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

            OnGoingBack(CurrentPage);
            CurrentPageIndex = Mathf.Max(0, CurrentPageIndex - 1);
            OnPageInitiated(CurrentPage, CurrentPageIndex);
        }

        /// <summary>
        /// Attempts to go to the next tutorial page.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        public bool TryGoToNextPage()
        {
            if (!CurrentPage || !CurrentPage.AreAllCriteriaSatisfied && !CurrentPage.HasMovedToNextPage)
                return false;
            if (m_Pages.Count == CurrentPageIndex + 1)
            {
                OnTutorialCompleted(true);
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
                OnPageInitiated(CurrentPage, CurrentPageIndex);
                if (m_Pages.Count == CurrentPageIndex + 1)
                {
                    OnTutorialCompleted(false);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// TODO 2.0 merge with RaiseTutorialModified?
        /// </summary>
        public void RaiseTutorialPagesModified()
        {
            TutorialPagesModified?.Invoke(this);
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseTutorialModifiedEvent()
        {
            TutorialModified?.Invoke(this);
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
            OnTutorialInitiated();
            if (PageCount > 0)
                OnPageInitiated(CurrentPage, CurrentPageIndex);
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
            LoadScene();
        }

        void OnTutorialInitiated()
        {
            TutorialInitiated?.Invoke();
        }

        void OnTutorialCompleted(bool exitTutorial)
        {
            TutorialCompleted?.Invoke(exitTutorial);
        }

        void OnPageInitiated(TutorialPage page, int index)
        {
            page?.Initiate();
            PageInitiated?.Invoke(page, index);
        }

        void OnGoingBack(TutorialPage page)
        {
            page?.RemoveCompletionRequirements();
            GoingBack?.Invoke(page);
        }

        /// <summary>
        /// Skips to the last page of the tutorial.
        /// </summary>
        public void SkipToLastPage()
        {
            Skipped = true;
            CurrentPageIndex = PageCount - 1;
            OnPageInitiated(CurrentPage, CurrentPageIndex);
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
        }
    }
}
