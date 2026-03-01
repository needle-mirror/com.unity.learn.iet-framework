using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomEditor(typeof(TutorialWelcomePage))]
    internal class TutorialWelcomePageEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private readonly string[] k_PropsToIgnore = { "m_Script" };
        private TutorialWelcomePage Target => (TutorialWelcomePage)target;
        private SerializedProperty m_Buttons;
        private SerializedProperty m_CurrentEvent;
        private const string k_Buttons = "m_Buttons";
        private const string k_OnClickEventPropertyPath = "OnClick";

        private void OnEnable()
        {
            InitializeSerializedProperties();
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            Target.RaiseModified();
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModified();
            return modifications;
        }

        private void InitializeSerializedProperties()
        {
            m_Buttons = serializedObject.FindProperty(k_Buttons);
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.styleSheets.Add(m_Stylesheet);

            // TODO : Update if related setting changes while the inspector is open.
            TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(root);

            Button showDialogButton = new(() => { TutorialModalWindow.Show(Target); })
            {
                text = Localization.Tr(LocalizationKeys.k_TutorialWelcomePageButtonShowDialog)
            };
            showDialogButton.AddToClassList("button-md");
            root.Add(showDialogButton);

            UIUtils.DrawInspectorExcluding(root, serializedObject, this, k_PropsToIgnore);

            // Styles and modifications

            PropertyField buttonsPropField = root.Q<PropertyField>("PropertyField:m_Buttons");
            buttonsPropField.AddToClassList("foldout-bold-title");

            HelpBox helpBox = TutorialEditorUtils.RenderEventStateWarningElement(root);
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt => RunCheckForEvent(helpBox));
            RunCheckForEvent(helpBox);

            return root;
        }

        /// <summary>
        /// Checks if any of the events associated with the Welcome Page's buttons are in call state EditorAndRuntime,
        /// and if so shows/hides the associated HelpBox to warn the user about it.
        /// </summary>
        private void RunCheckForEvent(HelpBox helpBox)
        {
            bool eventOffOrRuntimeOnlyExists = false;
            for (int i = 0; i < m_Buttons.arraySize; i++)
            {
                m_CurrentEvent = m_Buttons.GetArrayElementAtIndex(i).FindPropertyRelative(k_OnClickEventPropertyPath);
                if (!TutorialEditorUtils.EventIsNotInState(m_CurrentEvent, UnityEventCallState.EditorAndRuntime))
                    continue;

                eventOffOrRuntimeOnlyExists = true;
                break;
            }

            helpBox.style.display = eventOffOrRuntimeOnlyExists ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
