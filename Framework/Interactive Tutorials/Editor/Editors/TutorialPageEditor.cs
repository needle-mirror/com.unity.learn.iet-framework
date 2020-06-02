using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [CustomEditor(typeof(TutorialPage))]
    class TutorialPageEditor : Editor
    {
        const string k_ParagraphPropertyPath = "m_Paragraphs.m_Items";
        const string k_ParagraphMaskingSettingsRelativeProperty = "m_MaskingSettings";
        const string k_ParagraphVideoRelativeProperty = "m_Video";
        const string k_ParagraphImageRelativeProperty = "m_Image";
        const string k_ParagraphNarrativeTitleProperty = "m_Summary";
        const string k_ParagraphTypeProperty = "m_Type";

        const string k_ParagraphNarrativeDescriptionProperty = "m_Description";
        const string k_ParagraphTitleProperty = "m_InstructionBoxTitle";
        const string k_ParagraphDescriptionProperty = "m_InstructionText";


        const string k_ParagraphCriteriaTypePropertyPath = "m_CriteriaCompletion";
        const string k_ParagraphCriteriaPropertyPath = "m_Criteria";

        const string k_ParagraphNextTutorialPropertyPath = "m_Tutorial";
        const string k_ParagraphNextTutorialButtonTextPropertyPath = "m_TutorialButtonText";


        static readonly Regex s_MatchMaskingSettingsPropertyPath =
            new Regex(
                string.Format(
                    "(^{0}\\.Array\\.size)|(^({0}\\.Array\\.data\\[\\d+\\]\\.{1}\\.))",
                    k_ParagraphPropertyPath, k_ParagraphMaskingSettingsRelativeProperty
                )
            );

        TutorialPage tutorialPage { get { return (TutorialPage)target; } }

        [NonSerialized]
        string m_WarningMessage;

        /// <summary>
        /// Enable to display the old, not simplified, inspector
        /// </summary>
        bool m_ForceOldInspector = false;

        string[] propertiesToIgnoreInBaseClass = new string[] { "m_SectionTitle", "m_Paragraphs", "m_Script" };
        SerializedProperty m_MaskingSettings;
        SerializedProperty m_Type;
        SerializedProperty m_Video;
        SerializedProperty m_Image;

        SerializedProperty m_NarrativeTitle;
        SerializedProperty m_NarrativeDescription;
        SerializedProperty m_InstructionTitle;
        SerializedProperty m_InstructionDescription;

        SerializedProperty m_CriteriaCompletion;
        SerializedProperty m_Criteria;

        SerializedProperty m_TutorialButtonText;
        SerializedProperty m_NextTutorial;

        HeaderMediaType m_HeaderMediaType;

        enum HeaderMediaType
        {
            Image = ParagraphType.Image,
            Video = ParagraphType.Video
        }

        protected virtual void OnEnable()
        {
            InitializeSerializedProperties();

            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        protected virtual void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (tutorialPage == null) { return; }
            tutorialPage.RaiseTutorialPageMaskingSettingsChangedEvent();
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (tutorialPage == null) { return modifications; }

            bool targetModified = false;
            bool maskingChanged = false;

            foreach (var modification in modifications)
            {
                if (modification.currentValue.target != target) { continue; }

                targetModified = true;
                var propertyPath = modification.currentValue.propertyPath;
                if (s_MatchMaskingSettingsPropertyPath.IsMatch(propertyPath))
                {
                    maskingChanged = true;
                    break;
                }
            }

            if (maskingChanged)
            {
                tutorialPage.RaiseTutorialPageMaskingSettingsChangedEvent();
            }
            else if (targetModified)
            {
                tutorialPage.RaiseTutorialPageNonMaskingSettingsChangedEvent();
            }
            return modifications;
        }

        void InitializeSerializedProperties()
        {
            SerializedProperty paragraphs = serializedObject.FindProperty(k_ParagraphPropertyPath);
            if (paragraphs == null)
            {
                m_WarningMessage = string.Format(
                    "Unable to locate property path {0} on this object. Automatic masking updates will not work.",
                    k_ParagraphPropertyPath
                );
            }
            else if (paragraphs.arraySize > 0)
            {
                SerializedProperty firstParagraph = paragraphs.GetArrayElementAtIndex(0);

                m_MaskingSettings = firstParagraph.FindPropertyRelative(k_ParagraphMaskingSettingsRelativeProperty);
                if (m_MaskingSettings == null)
                    m_WarningMessage = string.Format(
                        "Unable to locate property path {0}.Array.data[0].{1} on this object. Automatic masking updates will not work.",
                        k_ParagraphPropertyPath,
                        k_ParagraphMaskingSettingsRelativeProperty
                    );

                m_Type = firstParagraph.FindPropertyRelative(k_ParagraphTypeProperty);
                m_HeaderMediaType = (HeaderMediaType)m_Type.intValue;
                var headerMediaParagraphType = (ParagraphType)m_Type.intValue;
                // Only Image and Video are allowed for the first paragraph which is always the header media in the new fixed tutorial page layout.
                if (headerMediaParagraphType != ParagraphType.Image && headerMediaParagraphType != ParagraphType.Video)
                {
                    m_Type.intValue = (int)ParagraphType.Image;
                }

                m_Video = firstParagraph.FindPropertyRelative(k_ParagraphVideoRelativeProperty);
                m_Image = firstParagraph.FindPropertyRelative(k_ParagraphImageRelativeProperty);

                switch (paragraphs.arraySize)
                {
                    case 2: SetupNarrativeOnlyPage(paragraphs); break;
                    case 4: SetupSwitchTutorialPage(paragraphs); break;
                    case 3:
                    default:
                        SetupNarrativeAndInstructivePage(paragraphs); break;
                }
            }
        }

        void SetupNarrativeParagraph(SerializedProperty paragraphs)
        {
            if (paragraphs.arraySize < 2)
            {
                m_NarrativeTitle = null;
                m_NarrativeDescription = null;
                return;
            }

            SerializedProperty narrativeParagraph = paragraphs.GetArrayElementAtIndex(1);
            m_NarrativeTitle = narrativeParagraph.FindPropertyRelative(k_ParagraphNarrativeTitleProperty);
            m_NarrativeDescription = narrativeParagraph.FindPropertyRelative(k_ParagraphNarrativeDescriptionProperty);
            // TODO refactoring, support the old name of the property for a while still
            if (m_NarrativeDescription == null)
                m_NarrativeDescription = narrativeParagraph.FindPropertyRelative("m_description1");
        }

        void SetupNarrativeOnlyPage(SerializedProperty paragraphs)
        {
            SetupNarrativeParagraph(paragraphs);
        }

        void SetupNarrativeAndInstructivePage(SerializedProperty paragraphs)
        {
            SetupNarrativeParagraph(paragraphs);
            if (paragraphs.arraySize > 2)
            {
                SerializedProperty instructionParagraph = paragraphs.GetArrayElementAtIndex(2);
                m_InstructionTitle = instructionParagraph.FindPropertyRelative(k_ParagraphTitleProperty);
                m_InstructionDescription = instructionParagraph.FindPropertyRelative(k_ParagraphDescriptionProperty);
                m_CriteriaCompletion = instructionParagraph.FindPropertyRelative(k_ParagraphCriteriaTypePropertyPath);
                m_Criteria = instructionParagraph.FindPropertyRelative(k_ParagraphCriteriaPropertyPath);
            }
            else
            {
                m_InstructionTitle = null;
                m_InstructionDescription = null;
                m_CriteriaCompletion = null;
                m_Criteria = null;
            }
        }

        void SetupSwitchTutorialPage(SerializedProperty paragraphs)
        {
            SetupNarrativeAndInstructivePage(paragraphs);
            if (paragraphs.arraySize > 3)
            {
                SerializedProperty tutorialSwitchParagraph = paragraphs.GetArrayElementAtIndex(3);
                m_NextTutorial = tutorialSwitchParagraph.FindPropertyRelative(k_ParagraphNextTutorialPropertyPath);
                m_TutorialButtonText = tutorialSwitchParagraph.FindPropertyRelative(k_ParagraphNextTutorialButtonTextPropertyPath);
            }
            else
            {
                m_NextTutorial = null;
                m_TutorialButtonText = null;
            }
        }

        public override void OnInspectorGUI()
        {
            if (!string.IsNullOrEmpty(m_WarningMessage))
            {
                EditorGUILayout.HelpBox(m_WarningMessage, MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();

            m_ForceOldInspector = EditorGUILayout.Toggle("Force default inspector", m_ForceOldInspector);
            EditorGUILayout.Space(10);
            if (m_ForceOldInspector)
            {
                base.OnInspectorGUI();
            }
            else
            {
                DrawSimplifiedInspector();
            }

            if (EditorGUI.EndChangeCheck())
            {
                TutorialWindow.GetWindow().ForceInititalizeTutorialAndPage();
            }
        }

        void DrawSimplifiedInspector()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("Header Media Type");
            m_HeaderMediaType = (HeaderMediaType)EditorGUILayout.EnumPopup(GUIContent.none, m_HeaderMediaType);
            m_Type.intValue = (int)m_HeaderMediaType;

            EditorGUILayout.Space(10);

            RenderProperty("Media", m_HeaderMediaType == HeaderMediaType.Image ? m_Image : m_Video);

            EditorGUILayout.Space(10);

            RenderProperty("Narrative Title", m_NarrativeTitle);

            EditorGUILayout.Space(10);

            RenderProperty("Narrative Description", m_NarrativeDescription);

            EditorGUILayout.Space(10);

            RenderProperty("Instruction Title", m_InstructionTitle);

            EditorGUILayout.Space(10);

            RenderProperty("Instruction Description", m_InstructionDescription);

            if (m_CriteriaCompletion != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Completion Criteria");
                EditorGUILayout.PropertyField(m_CriteriaCompletion, GUIContent.none);
                EditorGUILayout.PropertyField(m_Criteria, GUIContent.none);
            }

            if (m_NextTutorial != null)
            {
                EditorGUILayout.Space(10);
                RenderProperty("Next Tutorial", m_NextTutorial);
                RenderProperty("Next Tutorial button text", m_TutorialButtonText);
            }

            RenderProperty("Enable Masking", m_MaskingSettings);

            EditorGUILayout.EndVertical();

            DrawPropertiesExcluding(serializedObject, propertiesToIgnoreInBaseClass);

            serializedObject.ApplyModifiedProperties();
        }

        void RenderProperty(string name, SerializedProperty property)
        {
            if (property == null) { return; }
            EditorGUILayout.LabelField(name);
            EditorGUILayout.PropertyField(property, GUIContent.none);
        }
    }
}
