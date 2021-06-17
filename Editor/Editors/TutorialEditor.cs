using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    using static Localization;

    [CustomEditor(typeof(Tutorial))]
    class TutorialEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static GUIContent autoCompletion = new GUIContent(Tr("Auto Completion"));
            public static GUIContent startAutoCompletion = new GUIContent(Tr("Start Auto Completion"));
            public static GUIContent stopAutoCompletion = new GUIContent(Tr("Stop Auto Completion"));
        }

        private readonly string[] k_PropsToIgnore = { "m_Script", "m_LessonId" };
        private const string k_PagesPropertyPath = "m_Pages.m_Items";

        private static readonly Regex s_MatchPagesPropertyPath =
            new Regex(
                string.Format("^({0}\\.Array\\.size)|(^({0}\\.Array\\.data\\[\\d+\\]))", Regex.Escape(k_PagesPropertyPath))
            );

        Tutorial tutorial { get { return (Tutorial)target; } }

        [NonSerialized]
        private string m_WarningMessage;

        protected virtual void OnEnable()
        {
            if (serializedObject.FindProperty(k_PagesPropertyPath) == null)
            {
                m_WarningMessage = string.Format(
                    Tr("Unable to locate property path {0} on this object. Automatic masking updates will not work."),
                    k_PagesPropertyPath
                );
            }

            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        protected virtual void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (tutorial != null)
            {
                tutorial.RaiseModified();
            }
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (tutorial == null)
                return modifications;

            tutorial.RaiseModified();

            var pagesChanged = false;

            foreach (var modification in modifications)
            {
                if (modification.currentValue.target != target)
                    continue;

                var propertyPath = modification.currentValue.propertyPath;
                if (s_MatchPagesPropertyPath.IsMatch(propertyPath))
                {
                    pagesChanged = true;
                    break;
                }
            }

            if (pagesChanged)
                tutorial.RaiseModified();

            return modifications;
        }

        public override void OnInspectorGUI()
        {
            if (!string.IsNullOrEmpty(m_WarningMessage))
                EditorGUILayout.HelpBox(m_WarningMessage, MessageType.Warning);

            if (SerializedTypeDrawer.UseDefaultEditors)
            {
                base.OnInspectorGUI();
            }
            else
            {
                DrawPropertiesExcluding(serializedObject, k_PropsToIgnore);
                serializedObject.ApplyModifiedProperties();
            }

            // Auto completion
            GUILayout.Label(Contents.autoCompletion, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(tutorial.IsCompleted))
            {
                if (GUILayout.Button(tutorial.IsAutoCompleting ? Contents.stopAutoCompletion : Contents.startAutoCompletion))
                {
                    if (tutorial.IsAutoCompleting)
                        tutorial.StopAutoCompletion();
                    else
                        tutorial.StartAutoCompletion();
                }
            }
        }
    }
}
