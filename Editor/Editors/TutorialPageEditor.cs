using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Tutorials.Editor.CustomControl;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    using static Localization;

    [CustomEditor(typeof(TutorialPage))]
    internal class TutorialPageEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        // NOTE: the order here will be used for the UI
        private static readonly string[] k_EventPropertyPaths =
        {
            nameof(TutorialPage.Showing),
            nameof(TutorialPage.Shown),
            nameof(TutorialPage.Staying),
            nameof(TutorialPage.m_OnBeforeTutorialQuit), // This deprecated event cannot be migrated automatically so display it for the user
            nameof(TutorialPage.CriteriaValidated),
            // MaskingSettingsChanged & NonMaskingSettingsChanged exist but are hidden in the simplified view
        };

        private const string k_PageTitleProperty = nameof(TutorialPage.Title);
        private const string k_ParagraphPropertyPath = nameof(TutorialPage.m_PageParagraphs);
        private const string k_ParagraphMaskingSettingsRelativeProperty = "m_MaskingSettings";

        private static readonly Regex s_MatchMaskingSettingsPropertyPath =
            new(
                string.Format(
                    "(^{0}\\.Array\\.size)|(^({0}\\.Array\\.data\\[\\d+\\]\\.{1}\\.))",
                    k_ParagraphPropertyPath, k_ParagraphMaskingSettingsRelativeProperty
                )
            );

        private static bool ShowEvents
        {
            get => SessionState.GetBool("TutorialPageEditor.ShowEvents", false);
            set => SessionState.SetBool("TutorialPageEditor.ShowEvents", value);
        }
        // Non-null/empty if we have created a callback script and waiting for a scriptable object instance to be created for it.
        private static string CallbackAssetPath
        {
            get => SessionState.GetString("iet_creating_SO", string.Empty);
            set => SessionState.SetString("iet_creating_SO", value);
        }

        private TutorialPage Target => (TutorialPage)target;

        [NonSerialized] private string m_WarningMessage;

        private class EventPropertyData
        {
            public SerializedProperty Property;
            public GUIContent Content;
        }

        private List<EventPropertyData> m_Events = new();

        private SerializedProperty m_MaskingSettings;
        private SerializedProperty m_Type;
        private SerializedProperty m_VideoUrl;
        private SerializedProperty m_Video;
        private SerializedProperty m_Image;
        private SerializedProperty m_MediaContent;

        private SerializedProperty m_PageTitle;
        private SerializedProperty m_NarrativeTitle;
        private SerializedProperty m_NarrativeDescription;
        private SerializedProperty m_InstructionTitle;
        private SerializedProperty m_InstructionDescription;
        private SerializedProperty m_CodeSample;
        private SerializedProperty m_PostInstructionImage;

        private SerializedProperty m_CriteriaCompletion;
        private SerializedProperty m_Criteria;
        private SerializedProperty m_AutoAdvance;


        private void OnEnable()
        {
            InitializeSerializedProperties();

            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            serializedObject.Update();
            // No easy way to know which field changed so simply signal all changes.
            Target.RaiseMaskingSettingsChanged();
            Target.RaiseNonMaskingSettingsChanged();
            string path = AssetDatabase.GetAssetPath(Target);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.ImportAsset(path);
            }
            EditorApplication.RepaintProjectWindow();
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            bool targetModified = false;
            bool maskingChanged = false;

            // Paragraphs are sub-assets edited via a separate SerializedObject in ParagraphBaseDrawer,
            // so their UndoPropertyModification.currentValue.target is the paragraph SO, not the TutorialPage.
            HashSet<ParagraphBase> pageParagraphs = null;

            foreach (UndoPropertyModification modification in modifications)
            {
                Object modTarget = modification.currentValue.target;
                string propertyPath = modification.currentValue.propertyPath;

                if (modTarget == target)
                {
                    targetModified = true;
                    if (s_MatchMaskingSettingsPropertyPath.IsMatch(propertyPath))
                    {
                        maskingChanged = true;
                        break;
                    }
                    continue;
                }

                if (modTarget is ParagraphBase paragraph)
                {
                    pageParagraphs ??= new HashSet<ParagraphBase>(Target.Paragraphs);
                    if (!pageParagraphs.Contains(paragraph))
                        continue;

                    targetModified = true;
                    if (propertyPath != null && propertyPath.StartsWith(k_ParagraphMaskingSettingsRelativeProperty))
                    {
                        maskingChanged = true;
                        break;
                    }
                }
            }

            if (maskingChanged)
            {
                Target.RaiseMaskingSettingsChanged();
            }
            else if (targetModified)
            {
                Target.RaiseNonMaskingSettingsChanged();
            }
            return modifications;
        }

        private void InitializeSerializedProperties()
        {
            m_PageTitle = serializedObject.FindProperty(k_PageTitleProperty);
            k_EventPropertyPaths.ToList().ForEach(CreateEventProperty);
        }

        private void CreateEventProperty(string propertyPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Debug.Assert(property != null, $"Property path {propertyPath} not valid.");

            string tooltip = GetSerializedPropertyTooltip<TutorialPage>(property);
            EventPropertyData eventData = new()
            {
                Property = property,
                Content = new GUIContent(Tr(property.displayName), Tr(tooltip))
            };
            m_Events.Add(eventData);
        }

        private static string GetSerializedPropertyTooltip<Type>(SerializedProperty serializedProperty)
        {
            const BindingFlags bindedTypes = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo field = typeof(Type).GetField(serializedProperty.name, bindedTypes);
            TooltipAttribute[] attributes = field.GetCustomAttributes(typeof(TooltipAttribute), inherit: true) as TooltipAttribute[];
            return attributes.Length > 0 ? attributes[0].tooltip : string.Empty;
        }

         public override VisualElement CreateInspectorGUI()
         {
             VisualElement root = new();
             root.styleSheets.Add(m_Stylesheet);

             TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(root);

             if (!string.IsNullOrEmpty(m_WarningMessage))
             {
                 HelpBox helpBox = new(m_WarningMessage, HelpBoxMessageType.Warning);
                 root.Add(helpBox);
             }

             // Title
             PropertyField titleField = new(m_PageTitle, Tr(LocalizationKeys.k_TutorialPageLabelTitle));
             titleField.TrackPropertyValue(m_PageTitle, OnTitleChanged);

             root.Add(titleField);

             // Paragraphs
             SerializedProperty paragraphs = serializedObject.FindProperty(k_ParagraphPropertyPath);
             ParagraphListView listView = new((TutorialPage)target);
             listView.BindProperty(paragraphs);
             root.Add(listView);

             // All other properties
             root.Add(new PropertyField(serializedObject.FindProperty("m_CameraSettings")));

             // Custom Callbacks
             Foldout callbacksFoldout = new()
             {
                 text = Tr(LocalizationKeys.k_TutorialPageCustomCallbacks),
                 tooltip = Tr(LocalizationKeys.k_TutorialPageCustomCallbacksTooltip),
                 viewDataKey = "PageCallbacksFoldout",
                 value = false
             };
             callbacksFoldout.AddToClassList("callbacks-foldout");
             callbacksFoldout.AddToClassList("foldout-bold-title");

             HelpBox warningHelpBox = TutorialEditorUtils.RenderEventStateWarningElement(callbacksFoldout.contentContainer);
             warningHelpBox.style.display = m_Events.Any(e => TutorialEditorUtils.EventIsNotInState(e.Property, UnityEventCallState.EditorAndRuntime)) ?
                 DisplayStyle.Flex : DisplayStyle.None;

             m_Events.ForEach(data => callbacksFoldout.contentContainer.Add(CreateEventProperty(data.Content, data.Property)));

             root.Add(callbacksFoldout);

             // FAQs
             PropertyField faqEntriesField = new (serializedObject.FindProperty("m_FaqEntries"))
             {
                 label = "FAQ Entries", viewDataKey = "TutorialPageFaqEntriesFoldout"
             };
             faqEntriesField.AddToClassList("inspector-list");
             faqEntriesField.AddToClassList("foldout-bold-title");
             root.Add(faqEntriesField);

             return root;

             void OnTitleChanged(SerializedProperty obj) => RenamePage(Target);
         }

        private static VisualElement CreateEventProperty(GUIContent headerContent, SerializedProperty property)
        {
            VisualElement root = new();
            root.Add(new PropertyField(property, headerContent.text));

            return root;
        }

        private void InitializeEventWithDefaultData(SerializedProperty eventProperty)
        {
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/IET/TutorialCallbacks.asset"); // TODO check this
            //[TODO] Add listeners here if they are empty (?)
            ForceCallbacksListenerTarget(eventProperty, so);
            ForceCallbacksListenerState(eventProperty, UnityEventCallState.EditorAndRuntime);
        }

        /// <summary>
        /// Forces all callbacks of a UnityEvent (or derived class) to use a specific state
        /// </summary>
        /// <param name="eventProperty">A UnityEvent (or derived class) property</param>
        /// <param name="state"></param>
        private void ForceCallbacksListenerState(SerializedProperty eventProperty, UnityEventCallState state)
        {
            SerializedProperty persistentCalls = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            for (int i = 0; i < persistentCalls.arraySize; i++)
            {
                persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_CallState").intValue = (int)state;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ForceCallbacksListenerTarget(SerializedProperty eventProperty, Object listenerTarget)
        {
            SerializedProperty persistentCalls = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            for (int i = 0; i < persistentCalls.arraySize; i++)
            {
                persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target").objectReferenceValue = listenerTarget;
                serializedObject.ApplyModifiedProperties();
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (CallbackAssetPath.IsNullOrEmpty())
                return;

            string destFileName = CallbackAssetPath;
            CallbackAssetPath = string.Empty;

            const string errorMsg1 = "Could not create TutorialCallbacks instance automatically";
            const string errorMsg2 = "Create the instance using Assets > Create > Tutorials > TutorialCallbacks Instance";

            // TODO If the user creates the asset/script to a folder with asmdef this doesn't work.
            const string className = "TutorialCallbacks";
            Type type = Assembly.Load("Assembly-CSharp").GetType(className);
            if (type == null)
            {
                Debug.LogError($"{errorMsg1}: {className} class not found from Assembly-CSharp. {errorMsg2}.");
                return;
            }

            const string methodName = "CreateAndShowAsset";
            MethodInfo method = type.GetMethod(methodName);
            if (method == null)
            {
                Debug.LogError($"{errorMsg1}: {methodName} not found from {className} class. {errorMsg2}.");
                return;
            }

            method.Invoke(null, new object[] { destFileName });
        }

        internal static void RenamePage(TutorialPage page)
        {
            string pageTitle = page.Title.Value;
            string pageName = $"{page.IndexInTutorial}";

            if (!string.IsNullOrEmpty(pageTitle))
                pageName = $"{pageName}_{EditorWindowUtils.MakeValidFileName(pageTitle)}";

            if (page.name != pageName)
            {
                Undo.RecordObject(page, "Rename Tutorial Page");
                page.name = pageName;
            }

            AssetDatabase.SaveAssets();
        }
    }
}
