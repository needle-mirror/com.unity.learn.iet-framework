using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    using static Localization;

    [CustomEditor(typeof(Tutorial))]
    internal class TutorialEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private static class Contents
        {
            public static GUIContent s_AutoCompletion = new(Tr(LocalizationKeys.k_TutorialLabelAutoCompletion));
            public static GUIContent s_StartAutoCompletion = new(Tr(LocalizationKeys.k_TutorialButtonStartAutoCompletion));
            public static GUIContent s_StopAutoCompletion = new(Tr(LocalizationKeys.k_TutorialButtonStopAutoCompletion));
        }

        private static readonly string[] k_PropsToIgnore = { "m_Script", nameof(Tutorial.m_LessonId) };

        private static readonly string s_PagesPropertyPath = $"{nameof(Tutorial.m_Pages)}.m_Items";
        private static readonly string s_ScenesProperty = "m_Scenes";
        private static readonly string s_SceneManagementBehaviorProperty = "m_SceneManagementBehavior";
        private static readonly string s_ReturnToPreviousProperty = "m_ReturnToPreviousScenes";
        private static readonly string s_FaqEntriesProperty = "m_FaqEntries";

        private static readonly Regex s_MatchPagesPropertyPath =
            new(
                string.Format("^({0}\\.Array\\.size)|(^({0}\\.Array\\.data\\[\\d+\\]))", Regex.Escape(s_PagesPropertyPath))
            );

        private Tutorial Target => (Tutorial)target;

        [NonSerialized] private string m_WarningMessage;

        private SerializedProperty _mSceneManagementBehaviourProp;
        private PropertyField _scenesField;
        private PropertyField _returnToPreviousField;

        protected void OnEnable()
        {
            if (serializedObject.FindProperty(s_PagesPropertyPath) == null)
            {
                m_WarningMessage = string.Format(Tr(LocalizationKeys.k_MissingPropertyPathWarning), s_PagesPropertyPath);
            }

            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        protected void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (Target != null)
            {
                serializedObject.Update();
                Target.RaiseModified();
                string path = AssetDatabase.GetAssetPath(Target);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.ImportAsset(path);
                }
            }
            EditorApplication.RepaintProjectWindow();
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModified();

            bool pagesChanged = false;

            foreach (UndoPropertyModification modification in modifications)
            {
                if (modification.currentValue.target != target)
                {
                    continue;
                }

                string propertyPath = modification.currentValue.propertyPath;
                if (s_MatchPagesPropertyPath.IsMatch(propertyPath))
                {
                    pagesChanged = true;
                    break;
                }
            }

            if (pagesChanged)
            {
                Target.RaiseModified();
            }

            return modifications;
        }

        public override VisualElement CreateInspectorGUI()
        {
            _mSceneManagementBehaviourProp = serializedObject.FindProperty(s_SceneManagementBehaviorProperty);

            VisualElement root = new();
            root.styleSheets.Add(m_Stylesheet);

            TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(root);
            if (!string.IsNullOrEmpty(m_WarningMessage))
            {
                HelpBox helpBox = new(m_WarningMessage, HelpBoxMessageType.Warning);
                root.Add(helpBox);
            }

            UIUtils.DrawInspectorExcluding(root, serializedObject, this, k_PropsToIgnore);

            // Hack into the creation of a TutorialPage to make it a sub-asset
            root.Q<PropertyField>("PropertyField:m_Pages").RegisterCallbackOnce<GeometryChangedEvent>(OnPropReady);

            // Scene Management behaviour
            PropertyField sceneManagementBehaviour_Field = root.Q<PropertyField>($"PropertyField:{s_SceneManagementBehaviorProperty}");
            sceneManagementBehaviour_Field.label = "Behaviour";
            sceneManagementBehaviour_Field.RegisterValueChangeCallback(OnSceneManagementBehaviourChanged);

            _scenesField = root.Q<PropertyField>($"PropertyField:{s_ScenesProperty}");
            _scenesField.AddToClassList("indented-property");

            _returnToPreviousField = root.Q<PropertyField>($"PropertyField:{s_ReturnToPreviousProperty}");
            _returnToPreviousField.AddToClassList("indented-property");

            // Gather all event properties in a foldout
            Foldout callbacksFoldout = new()
            {
                text = "Custom Callbacks",
                viewDataKey = "TutorialCallbacksFoldout",
                value = false
            };
            callbacksFoldout.AddToClassList("callbacks-foldout");
            callbacksFoldout.AddToClassList("foldout-bold-title");

            callbacksFoldout.contentContainer.Add(root.Q<PropertyField>($"PropertyField:{nameof(Tutorial.Initiated)}"));
            callbacksFoldout.contentContainer.Add(root.Q<PropertyField>($"PropertyField:{nameof(Tutorial.PageInitiated)}"));
            callbacksFoldout.contentContainer.Add(root.Q<PropertyField>($"PropertyField:{nameof(Tutorial.GoingBack)}"));
            callbacksFoldout.contentContainer.Add(root.Q<PropertyField>($"PropertyField:{nameof(Tutorial.Completed)}"));
            callbacksFoldout.contentContainer.Add(root.Q<PropertyField>($"PropertyField:{nameof(Tutorial.Quit)}"));

            // FAQs
            PropertyField faqEntriesField = root.Q<PropertyField>($"PropertyField:{s_FaqEntriesProperty}");
            faqEntriesField.label = "FAQ Entries";
            faqEntriesField.viewDataKey = "TutorialFaqEntriesFoldout";
            faqEntriesField.AddToClassList("inspector-list");
            faqEntriesField.AddToClassList("foldout-bold-title");

            root.Add(callbacksFoldout);
            callbacksFoldout.PlaceBehind(faqEntriesField);

#if TUTORIAL_AUTHORING
            // Auto Completion button
            Button autoCompleteButton = new()
            {
                text = Target.IsAutoCompleting ? Contents.s_StopAutoCompletion.text : Contents.s_StartAutoCompletion.text
            };
            autoCompleteButton.AddToClassList("button-md");
            autoCompleteButton.clicked += () =>
            {
                if (Target.IsAutoCompleting) Target.StopAutoCompletion();
                else Target.StartAutoCompletion();
            };
            autoCompleteButton.SetEnabled(!Target.IsCompleted);
            root.Add(autoCompleteButton);
#endif

            return root;

            void OnPropReady(GeometryChangedEvent evt)
            {
                ListView pagesList = (ListView)((VisualElement)evt.currentTarget)[0];

                if (pagesList.itemsSource.Count == 0) pagesList.allowRemove = false;

                pagesList.onAdd = OnAddPageClicked;
                pagesList.onRemove = OnRemovePageClicked;
                pagesList.itemIndexChanged += (_, _) => RenameItems();
            }

            void OnAddPageClicked(BaseListView list)
            {
                int undoGroup = Undo.GetCurrentGroup();

                TutorialPage newPage = CreateNewPageAsSubAsset(Target);
                AssetDatabase.SaveAssets();

                serializedObject.Update();
                SerializedProperty pagesProperty = serializedObject.FindProperty(s_PagesPropertyPath);
                pagesProperty.arraySize++;
                int i = pagesProperty.arraySize - 1;
                pagesProperty.GetArrayElementAtIndex(i).objectReferenceValue =  newPage;
                serializedObject.ApplyModifiedProperties();

                list.allowRemove = true;
                RenameItems();

                Undo.SetCurrentGroupName("Create Tutorial Page");
                Undo.CollapseUndoOperations(undoGroup);
            }

            void OnRemovePageClicked(BaseListView list)
            {
                serializedObject.Update();
                SerializedProperty pagesProperty = serializedObject.FindProperty(s_PagesPropertyPath);

                int selectedIndex = list.selectedIndex != -1 ? list.selectedIndex : pagesProperty.arraySize - 1;
                if (selectedIndex < 0 || selectedIndex >= pagesProperty.arraySize) return;

                TutorialPage pageToDelete = (TutorialPage)pagesProperty.GetArrayElementAtIndex(selectedIndex).objectReferenceValue;

                int undoGroup = Undo.GetCurrentGroup();

                if (pageToDelete != null)
                    pagesProperty.GetArrayElementAtIndex(selectedIndex).objectReferenceValue = null;
                pagesProperty.DeleteArrayElementAtIndex(selectedIndex);
                serializedObject.ApplyModifiedProperties();

                if (pageToDelete != null) Undo.DestroyObjectImmediate(pageToDelete);
                AssetDatabase.SaveAssets();

                list.selectedIndex = -1;

                if (pagesProperty.arraySize == 0) list.allowRemove = false;
                if (pagesProperty.arraySize != 0) RenameItems();

                Undo.SetCurrentGroupName("Remove Tutorial Page");
                Undo.CollapseUndoOperations(undoGroup);
            }

            void RenameItems()
            {
                SerializedProperty pagesProperty = serializedObject.FindProperty(s_PagesPropertyPath);
                for (int i = 0; i < pagesProperty.arraySize; i++)
                {
                    TutorialPage page = (TutorialPage)pagesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (page == null) continue;
                    if (page.IndexInTutorial != i)
                    {
                        Undo.RecordObject(page, "Reindex Tutorial Pages");
                        page.IndexInTutorial = i;
                    }
                    TutorialPageEditor.RenamePage(page);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void OnSceneManagementBehaviourChanged(SerializedPropertyChangeEvent evt) => UpdateSceneManagementFieldsVisibility();

        private void UpdateSceneManagementFieldsVisibility()
        {
            Tutorial.SceneManagementBehaviorType newBehaviour = (Tutorial.SceneManagementBehaviorType)_mSceneManagementBehaviourProp.enumValueIndex;
            switch (newBehaviour)
            {
                case Tutorial.SceneManagementBehaviorType.CreateNewScene:
                    UIUtils.Hide(_scenesField);
                    UIUtils.Show(_returnToPreviousField);
                    break;
                case Tutorial.SceneManagementBehaviorType.UseActiveScene:
                    UIUtils.Hide(_scenesField);
                    UIUtils.Hide(_returnToPreviousField);
                    break;
                case Tutorial.SceneManagementBehaviorType.LoadScenes:
                    UIUtils.Show(_scenesField);
                    UIUtils.Show(_returnToPreviousField);
                    break;
            }
        }

        internal static TutorialPage CreateNewPageAsSubAsset(Tutorial tutorial)
        {
            int newPageIndex = tutorial.PagesCollection.Count;
            TutorialPage newPage = CreateInstance<TutorialPage>();
            Undo.RegisterCreatedObjectUndo(newPage, "Create Tutorial Page");
            newPage.IndexInTutorial = newPageIndex;
            newPage.Title = $"Page {newPageIndex}";
            TutorialPageEditor.RenamePage(newPage);
            AssetDatabase.AddObjectToAsset(newPage, tutorial);

            return newPage;
        }
    }
}
