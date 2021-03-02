using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// A TutorialPage consists of TutorialParagraphs which define the content of the page.
    /// </summary>
    public class TutorialPage : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when any page's criteria are tested for completion.
        /// </summary>
        public static event Action<TutorialPage> CriteriaCompletionStateTested;

        // TODO 2.0 merge these two events and provide event data which tells which type of change we had?

        /// <summary>
        /// Raised when any page's masking settings are changed.
        /// </summary>
        public static event Action<TutorialPage> TutorialPageMaskingSettingsChanged;

        /// <summary>
        /// Raised when any page's non-masking settings are changed.
        /// </summary>
        public static event Action<TutorialPage> TutorialPageNonMaskingSettingsChanged;

        internal event Action<TutorialPage> m_PlayedCompletionSound;

        /// <summary>
        /// Are we moving to the next page?
        /// </summary>
        public bool HasMovedToNextPage { get; private set; }

        /// <summary>
        /// Are all criteria satisfied?
        /// </summary>
        public bool AreAllCriteriaSatisfied { get; private set; }

        /// <summary>
        /// Paragraphs of this page.
        /// </summary>
        public TutorialParagraphCollection Paragraphs => m_Paragraphs;
        [SerializeField]
        internal TutorialParagraphCollection m_Paragraphs = new TutorialParagraphCollection();

        /// <summary>
        /// Has the current page any completion criteria?
        /// </summary>
        /// <returns></returns>
        public bool HasCriteria()
        {
            foreach (TutorialParagraph para in Paragraphs)
            {
                foreach (TypedCriterion crit in para.Criteria)
                {
                    if (crit.Criterion != null) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Currently active masking settings.
        /// </summary>
        internal MaskingSettings CurrentMaskingSettings
        {
            get
            {
                MaskingSettings result = null;
                for (int i = 0, count = m_Paragraphs.Count; i < count; ++i)
                {
                    if (!m_Paragraphs[i].MaskingSettings.Enabled) { continue; }

                    result = m_Paragraphs[i].MaskingSettings;
                    if (!m_Paragraphs[i].Completed)
                        break;
                }
                return result;
            }
        }

        [Header("Initial Camera Settings")]
        [SerializeField]
        SceneViewCameraSettings m_CameraSettings = new SceneViewCameraSettings();

        /// <summary>
        /// The text shown on the Next button on all pages except the last page.
        /// </summary>
        [Header("Button Labels")]
        [Tooltip("The text shown on the next button on all pages except the last page.")]
        public LocalizableString NextButton = "Next";

        /// <summary>
        /// The text shown on the next button on the last page.
        /// </summary>
        [Tooltip("The text shown on the Next button on the last page.")]
        public LocalizableString DoneButton = "Done";

        // Backwards-compatibility for < 1.2
        [SerializeField, HideInInspector]
        string m_NextButton = "Next";
        [SerializeField, HideInInspector]
        string m_DoneButton = "Done";

        /// <summary>
        /// Returns the asset database GUID of this asset.
        /// </summary>
        public string Guid => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));

        [Header("Sounds")]
        [SerializeField]
        AudioClip m_CompletedSound = null;

        /// <summary>
        /// Should we auto-advance upon completion.
        /// </summary>
        public bool AutoAdvanceOnComplete { get => m_autoAdvance; set => m_autoAdvance = value; }
        [Header("Auto advance on complete?")]
        [SerializeField]
        bool m_autoAdvance;

        // TODO 2.0 check the naming of these events
        [Header("Callbacks")]
        [SerializeField]
        [Tooltip("These methods will be called right before the page is displayed (even when going back)")]
        internal UnityEvent m_OnBeforePageShown = default;

        [Tooltip("These methods will be called right after the page is displayed (even when going back)")]
        [SerializeField]
        internal UnityEvent m_OnAfterPageShown = default;

        [Tooltip("These methods will be called when the user force-quits the tutorial from this tutorial page, before quitting the tutorial")]
        [SerializeField]
        internal UnityEvent m_OnBeforeTutorialQuit = default;

        [Tooltip("These methods will be called while the user is reading this tutorial page, every editor frame")]
        [SerializeField]
        internal UnityEvent m_OnTutorialPageStay = default;

        static Queue<WeakReference<TutorialPage>> s_DeferedValidationQueue = new Queue<WeakReference<TutorialPage>>();

        /// <summary> TODO 2.0 Make internal. </summary>
        public void RaiseTutorialPageMaskingSettingsChangedEvent()
        {
            TutorialPageMaskingSettingsChanged?.Invoke(this);
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void RaiseTutorialPageNonMaskingSettingsChangedEvent()
        {
            TutorialPageNonMaskingSettingsChanged?.Invoke(this);
        }

        static TutorialPage()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            while (s_DeferedValidationQueue.Count != 0)
            {
                var weakPageReference = s_DeferedValidationQueue.Dequeue();
                TutorialPage page;
                if (weakPageReference.TryGetTarget(out page))
                {
                    if (page != null) //Taking into account "unity null"
                    {
                        page.SyncCriteriaAndFutureReferences();
                    }
                }
            }
        }

        void OnValidate()
        {
            // Defer synchronization of sub-assets to next editor update due to AssetDatabase interactions

            // Retaining a reference to this instance in OnValidate/OnEnable can cause issues on project load
            // The same object might be imported more than once and if it's referenced it won't be unloaded correctly
            // Use WeakReference instead of subscribing directly to EditorApplication.update to avoid strong reference

            s_DeferedValidationQueue.Enqueue(new WeakReference<TutorialPage>(this));
        }

        void SyncCriteriaAndFutureReferences()
        {
            // Find instanceIDs of referenced criteria
            var referencedCriteriaInstanceIDs = new HashSet<int>();
            foreach (var paragraph in Paragraphs)
            {
                foreach (var typedCriterion in paragraph.Criteria)
                {
                    if (typedCriterion.Criterion != null)
                        referencedCriteriaInstanceIDs.Add(typedCriterion.Criterion.GetInstanceID());
                }
            }

            // Destroy unreferenced criteria
            var assetPath = AssetDatabase.GetAssetPath(this);
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var criteria = assets.Where(o => o is Criterion).Cast<Criterion>();
            foreach (var criterion in criteria)
            {
                if (!referencedCriteriaInstanceIDs.Contains(criterion.GetInstanceID()))
                    DestroyImmediate(criterion, true);
            }

            // Update future reference names
            var futureReferences = assets.Where(o => o is FutureObjectReference).Cast<FutureObjectReference>();
            foreach (var futureReference in futureReferences)
            {
                if (futureReference.Criterion == null
                    || !referencedCriteriaInstanceIDs.Contains(futureReference.Criterion.GetInstanceID()))
                {
                    // Destroy future reference from unrefereced criteria
                    DestroyImmediate(futureReference, true);
                }
                else
                    UpdateFutureObjectReferenceName(futureReference);
            }
        }

        /// <summary>
        /// TODO 2.0 Make internal.
        /// </summary>
        /// <param name="futureReference"></param>
        public void UpdateFutureObjectReferenceName(FutureObjectReference futureReference)
        {
            int paragraphIndex;
            int criterionIndex;
            if (GetIndicesForCriterion(futureReference.Criterion, out paragraphIndex, out criterionIndex))
            {
                futureReference.name = string.Format("Paragraph {0}, Criterion {1}, {2}",
                    paragraphIndex + 1, criterionIndex + 1, futureReference.ReferenceName);
            }
        }

        bool GetIndicesForCriterion(Criterion criterion, out int paragraphIndex, out int criterionIndex)
        {
            paragraphIndex = 0;
            criterionIndex = 0;

            foreach (var paragraph in Paragraphs)
            {
                foreach (var typedCriterion in paragraph.Criteria)
                {
                    if (typedCriterion.Criterion == criterion)
                        return true;

                    criterionIndex++;
                }

                paragraphIndex++;
            }

            return false;
        }

        internal void Initiate()
        {
            SetupCompletionRequirements();
            if (m_CameraSettings != null && m_CameraSettings.Enabled)
            {
                m_CameraSettings.Apply();
            }
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void ResetUserProgress()
        {
            RemoveCompletionRequirements();
            foreach (var paragraph in Paragraphs)
            {
                if (paragraph.Type == ParagraphType.Instruction)
                {
                    foreach (var criteria in paragraph.Criteria)
                    {
                        if (criteria != null && criteria.Criterion != null)
                        {
                            criteria.Criterion.ResetCompletionState();
                            criteria.Criterion.StopTesting();
                        }
                    }
                }
            }
            AreAllCriteriaSatisfied = false;
            HasMovedToNextPage = false;
        }

        internal void SetupCompletionRequirements()
        {
            ValidateCriteria();
            if (HasMovedToNextPage)
                return;

            Criterion.CriterionCompleted += OnCriterionCompleted;
            Criterion.CriterionInvalidated += OnCriterionInvalidated;

            foreach (var paragraph in Paragraphs)
            {
                if (paragraph.Criteria != null)
                {
                    foreach (var criterion in paragraph.Criteria)
                    {
                        if (criterion.Criterion)
                            criterion.Criterion.StartTesting();
                    }
                }
            }
        }

        internal void RemoveCompletionRequirements()
        {
            Criterion.CriterionCompleted -= OnCriterionCompleted;
            Criterion.CriterionInvalidated -= OnCriterionInvalidated;

            foreach (var paragraph in Paragraphs)
            {
                if (paragraph.Criteria != null)
                {
                    foreach (var criterion in paragraph.Criteria)
                    {
                        if (criterion.Criterion)
                        {
                            criterion.Criterion.StopTesting();
                        }
                    }
                }
            }
        }

        void OnCriterionCompleted(Criterion sender)
        {
            if (!m_Paragraphs.Any(p => p.Criteria.Any(c => c.Criterion == sender)))
                return;

            if (sender.Completed)
            {
                int paragraphIndex, criterionIndex;
                if (GetIndicesForCriterion(sender, out paragraphIndex, out criterionIndex))
                {
                    // only play sound effect and clear undo if all preceding criteria are already complete
                    var playSoundEffect = true;
                    for (int i = 0; i < paragraphIndex; ++i)
                    {
                        if (!m_Paragraphs[i].Criteria.All(c => c.Criterion.Completed))
                        {
                            playSoundEffect = false;
                            break;
                        }
                    }
                    if (playSoundEffect)
                    {
                        Undo.ClearAll();
                        if (m_CompletedSound != null)
                            AudioUtilProxy.PlayClip(m_CompletedSound);
                        m_PlayedCompletionSound?.Invoke(this);
                    }
                }
            }
            ValidateCriteria();
        }

        void OnCriterionInvalidated(Criterion sender)
        {
            if (m_Paragraphs.Any(p => p.Criteria.Any(c => c.Criterion == sender)))
                ValidateCriteria();
        }

        internal void ValidateCriteria()
        {
            AreAllCriteriaSatisfied = true;

            foreach (var paragraph in Paragraphs)
            {
                if (paragraph.Type == ParagraphType.Instruction)
                {
                    if (!paragraph.Completed)
                    {
                        AreAllCriteriaSatisfied = false;
                        break;
                    }
                }

                if (!AreAllCriteriaSatisfied)
                    break;
            }

            CriteriaCompletionStateTested?.Invoke(this);
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void OnPageCompleted()
        {
            RemoveCompletionRequirements();
            HasMovedToNextPage = true;
        }

        /// <summary>
        /// Called when the frontend of the page has not been displayed yet to the user
        /// TODO 2.0 Make internal.
        /// </summary>
        public void RaiseOnBeforePageShownEvent()
        {
            m_OnBeforePageShown?.Invoke();
        }

        /// <summary>
        /// Called right after the frontend of the page is displayed to the user
        /// TODO 2.0 Make internal.
        /// </summary>
        public void RaiseOnAfterPageShownEvent()
        {
            m_OnAfterPageShown?.Invoke();
        }

        /// <summary>
        /// Called when the user force-quits the tutorial from this tutorial page, before quitting the tutorial
        /// </summary>
        internal void RaiseOnBeforeQuitTutorialEvent()
        {
            m_OnBeforeTutorialQuit?.Invoke();
        }

        /// <summary>
        /// Called while the user is reading this tutorial page, every editor frame
        /// </summary>
        internal void RaiseOnTutorialPageStayEvent()
        {
            m_OnTutorialPageStay?.Invoke();
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
            TutorialParagraph.MigrateStringToLocalizableString(ref m_NextButton, ref NextButton);
            TutorialParagraph.MigrateStringToLocalizableString(ref m_DoneButton, ref DoneButton);
        }
    }
}
