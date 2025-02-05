using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
{
    [CustomEditor(typeof(TutorialWelcomePage))]
    class TutorialWelcomePageEditor : UnityEditor.Editor
    {
        readonly string[] k_PropsToIgnore = { "m_Script" };
        TutorialWelcomePage Target => (TutorialWelcomePage)target;
        SerializedProperty m_Buttons;
        SerializedProperty m_CurrentEvent;
        const string k_Buttons = "m_Buttons";
        const string k_OnClickEventPropertyPath = "OnClick";

        void OnEnable()
        {
            InitializeSerializedProperties();
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            Target.RaiseModified();
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModified();
            return modifications;
        }

        void InitializeSerializedProperties()
        {
            m_Buttons = serializedObject.FindProperty(k_Buttons);
        }

        public override void OnInspectorGUI()
        {
            TutorialProjectSettings.DrawDefaultAssetRestoreWarning();

            if (GUILayout.Button(Localization.Tr(LocalizationKeys.k_TutorialWelcomePageButtonShowDialog)))
            {
                TutorialModalWindow.Show(Target);
            }

            GUILayout.Space(10);

            if (SerializedTypeDrawer.UseDefaultEditors)
            {
                base.OnInspectorGUI();
            }
            else
            {
                serializedObject.Update();

                DrawPropertiesExcluding(serializedObject, k_PropsToIgnore);

                bool eventOffOrRuntimeOnlyExists = false;
                for (int i = 0; i < m_Buttons.arraySize; i++)
                {
                    m_CurrentEvent = m_Buttons.GetArrayElementAtIndex(i).FindPropertyRelative(k_OnClickEventPropertyPath);
                    if (!TutorialEditorUtils.EventIsNotInState(m_CurrentEvent, UnityEngine.Events.UnityEventCallState.EditorAndRuntime))
                        continue;

                    eventOffOrRuntimeOnlyExists = true;
                    break;
                }
                if (eventOffOrRuntimeOnlyExists)
                {
                    TutorialEditorUtils.RenderEventStateWarning();
                }
                serializedObject.ApplyModifiedProperties();
            }
        }

        //TODO : this has been disabled as it's too big a change for 4.1, but left here to be activated in 5.0
        // public override VisualElement CreateInspectorGUI()
        // {
        //     VisualElement rootElement = new VisualElement();
        //
        //     //TODO : this won't be updated if setting change midway through this inspector being opened. Should be very
        //     //rare for users to change that there, but look into fixing that
        //     TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(rootElement);
        //
        //     var showDialogButton = new Button(() => { TutorialModalWindow.Show(Target); });
        //     showDialogButton.text = Localization.Tr(LocalizationKeys.k_TutorialWelcomePageButtonShowDialog);
        //     showDialogButton.style.marginBottom = 10;
        //     rootElement.Add(showDialogButton);
        //
        //     if (SerializedTypeDrawer.UseDefaultEditors)
        //     {
        //         InspectorElement.FillDefaultInspector(rootElement, serializedObject, this);
        //     }
        //     else
        //     {
        //         UIElementsUtils.DrawPropertiesExcluding(rootElement, serializedObject, k_PropsToIgnore);
        //
        //         //add the helpbox, and its visibility will be controlled by the RunCheckForEvent function
        //         var helpBox = TutorialEditorUtils.RenderEventStateWarningElement(rootElement);
        //
        //         //if anything under send a serializedproperty change event, we need to check the events again
        //         rootElement.RegisterCallback<SerializedPropertyChangeEvent>(evt => { RunCheckForEvent(helpBox); });
        //         RunCheckForEvent(helpBox);
        //     }
        //
        //
        //     return rootElement;
        // }

        void RunCheckForEvent(HelpBox helpBox)
        {
            bool eventOffOrRuntimeOnlyExists = false;
            for (int i = 0; i < m_Buttons.arraySize; i++)
            {
                m_CurrentEvent = m_Buttons.GetArrayElementAtIndex(i).FindPropertyRelative(k_OnClickEventPropertyPath);
                if (!TutorialEditorUtils.EventIsNotInState(m_CurrentEvent, UnityEngine.Events.UnityEventCallState.EditorAndRuntime))
                    continue;

                eventOffOrRuntimeOnlyExists = true;
                break;
            }

            helpBox.style.display = eventOffOrRuntimeOnlyExists ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
