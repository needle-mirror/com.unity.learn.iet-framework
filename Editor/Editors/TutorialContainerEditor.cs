using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    [CustomEditor(typeof(TutorialContainer))]
    class TutorialContainerEditor : UnityEditor.Editor
    {
        static readonly bool k_IsAuthoringMode = ProjectMode.IsAuthoringMode();
        readonly string[] k_PropertiesToHide =
        {
            "m_Script",
             nameof(TutorialContainer.Modified) // this is not not something tutorial authors should subscribe to typically
        };

        TutorialContainer Target => (TutorialContainer)target;

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
            Target.RaiseModified();
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModified();
            return modifications;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(Localization.Tr(MenuItems.ShowTutorials)))
            {
                // Make sure we will display 'this' container in the window.
                var window = Target.ProjectLayout != null
                    ? TutorialWindow.CreateWindowAndLoadLayout(Target)
                    : TutorialWindow.CreateNextToInspector();

                window.ActiveContainer = Target;
            }

            if (k_IsAuthoringMode)
            {
                EditorGUILayout.Space(10);
                DrawPropertiesExcluding(serializedObject, k_PropertiesToHide);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
