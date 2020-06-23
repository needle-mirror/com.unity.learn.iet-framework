using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [CustomEditor(typeof(TutorialWelcomePage))]
    public class TutorialWelcomePageEditor : Editor
    {
        static readonly bool k_IsAuthoringMode = ProjectMode.IsAuthoringMode();
        readonly string[] k_PropsToIgnore = { "m_Script" };
        TutorialWelcomePage Target => (TutorialWelcomePage)target;

        void OnEnable()
        {
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
            Target.RaiseModifiedEvent();
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModifiedEvent();
            return modifications;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button(Localization.Tr("Show Welcome Dialog")))
                TutorialModalWindow.TryToShow(Target, null);

            if (k_IsAuthoringMode)
            {
                GUILayout.Space(10);
                //base.OnInspectorGUI();
                DrawPropertiesExcluding(serializedObject, k_PropsToIgnore);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
