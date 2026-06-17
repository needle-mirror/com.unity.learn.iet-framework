using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A generic event for signaling changes in a tutorial page.
    /// Parameters: sender.
    /// </summary>
    [Serializable]
    public class TutorialPageEvent : UnityEvent<TutorialPage>
    {
    }

    /// <summary>
    /// A TutorialPage consists of TutorialParagraphs which define the content of the page.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class TutorialPage : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when any page's criteria are tested for completion.
        /// </summary>
        public static TutorialPageEvent CriteriaCompletionStateTested = new();

        /// <summary>
        /// Raised when any page's masking settings are changed.
        /// </summary>
        public static TutorialPageEvent TutorialPageMaskingSettingsChanged = new();

        /// <summary>
        /// Raised when any page's non-masking settings are changed.
        /// </summary>
        public static TutorialPageEvent TutorialPageNonMaskingSettingsChanged = new();

        internal event Action<TutorialPage> m_PlayedCompletionSound;

        /// <summary>
        /// The index in the <see cref="Tutorial"/> that contains it. Used for renaming the asset.
        /// </summary>
        [field: SerializeField] internal int IndexInTutorial { get; set; }

        /// <summary>
        /// Title of the page
        /// </summary>
        [SerializeField]
        [Header("Contents")]
        [Tooltip("Title shown in the card/header.")]
        public LocalizableString Title = new();

        /// <summary>
        /// Are we moving to the next page?
        /// </summary>
        public bool HasMovedToNextPage { get; private set; }

        /// <summary>
        /// Are all criteria satisfied?
        /// </summary>
        public bool AreAllCriteriaSatisfied => Paragraphs.All(p => p.IsCompleted());

        /// <summary>
        /// Legacy Paragraphs of this page. Need migration to V6 format
        /// </summary>
        internal TutorialParagraphCollection LegacyParagraphs => m_LegacyParagraphs;

        [FormerlySerializedAs("m_Paragraphs")]
        [SerializeField]
        internal TutorialParagraphCollection m_LegacyParagraphs = new();

        /// <summary>
        /// Paragraphs contained in this page.
        /// </summary>
        public List<ParagraphBase> Paragraphs => m_PageParagraphs;
        [SerializeField]
        internal List<ParagraphBase> m_PageParagraphs = new();

        /// <summary>
        /// Currently active masking settings.
        /// </summary>
        internal MaskingSettings CurrentMaskingSettings
        {
            get
            {
                MaskingSettings result = null;
                for (int i = 0, count = m_PageParagraphs.Count; i < count; ++i)
                {
                    if (!m_PageParagraphs[i].MaskingSettings.Enabled)
                        continue;

                    result = m_PageParagraphs[i].MaskingSettings;
                    if (!m_PageParagraphs[i].IsCompleted())
                        break;
                }
                return result;
            }
        }

        [Header("Settings")]
        [SerializeField]
        private SceneViewCameraSettings m_CameraSettings = new();

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

        [Header("Sounds")]
        [SerializeField]
        private AudioClip m_CompletedSound;

        /// <summary>
        /// Faq Entries for that specific page
        /// </summary>
        [FormerlySerializedAs("m_FAQEntries")]
        [SerializeField]
        public FaqEntry[] m_FaqEntries = Array.Empty<FaqEntry>();

        /// <summary>
        /// Should we auto-advance upon completion.
        /// </summary>
        public bool AutoAdvanceOnComplete { get => m_AutoAdvance; set => m_AutoAdvance = value; }
        [SerializeField, FormerlySerializedAs("m_autoAdvance"), Tooltip("Should we auto-advance upon completion.")]
        internal bool m_AutoAdvance;

        // Header attribute disabled because it was appearing in the custom inspector
        //[Header("Events")]

        /// <summary>
        /// Raised before this page is displayed (even when going back).
        /// </summary>
        [Tooltip("Raised before this page is displayed (even when going back).")]
        public TutorialPageEvent Showing = new();

        /// <summary>
        /// Raised after this page is displayed (even when going back).
        /// </summary>
        [Tooltip("Raised after this page is displayed (even when going back).")]
        public TutorialPageEvent Shown = new();

        /// <summary>
        /// Raised while the user is staying on this tutorial page, every Editor frame.
        /// </summary>
        [Tooltip("Raised while the user is staying on this tutorial page, every Editor frame.")]
        public TutorialPageEvent Staying = new();

        /// <summary>
        /// Raised when this page's criteria are tested for completion.
        /// </summary>
        [Tooltip("Raised when this page's criteria are tested for completion.")]
        public TutorialPageEvent CriteriaValidated = new();

        /// <summary>
        /// Raised when this page's masking settings are changed.
        /// </summary>
        [Tooltip("Raised when this page's masking settings are changed.")]
        public TutorialPageEvent MaskingSettingsChanged = new();

        /// <summary>
        /// Raised when this page's non-masking settings are changed.
        /// </summary>
        [Tooltip("Raised when this page's non-masking settings are changed.")]
        public TutorialPageEvent NonMaskingSettingsChanged = new();

        private static Queue<WeakReference<TutorialPage>> s_DeferedValidationQueue = new();

        // Backwards-compatibility for < 2.0.0-pre.6
        [SerializeField, HideInInspector] internal UnityEvent m_OnBeforePageShown;
        [SerializeField, HideInInspector] internal UnityEvent m_OnAfterPageShown;
        [SerializeField, HideInInspector] internal UnityEvent m_OnTutorialPageStay;
        [SerializeField, Tooltip("This event will be deprecated, please migrate to use Tutorial's Quit event instead.")]
        internal UnityEvent m_OnBeforeTutorialQuit;

        // Backwards-compatibility for < 1.2
        [SerializeField, HideInInspector] private string m_NextButton = "Next";
        [SerializeField, HideInInspector] private string m_DoneButton = "Done";

        /// <summary>
        /// Raises TutorialPageMaskingSettingsChanged event.
        /// </summary>
        public void RaiseMaskingSettingsChanged()
        {
            MaskingSettingsChanged?.Invoke(this);
            TutorialPageMaskingSettingsChanged?.Invoke(this);
        }

        /// <summary>
        /// Raises TutorialPageNonMaskingSettingsChanged event.
        /// </summary>
        public void RaiseNonMaskingSettingsChanged()
        {
            NonMaskingSettingsChanged?.Invoke(this);
            TutorialPageNonMaskingSettingsChanged?.Invoke(this);
        }

        // static TutorialPage()
        // {
        //     EditorApplication.update += OnEditorUpdate;
        // }

        private static void OnEditorUpdate()
        {
            while (s_DeferedValidationQueue.Count != 0)
            {
                WeakReference<TutorialPage> weakPageReference = s_DeferedValidationQueue.Dequeue();
                if (weakPageReference.TryGetTarget(out TutorialPage page))
                {
                    if (page != null) //Taking into account "unity null"
                    {
                        page.SyncCriteriaAndFutureReferences();
                    }
                }
            }
        }

        private void OnEnable()
        {
            // Migrate content from < 2.0.0-pre.6
            // NOTE events are migrated in OnEnable() instead of OnAfterDeserialize() due to the use of SerializedObject:
            // "UnityException: InternalCreate is not allowed to be called during serialization,
            // call it from OnEnable instead. Called from ScriptableObject 'TutorialPage'."
            if (m_OnBeforePageShown != null && m_OnBeforePageShown.GetPersistentEventCount() > 0)
            {
                TransferPersistentCalls(this, nameof(m_OnBeforePageShown), nameof(Showing));
                Debug.Log($"{AssetDatabase.GetAssetPath(this)}: OnBeforePageShown event is deprecated, migrated to use Showing automatically.");
            }

            if (m_OnAfterPageShown != null && m_OnAfterPageShown.GetPersistentEventCount() > 0)
            {
                TransferPersistentCalls(this, nameof(m_OnBeforePageShown), nameof(Shown));
                Debug.Log($"{AssetDatabase.GetAssetPath(this)}: OnAfterPageShown event is be deprecated, migrated to use Shown automatically.");
            }

            if (m_OnBeforeTutorialQuit != null && m_OnBeforeTutorialQuit.GetPersistentEventCount() > 0)
            {
                // A page doesn't have an explicit parent tutorial, and page can be in multiple tutorials; the users must migrate this event on their own.
                Debug.LogWarning($"{AssetDatabase.GetAssetPath(this)}: OnBeforeTutorialQuit event is deprecated, please migrate to use Tutorial's Quit event instead.");
            }

            if (m_OnTutorialPageStay != null && m_OnTutorialPageStay.GetPersistentEventCount() > 0)
            {
                TransferPersistentCalls(this, nameof(m_OnTutorialPageStay), nameof(Staying));
                Debug.Log($"{AssetDatabase.GetAssetPath(this)}: OnTutorialPageStay event is deprecated, asset migrated to use Staying automatically.");
            }
        }

        private void OnValidate()
        {
            foreach (ParagraphBase paragraph in Paragraphs)
            {
                paragraph?.Validate();
            }

            // Defer synchronization of sub-assets to next editor update due to AssetDatabase interactions

            // Retaining a reference to this instance in OnValidate/OnEnable can cause issues on project load
            // The same object might be imported more than once and if it's referenced it won't be unloaded correctly
            // Use WeakReference instead of subscribing directly to EditorApplication.update to avoid strong reference

            s_DeferedValidationQueue.Enqueue(new WeakReference<TutorialPage>(this));
        }
        
        private void SyncCriteriaAndFutureReferences()
        {
            // Find IDs of referenced criteria
#if UNITY_6000_3_OR_NEWER
            HashSet<EntityId> referencedCriteriaIDs = new();
#else
            HashSet<int> referencedCriteriaIDs = new();
#endif
            
            foreach (ParagraphBase paragraph in Paragraphs)
            {
                TypedCriterionCollection paragraphCriteria = paragraph.Criterias();
                if (paragraphCriteria == null) continue;

                foreach (TypedCriterion criterion in paragraphCriteria)
                {
                    if (criterion.Criterion != null)
                             referencedCriteriaIDs.Add(IdUtils.GetIdFor(criterion.Criterion));
                }
            }

            // Do it ALSO for legacy paragraph as this could be in the process of being converted to V6 version
            foreach (TutorialParagraph legacyParagraph in LegacyParagraphs)
            {
                foreach (TypedCriterion typedCriterion in legacyParagraph.Criteria)
                {
                    if (typedCriterion.Criterion != null)
                        referencedCriteriaIDs.Add(IdUtils.GetIdFor(typedCriterion.Criterion));
                }
            }

            // Destroy unreferenced criteria
            string assetPath = AssetDatabase.GetAssetPath(this);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            IEnumerable<Criterion> criteria = assets.Where(o => o is Criterion).Cast<Criterion>();
            foreach (Criterion criterion in criteria)
            {
                if (!referencedCriteriaIDs.Contains(IdUtils.GetIdFor(criterion)))
                    DestroyImmediate(criterion, true);
            }

            // Update future reference names
            IEnumerable<FutureObjectReference> futureReferences = assets.Where(o => o is FutureObjectReference).Cast<FutureObjectReference>();
            foreach (FutureObjectReference futureReference in futureReferences)
            {
                if (futureReference.Criterion == null
                    || !referencedCriteriaIDs.Contains(IdUtils.GetIdFor(futureReference.Criterion)))
                {
                    // Destroy future reference from unreferenced criteria
                    DestroyImmediate(futureReference, true);
                }
                else
                    UpdateFutureObjectReferenceName(futureReference);
            }
        }

        internal void UpdateFutureObjectReferenceName(FutureObjectReference futureReference)
        {
            if (GetIndicesForCriterion(futureReference.Criterion, out int paragraphIndex, out int criterionIndex))
            {
                futureReference.name = string.Format("Paragraph {0}, Criterion {1}, {2}",
                    paragraphIndex + 1, criterionIndex + 1, futureReference.ReferenceName);
            }
        }

        private bool GetIndicesForCriterion(Criterion criterion, out int paragraphIndex, out int criterionIndex)
        {
            paragraphIndex = 0;
            criterionIndex = 0;

            foreach (ParagraphBase paragraph in Paragraphs)
            {
                if (paragraph.HasCriteria())
                {
                    criterionIndex = 0;
                    foreach (TypedCriterion pCriteria in paragraph.Criterias())
                    {
                        if (pCriteria.Criterion == criterion)
                            return true;
                        criterionIndex++;
                    }
                }

                paragraphIndex++;
            }

            return false;
        }

        internal void ApplyCameraSettings()
        {
            if (m_CameraSettings != null && m_CameraSettings.Enabled)
            {
                m_CameraSettings.Apply();
            }
        }

        internal void PlayCompletionSound()
        {
            Undo.ClearAll(); //TODO: investigate why this is needed
            if (m_CompletedSound != null)
            {
                AudioUtilProxy.PlayClip(m_CompletedSound);
            }
            m_PlayedCompletionSound?.Invoke(this);
        }

        internal void Initiate()
        {
            ApplyCameraSettings();
        }

        internal void SetupCompletionCriteria(UnityAction<Criterion, ParagraphBase> onCriterionCompleted, UnityAction<Criterion, ParagraphBase> onCriterionInvalidated, UnityAction<TutorialPage> onPageCompletionStatusChangedOrSet = null)
        {
            foreach (ParagraphBase paragraph in Paragraphs)
            {
                if (paragraph.HasCriteria())
                {
                    if (paragraph.Criterias() == null) { continue; }
                    foreach (TypedCriterion criterion in paragraph.Criterias())
                    {
                        if (criterion.Criterion)
                        {
                            criterion.Criterion.Completed.AddListener(crit => { OnCriterionCompleted(crit); onCriterionCompleted.Invoke(crit, paragraph); paragraph.OnCriterionUpdated();});
                            criterion.Criterion.Invalidated.AddListener(crit => { OnCriterionInvalidated(crit); onCriterionInvalidated.Invoke(crit, paragraph); paragraph.OnCriterionUpdated();});
                            criterion.Criterion.StartTesting();
                        }
                    }
                }
            }

            CriteriaCompletionStateTested.RemoveAllListeners();
            if (onPageCompletionStatusChangedOrSet != null)
            {
                CriteriaCompletionStateTested.AddListener(onPageCompletionStatusChangedOrSet);
            }
            OnCompletionCriteriaStatusChangedOrSet();
        }

        internal void ResetUserProgressAndCompletionCriteria()
        {
            foreach (ParagraphBase paragraph in Paragraphs)
            {
                if (paragraph.HasCriteria())
                {
                    if (paragraph.Criterias() == null)
                    {
                        continue;
                    }

                    foreach (TypedCriterion criterion in paragraph.Criterias())
                    {
                        if (criterion != null && criterion.Criterion != null)
                        {
                            criterion.Criterion.Completed.RemoveAllListeners();
                            criterion.Criterion.Invalidated.RemoveAllListeners();
                            criterion.Criterion.StopTesting();
                            criterion.Criterion.ResetCompletionState();
                        }
                    }
                }
            }
            HasMovedToNextPage = false;
        }

        private void OnCriterionCompleted(Criterion sender)
        {
            OnCompletionCriteriaStatusChangedOrSet();
        }

        private void OnCriterionInvalidated(Criterion sender)
        {
            OnCompletionCriteriaStatusChangedOrSet();
        }

        private void OnCompletionCriteriaStatusChangedOrSet()
        {
            CriteriaValidated?.Invoke(this);
            CriteriaCompletionStateTested?.Invoke(this);
        }

        internal void MarkAsCompleted()
        {
            ResetUserProgressAndCompletionCriteria();
            HasMovedToNextPage = true;
            //todo: add page-specific analytics here?
        }

        internal void RaiseShowing()
        {
            Showing?.Invoke(this);
            m_OnBeforePageShown?.Invoke();
        }

        internal void RaiseShown()
        {
            Shown?.Invoke(this);
            m_OnAfterPageShown?.Invoke();
        }

        internal void RaiseOnBeforeTutorialQuit()
        {
            m_OnBeforeTutorialQuit?.Invoke();
        }

        internal void RaiseStaying()
        {
            Staying?.Invoke(this);
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
            MigrateContentFromV1ToV2();
            MigrateContentFromV2ToV3();
        }

        /// <summary>
        /// Migrate content from < 1.2.
        /// </summary>
        private void MigrateContentFromV1ToV2()
        {
            TutorialParagraph.MigrateStringToLocalizableString(ref m_NextButton, ref NextButton);
            TutorialParagraph.MigrateStringToLocalizableString(ref m_DoneButton, ref DoneButton);
        }

        /// <summary>
        /// Migrate content from < 3.0
        /// </summary>
        private void MigrateContentFromV2ToV3()
        {
            if (Title.Untranslated.IsNullOrEmpty())
            {
                if (LegacyParagraphs.Count > 1)
                {
                    if (LegacyParagraphs[1].Title != null //in previous version, the title of the 2nd paragraph, which was always narrative, was the title ofthe page
                        && LegacyParagraphs[1].Title.Untranslated.IsNotNullOrEmpty())
                    {
                        Title = LegacyParagraphs[1].Title.Untranslated;
                        LegacyParagraphs[1].Title = string.Empty;
                    }
                }
            }
        }

        // Migrate old style TutorialParagraph to new ParagraphBase system in v6
        internal void MigrateToV6()
        {
            foreach (TutorialParagraph obsoleteParagraph in LegacyParagraphs)
            {
                switch (obsoleteParagraph.Type)
                {
                    case ParagraphType.Narrative:
                    {
                        NarrativeParagraph np = AddParagraph<NarrativeParagraph>();
                        np.Text = obsoleteParagraph.Text;
                        np.MaskingSettings.CopySettingsFrom(obsoleteParagraph.MaskingSettings);

                        break;
                    }
                    case ParagraphType.Media:
                    {
                        MediaParagraph mp = AddParagraph<MediaParagraph>();
                        mp.Media = obsoleteParagraph.Media;
                        mp.MaskingSettings.CopySettingsFrom(obsoleteParagraph.MaskingSettings);

                        break;
                    }
                    case ParagraphType.Instruction:
                    {
                        InstructionsParagraph ip = AddParagraph<InstructionsParagraph>();
                        ip.Title = obsoleteParagraph.Title;
                        ip.Text = obsoleteParagraph.Text;

                        // Only Instruction paragraphs have masking
                        ip.MaskingSettings.CopySettingsFrom(obsoleteParagraph.MaskingSettings);

                        // Only Instruction paragraphs have criteria
                        foreach(TypedCriterion c in obsoleteParagraph.Criteria)
                        {
                            ip.m_Criteria.AddItem(c);
                        }

                        break;
                    }
                    case ParagraphType.SwitchTutorial:
                    {
                        NextTutorialButtonParagraph bp = AddParagraph<NextTutorialButtonParagraph>();
                        bp.ButtonText = obsoleteParagraph.Text;
                        bp.NextTutorial = obsoleteParagraph.m_Tutorial;

                        break;
                    }
                }

                if (obsoleteParagraph.CodeSample.IsNotNullOrEmpty())
                {
                    CodeSampleParagraph c = AddParagraph<CodeSampleParagraph>();
                    c.CodeSample = obsoleteParagraph.CodeSample;
                }

                if (obsoleteParagraph.PostInstructionImage != null)
                {
                    MediaParagraph mp = AddParagraph<MediaParagraph>();
                    mp.Media = new MediaContent
                    {
                        ContentType = MediaContent.MediaContentType.Image,
                        Image = obsoleteParagraph.PostInstructionImage
                    };
                }
            }

#pragma warning disable CS0618
            m_LegacyParagraphs = new();
#pragma warning restore CS061
        }

        internal ParagraphBase AddParagraph(Type paragraphType)
        {
            ParagraphBase newParagraph = CreateInstance(paragraphType) as ParagraphBase;
            Undo.RegisterCreatedObjectUndo(newParagraph, "Created new Paragraph");
            newParagraph!.hideFlags |= HideFlags.HideInHierarchy;
            newParagraph.name = paragraphType.Name;
            AssetDatabase.AddObjectToAsset(newParagraph, this);
            AssetDatabase.SaveAssets();

            SerializedObject serializedObject = new(this);
            SerializedProperty list = serializedObject.FindProperty(nameof(m_PageParagraphs));
            list.arraySize += 1;
            SerializedProperty newElement = list.GetArrayElementAtIndex(list.arraySize - 1);
            newElement.objectReferenceValue = newParagraph;
            serializedObject.ApplyModifiedProperties();

            return newParagraph;
        }

        internal T AddParagraph<T>() where T : ParagraphBase
        {
            return AddParagraph(typeof(T)) as T;
        }

        internal static TutorialPage Create(params TutorialParagraph[] paragraphs)
        {
            TutorialPage page = CreateInstance<TutorialPage>();
            page.LegacyParagraphs.SetItems(paragraphs);
            return page;
        }


        // Based on https://gist.github.com/wesleywh/1c56d880c0289371ea2dc47661a0cdaf
        private static void TransferPersistentCalls(Object obj, in string srcEventName, in string dstEventName)
        {
            SerializedObject so = new(obj);
            const string CallsPropertyPathFormat = "{0}.m_PersistentCalls.m_Calls";
            SerializedProperty srcCalls = so.FindProperty(string.Format(CallsPropertyPathFormat, GetValidFieldName(srcEventName.Trim())));
            SerializedProperty dstCalls = so.FindProperty(string.Format(CallsPropertyPathFormat, GetValidFieldName(dstEventName.Trim())));
            int dstOffset = dstCalls.arraySize;

            for (int srcIndex = 0; srcIndex < srcCalls.arraySize; srcIndex++)
            {
                SerializedProperty srcCall = srcCalls.GetArrayElementAtIndex(srcIndex);
                SerializedProperty srcTarget = GetCallTarget(srcCall);
                SerializedProperty srcMethodName = GetCallMethodName(srcCall);
                SerializedProperty srcMode = GetCallMode(srcCall);
                SerializedProperty srcCallState = GetCallState(srcCall);
                SerializedProperty srcArgs = GetCallArgs(srcCall);
                SerializedProperty srcObjectArg = GetCallObjectArg(srcArgs);
                SerializedProperty srcObjectArgType = GetCallObjectArgType(srcArgs);
                SerializedProperty srcIntArg = GetCallIntArg(srcArgs);
                SerializedProperty srcFloatArg = GetCallFloatArg(srcArgs);
                SerializedProperty srcStringArg = GetCallStringArg(srcArgs);
                SerializedProperty srcBoolArg = GetCallBoolArg(srcArgs);

                SerializedProperty dstCall;
                if (dstOffset > 0)
                {
                    dstCall = dstCalls.GetArrayElementAtIndex(srcIndex);
                    // If we are satisfied that the call is exactly the same, skip ahead.
                    if (SerializedProperty.DataEquals(srcCall, dstCall))
                        continue;
                }

                // Only unique properties beyond this point. Append with care.
                // Copy Properties from Source to Destination
                dstCalls.InsertArrayElementAtIndex(dstOffset + srcIndex);
                dstCall = dstCalls.GetArrayElementAtIndex(dstOffset + srcIndex);

                SerializedProperty dstTarget = GetCallTarget(dstCall);
                SerializedProperty dstMethodName = GetCallMethodName(dstCall);
                SerializedProperty dstMode = GetCallMode(dstCall);
                SerializedProperty dstCallState = GetCallState(dstCall);
                SerializedProperty dstArgs = GetCallArgs(dstCall);
                SerializedProperty dstObjectArg = GetCallObjectArg(dstArgs);
                SerializedProperty dstObjectArgType = GetCallObjectArgType(dstArgs);
                SerializedProperty dstIntArg = GetCallIntArg(dstArgs);
                SerializedProperty dstFloatArg = GetCallFloatArg(dstArgs);
                SerializedProperty dstStringArg = GetCallStringArg(dstArgs);
                SerializedProperty dstBoolArg = GetCallBoolArg(dstArgs);

                dstTarget.objectReferenceValue = srcTarget.objectReferenceValue;
                dstMethodName.stringValue = srcMethodName.stringValue;
                dstMode.enumValueIndex = srcMode.enumValueIndex;
                dstCallState.enumValueIndex = srcCallState.enumValueIndex;

                dstObjectArg.objectReferenceValue = srcObjectArg.objectReferenceValue;
                dstObjectArgType.stringValue = srcObjectArgType.stringValue;
                dstIntArg.intValue = srcIntArg.intValue;
                dstFloatArg.floatValue = srcFloatArg.floatValue;
                dstStringArg.stringValue = srcStringArg.stringValue;
                dstBoolArg.boolValue = srcBoolArg.boolValue;
            }

            srcCalls.ClearArray();

            so.ApplyModifiedProperties();

            string GetValidFieldName(in string name)
            {
                const BindingFlags bindedTypes = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = obj.GetType().GetField(name, bindedTypes);
                object value = field?.GetValue(obj);
                if (value is UnityEventBase)
                    return name;

                throw new FieldAccessException("Incorrect event name.");
            }

            SerializedProperty GetCallTarget(in SerializedProperty sp) => sp?.FindPropertyRelative("m_Target");
            SerializedProperty GetCallMethodName(in SerializedProperty sp) => sp?.FindPropertyRelative("m_MethodName");
            SerializedProperty GetCallMode(in SerializedProperty sp) => sp?.FindPropertyRelative("m_Mode");
            SerializedProperty GetCallState(in SerializedProperty sp) => sp?.FindPropertyRelative("m_CallState");

            SerializedProperty GetCallArgs(in SerializedProperty sp) => sp?.FindPropertyRelative("m_Arguments");
            SerializedProperty GetCallObjectArg(in SerializedProperty sp) => sp?.FindPropertyRelative("m_ObjectArgument");
            SerializedProperty GetCallObjectArgType(in SerializedProperty sp) => sp?.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName");
            SerializedProperty GetCallIntArg(in SerializedProperty sp) => sp?.FindPropertyRelative("m_IntArgument");
            SerializedProperty GetCallFloatArg(in SerializedProperty sp) => sp?.FindPropertyRelative("m_FloatArgument");
            SerializedProperty GetCallStringArg(in SerializedProperty sp) => sp?.FindPropertyRelative("m_StringArgument");
            SerializedProperty GetCallBoolArg(in SerializedProperty sp) => sp?.FindPropertyRelative("m_BoolArgument");
        }
    }
}
