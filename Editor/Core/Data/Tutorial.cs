using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Editor
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
    public class GoingBackEvent : UnityEvent<Tutorial, TutorialPage>
    {
    }

    /// <summary>
    /// A container for tutorial pages which implement the tutorial's functionality.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class Tutorial : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Tutorial version, arbitrary string, typically integers are used.
        /// </summary>
        public string Version { get => m_Version; set => m_Version = value; }

        [SerializeField] private string m_Version = "0";

        /// <summary>
        /// Scene management behavior at the start of a tutorial.
        /// </summary>
        public enum SceneManagementBehaviorType
        {
            /// <summary>
            /// Create a new scene with default game objects (a light and camera) in the scene.
            /// </summary>
            CreateNewScene,
            /// <summary>
            /// Do nothing, use the currently active scene(s).
            /// </summary>
            UseActiveScene,
            /// <summary>
            /// Load one or more specified scenes.
            /// </summary>
            LoadScenes,
        }

        /// <summary>
        /// Is the current page of the tutorial the first one?
        /// </summary>
        /// <returns>True if yes, false otherwise</returns>
        public bool CurrentPageIsFirst() => CurrentPageIndex == 0;

        /// <summary>
        /// Is the current page of the tutorial the last one?
        /// </summary>
        /// <returns>True if yes, false otherwise</returns>
        public bool CurrentPageIsLast() => CurrentPageIndex == PageCount - 1;

        /// <summary>
        /// The title shown in the window.
        /// </summary>
        [Header("Contents")]
        public LocalizableString TutorialTitle = new();

        /// <summary>
        /// Lesson ID used for progress tracking. Typically there's no need to alter this value manually,
        /// instead use the ProgressTrackingEnabled property (a GUID will be generated automatically).
        /// </summary>
        public string LessonId { get => m_LessonId; set => m_LessonId = value; }

        [SerializeField]
        internal string m_LessonId = "";

        // Cache previous value in order to be restore it if tracking is toggled during the session
        private string m_PreviousLessonId;

        /// <summary>
        /// All the pages of this tutorial.
        /// </summary>
        public TutorialPageCollection PagesCollection { get => m_Pages; set => m_Pages = value; }

        [SerializeField, FormerlySerializedAs("m_Steps")]
        internal TutorialPageCollection m_Pages = new();

        /// <summary>
        /// Scene management behavior at the start of a tutorial.
        /// </summary>
        public SceneManagementBehaviorType SceneManagementBehavior { get => m_SceneManagementBehavior; set => m_SceneManagementBehavior = value; }

        /// <summary>
        /// Will the tutorial revert to the open scenes before it was started or leave the scene as is (default is true)
        /// </summary>
        public bool ReturnToPreviousScenes { get => m_ReturnToPreviousScenes; set => m_ReturnToPreviousScenes = value; }

        /// <summary>
        /// Scenes to be loaded when the tutorial starts, if any.
        /// </summary>
        /// <remarks>
        /// The first scene is the main scene, the rest are loaded as additive scenes.
        /// </remarks>
        public SceneAsset[] Scenes { get => m_Scenes; set => m_Scenes = value; }

        [Header("Scene Management")]
        [SerializeField, Tooltip("Behaviour to adopt when the tutorial starts.")]
        internal SceneManagementBehaviorType m_SceneManagementBehavior;

        [SerializeField, Tooltip("Revert to previous scene(s) on tutorial ends, or keep the opened scene(s).")]
        internal bool m_ReturnToPreviousScenes = true;

        [SerializeField, Tooltip("Scenes to be loaded when the tutorial starts, if any. The first scene is the main scene, the rest are loaded as additive scenes.")]
        internal SceneAsset[] m_Scenes;

        /// <summary>
        /// Scene view camera settings to be applied when the tutorial starts.
        /// </summary>
        public SceneViewCameraSettings DefaultSceneCameraSettings { get => m_DefaultSceneCameraSettings; set => m_DefaultSceneCameraSettings = value; }

        [Header("Settings")]
        [SerializeField]
        private SceneViewCameraSettings m_DefaultSceneCameraSettings;

        /// <summary>
        /// Enables progress tracking and completion checkmarks for this tutorial.
        /// </summary>
        public bool ProgressTrackingEnabled { get => m_ProgressTrackingEnabled; set => m_ProgressTrackingEnabled = value; }

        [SerializeField] private bool m_ProgressTrackingEnabled;

        /// <summary>
        /// The layout used by the tutorial
        /// </summary>
        public UnityObject WindowLayout { get => m_WindowLayout; set => m_WindowLayout = value; }

        [SerializeField, Tooltip("Saved layouts can be found in the following directories:\n" +
            "Windows: %APPDATA%/Unity/<version>/Preferences/Layouts\n" +
            "macOS: ~/Library/Preferences/Unity/<version>/Layouts\n" +
            "Linux: ~/.config/Preferences/Unity/<version>/Layouts")]
        private UnityObject m_WindowLayout;

        internal string WindowLayoutPath => AssetDatabase.GetAssetPath(WindowLayout);

        /// <summary>
        /// A dialog displayed when the tutorial is completed, if the user is not switching to a different tutorial
        /// </summary>
        [SerializeField, Tooltip("A dialog displayed when the tutorial is completed, if the user is not switching to a different tutorial")]
        public TutorialWelcomePage CompletionDialog;

        internal bool ShowCompletionDialogOnQuit { get; set; }

        /// <summary>
        /// Raised when any tutorial is modified.
        /// </summary>
        public static TutorialEvent TutorialModified = new();

        /// <summary>
        /// Raised when this tutorial is modified.
        /// </summary>
        internal TutorialEvent Modified = new();

        /// <summary>
        /// Raised after this tutorial has been  initiated.
        /// </summary>
        public TutorialEvent Initiated = new();

        /// <summary>
        /// Raised after a page of this tutorial has been initiated.
        /// </summary>
        public PageInitiatedEvent PageInitiated = new();

        /// <summary>
        /// Raised when we are going back to the previous page.
        /// </summary>
        public GoingBackEvent GoingBack = new();

        /// <summary>
        /// Raised after this tutorial has been completed.
        /// </summary>
        /// <remarks>
        /// This even is raised repeatedly when, for example, going back and forth between the second-to-last and last page,
        /// and the last page has its possible criteria completed.
        /// </remarks>
        public TutorialEvent Completed = new();

        /// <summary>
        /// Raised when this tutorial is quit at any point.
        /// Quit is signaled always, whether the tutorial is quit by closing the tutorial,\
        /// completing the tutorial or by proceeding to the next tutorial.
        /// </summary>
        public TutorialEvent Quit = new();

        internal string SessionStateKey => $"Unity.Tutorials.Editor.lesson{LessonId.AsEmptyIfNull()}";

        /// <summary>
        /// Has the tutorial already been completed by the logged-in user?
        /// </summary>
        internal bool CompletedByUser
        {
            get => m_CompletedByUser;
            set
            {
                if (m_CompletedByUser == value)
                {
                    return;
                }
                m_CompletedByUser = value;
                SaveCompletionStateLocally();
            }
        }

        private bool m_CompletedByUser;

        private AutoCompletion m_AutoCompletion;

        // Backwards-compatibility for < 2.1
        [SerializeField, HideInInspector]
        internal SceneAsset m_Scene;
        // Backwards-compatibility for < 1.2
        [SerializeField, HideInInspector] private string m_TutorialTitle;

        /// <summary>
        /// Is this tutorial being skipped currently.
        /// </summary>
        public bool Skipped { get; private set; }

        /// <summary>
        /// The current page index.
        /// </summary>
        public int CurrentPageIndex { get; private set; }

        /// <summary>
        /// Returns the current page.
        /// </summary>
        public TutorialPage CurrentPage =>
            PagesCollection == null || PagesCollection.Count == 0
            ? null
            : PagesCollection[CurrentPageIndex = Mathf.Min(CurrentPageIndex, PagesCollection.Count - 1)];

        /// <summary>
        /// The page count of the tutorial.
        /// </summary>
        public int PageCount => PagesCollection.Count;

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
            public TutorialPageCollection()
            { }
            /// <summary> Creates a new collection from existing items. </summary>
            /// <param name="items">The list of TutorialPage with collections is insitialized with</param>
            public TutorialPageCollection(IList<TutorialPage> items) : base(items) { }
        }

        /// <summary>
        /// Faq Entries for that specific tutorial
        /// </summary>
        [SerializeField]
        private FaqEntry[] m_FaqEntries = Array.Empty<FaqEntry>();


        private bool HasScenes() => Scenes?.Length > 0;

        private void ClearLessonId()
        {
            m_PreviousLessonId = LessonId;
            LessonId = string.Empty;
        }

        private void GenerateNewLessonId()
        {
            m_PreviousLessonId = LessonId;
            LessonId = Guid.NewGuid().ToString();
        }

        private void RestorePreviousLessonId()
        {
            LessonId = m_PreviousLessonId;
        }

        private void OnValidate()
        {
            TutorialTitle = POFileUtils.SanitizeString(TutorialTitle);

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

        private Tutorial()
        {
            m_AutoCompletion = new AutoCompletion(this);
        }

        private void OnEnable()
        {
            m_AutoCompletion.OnEnable();
        }

        private void OnDisable()
        {
            m_AutoCompletion.OnDisable();
        }

        /// <summary>
        /// Starts this Tutorial. Can be used as a callback for a button in the Dialog Window welcome page.
        /// </summary>
        public void StartTutorial()
        {
            TutorialWindow.StartTutorial(this);
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
            CurrentPage?.ResetUserProgressAndCompletionCriteria();
        }

        /// <summary>
        /// Goes to the previous tutorial page.
        /// </summary>
        public void GoToPreviousPage()
        {
            if (CurrentPageIndex == 0)
            {
                return;
            }

            RaiseGoingBack(CurrentPage);
            CurrentPageIndex = Mathf.Max(0, CurrentPageIndex - 1);
            RaisePageInitiated(CurrentPage, CurrentPageIndex);
        }

        internal bool CanMoveToNextPage => CurrentPage && CurrentPage.AreAllCriteriaSatisfied;

        /// <summary>
        /// Attempts to go to the next tutorial page.
        /// </summary>
        /// <returns>true if we proceeded to the next page, false in any other case.</returns>
        public bool TryGoToNextPage()
        {
            if (!CanMoveToNextPage)
            {
                return false;
            }

            CurrentPage.MarkAsCompleted();
            if (CurrentPageIsLast())
            {
                RaiseCompleted();
                return false;
            }

            int nextPageIndex = Mathf.Min(PagesCollection.Count - 1, CurrentPageIndex + 1);
            if (nextPageIndex != CurrentPageIndex)
            {
                CurrentPageIndex = nextPageIndex;
                RaisePageInitiated(CurrentPage, CurrentPageIndex);
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

        private void LoadScene()
        {
            switch (SceneManagementBehavior)
            {
                case SceneManagementBehaviorType.UseActiveScene:
                    // Do nothing
                    break;
                case SceneManagementBehaviorType.CreateNewScene:
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                    break;
                case SceneManagementBehaviorType.LoadScenes:
                    if (HasScenes())
                    {
                        int index = 0;
                        try
                        {
                            // The first scene is the main scene, the rest are additive.
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(Scenes[index]));
                            ++index;
                            for (; index < Scenes.Length; ++index)
                            {
                                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(Scenes[index]), OpenSceneMode.Additive);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Could not load Scenes Element {index}: {e.Message}");
                        }
                    }
                    break;
            }

            // move scene view camera into place
            if (DefaultSceneCameraSettings != null && DefaultSceneCameraSettings.Enabled)
                DefaultSceneCameraSettings.Apply();
        }

        internal void ResetProgress()
        {
            foreach (TutorialPage page in PagesCollection)
            {
                page?.ResetUserProgressAndCompletionCriteria();
            }
            CurrentPageIndex = 0;
            Skipped = false;
        }

        /// <summary>
        /// Raises both Initiated and PageInitiated
        /// </summary>
        internal void Initiate()
        {
            LoadScene();
            RaiseTutorialInitiated();

            if (PageCount > 0)
            {
                RaisePageInitiated(CurrentPage, CurrentPageIndex);
            }
        }

        private void RaiseTutorialInitiated()
        {
            Initiated?.Invoke(this);
        }

        private void RaiseCompleted()
        {
            Completed?.Invoke(this);
        }

        internal void RaiseQuit()
        {
            Quit?.Invoke(this);
        }

        internal void RaisePageInitiated(TutorialPage page, int index)
        {
            page?.Initiate();
            PageInitiated?.Invoke(this, page, index);
        }

        private void RaiseGoingBack(TutorialPage page)
        {
            page?.ResetUserProgressAndCompletionCriteria();
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
        /// Adds a page to the tutorial.
        /// </summary>
        /// <param name="tutorialPage">The page to be added.</param>
        public void AddPage(TutorialPage tutorialPage)
        {
            PagesCollection.AddItem(tutorialPage);
        }

        /// <summary>
        /// Fill the provided List with the questions for the given section
        /// </summary>
        /// <param name="section">The section for which get the FAQ question</param>
        /// <param name="questions">The list that will be filled with the question</param>
        public void GetFaqQuestions(HelpPanelHandler.Section section, out List<FaqEntry> questions)
        {
            questions = new List<FaqEntry>();

            switch (section)
            {
                case HelpPanelHandler.Section.TutorialContainer:
                    questions.AddRange(TutorialWindow.Instance.CurrentContainer.FaqEntries);
                    break;
                case HelpPanelHandler.Section.Tutorial:
                    questions.AddRange(m_FaqEntries);
                    break;
                case HelpPanelHandler.Section.Page:
                    questions.AddRange(CurrentPage.m_FaqEntries);
                    break;
            }
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
            if (!ProgressTrackingEnabled
            && LessonId.IsNotNullOrWhiteSpace()
            && m_PreviousLessonId.IsNullOrWhiteSpace()
            && LessonId != m_PreviousLessonId)
            {
                m_PreviousLessonId = LessonId;
                ProgressTrackingEnabled = true;
            }

            // This is for supporting assets made with pre 2.1 -versions of IET
            if (m_Scene != null)
            {
                Scenes = new SceneAsset[1];
                Scenes[0] = m_Scene;
                m_Scene = null;
            }
        }

        /// <summary>
        /// Loads the completion state of the Tutorial
        /// </summary>
        /// <returns>returns true if the state was found from EditorPrefs</returns>
        internal bool LoadLocalCompletionState()
        {
            const string nonexisting = "NONEXISTING";
            string state = SessionState.GetString(SessionStateKey, nonexisting);
            if (state == "")
            {
                CompletedByUser = false;
            }
            else if (state == "Finished")
            {
                CompletedByUser = true;
            }
            return state != nonexisting;
        }

        private void SaveCompletionStateLocally()
        {
            SessionState.SetString(SessionStateKey, CompletedByUser ? "Finished" : "");
        }

        internal void EmbedPagesAsSubAssetsV6()
        {
            for (int i = 0; i < m_Pages.Count; i++)
            {
                TutorialPage tutorialPage = m_Pages[i];
                if (tutorialPage == null)
                {
                    Debug.LogWarning($"[Tutorial Framework] Tutorial '{name}': page at index {i} is null, skipping.");
                    continue;
                }
                if (AssetDatabase.IsSubAsset(tutorialPage)) continue;

                string oldPagePath = AssetDatabase.GetAssetPath(tutorialPage);
                UnityObject[] oldAssets = AssetDatabase.LoadAllAssetsAtPath(oldPagePath);

                TutorialPage newPage = Instantiate(tutorialPage);
                m_Pages[i] = newPage;
                AssetDatabase.AddObjectToAsset(newPage, this);

                newPage.IndexInTutorial = i;
                newPage.m_PageParagraphs = new List<ParagraphBase>();

                var criterionMap = new Dictionary<Criterion, Criterion>();

                foreach (ParagraphBase paragraph in tutorialPage.m_PageParagraphs)
                {
                    if (paragraph == null)
                    {
                        Debug.LogWarning($"[Tutorial Framework] Tutorial '{name}', page '{tutorialPage.name}': null paragraph entry, skipping.");
                        continue;
                    }
                    ParagraphBase newParagraph = Instantiate(paragraph);
                    newParagraph.hideFlags |= HideFlags.HideInHierarchy;
                    newParagraph.name = paragraph.GetType().Name;
                    AssetDatabase.AddObjectToAsset(newParagraph, this);
                    newPage.Paragraphs.Add(newParagraph);

                    // Copy criteria so they are not lost when the old page asset is deleted
                    TypedCriterionCollection criteriaCollection = newParagraph.Criterias();
                    if (criteriaCollection == null) continue;

                    foreach (TypedCriterion tc in criteriaCollection)
                    {
                        if (tc.Criterion == null) continue;
                        Criterion oldCriterion = tc.Criterion;
                        Criterion newCriterion = Instantiate(oldCriterion);
                        newCriterion.hideFlags |= HideFlags.HideInHierarchy;
                        newCriterion.name = oldCriterion.name;
                        AssetDatabase.AddObjectToAsset(newCriterion, this);
                        tc.Criterion = newCriterion;
                        criterionMap[oldCriterion] = newCriterion;
                    }
                }

                // Copy FutureObjectReferences and remap them to the new criteria
                var futureRefMap = new Dictionary<FutureObjectReference, FutureObjectReference>();
                foreach (UnityObject obj in oldAssets)
                {
                    if (obj is not FutureObjectReference oldFutureRef) continue;
                    if (oldFutureRef.Criterion == null) continue;
                    if (!criterionMap.TryGetValue(oldFutureRef.Criterion, out Criterion newCriterion)) continue;

                    FutureObjectReference newFutureRef = Instantiate(oldFutureRef);
                    newFutureRef.Criterion = newCriterion;
                    newFutureRef.hideFlags |= HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(newFutureRef, this);
                    futureRefMap[oldFutureRef] = newFutureRef;
                }

                // Remap FutureObjectReference fields inside each new criterion
                foreach (Criterion newCriterion in criterionMap.Values)
                {
                    RemapFutureObjectReferences(newCriterion, futureRefMap);
                }

                TutorialPageEditor.RenamePage(newPage);

                AssetDatabase.DeleteAsset(oldPagePath);
            }
        }

        /// <summary>
        /// Iterates all serialized fields of the given object and replaces any FutureObjectReference
        /// references found in the mapping dictionary with the mapped value.
        /// Used during v6 migration to update criterion fields after copying sub-assets.
        /// </summary>
        static void RemapFutureObjectReferences(UnityObject target, Dictionary<FutureObjectReference, FutureObjectReference> refMap)
        {
            if (refMap.Count == 0) return;

            var so = new SerializedObject(target);
            SerializedProperty prop = so.GetIterator();
            bool modified = false;

            while (prop.Next(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.objectReferenceValue is FutureObjectReference oldRef
                    && refMap.TryGetValue(oldRef, out FutureObjectReference newRef))
                {
                    prop.objectReferenceValue = newRef;
                    modified = true;
                }
            }

            if (modified)
                so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal void MigrateSceneBehaviourV6()
        {
            if (Scenes?.Length > 0)
            {
                SceneManagementBehavior = SceneManagementBehaviorType.LoadScenes;
            }
        }
    }
}
