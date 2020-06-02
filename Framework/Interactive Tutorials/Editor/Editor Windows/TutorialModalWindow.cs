using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.InteractiveTutorials
{
    // A modal/utility window. Utilizes masking for the modality.
    class TutorialModalWindow : EditorWindow
    {
        const int kWidth = 700;
        const int kHeight = 500;
        const int kLeftColumnWidth = 300;

        [SerializeField]
        TutorialStyles m_Styles = null;
        [SerializeField]
        TutorialWelcomePage m_WelcomePage;
        List<TutorialParagraphView> m_Paragraphs = new List<TutorialParagraphView>();
        Action onClose;

        public static bool Visible { get; private set; }

        // Remember to set prior to calling TryToShow().
        public static bool MaskingEnabled { get; set; } = false;

        public static void TryToShow(string windowTitle, TutorialWelcomePage welcomePage, Action onClose)
        {
            if (Visible)
                return;

            var window = GetWindow<TutorialModalWindow>(utility: true, windowTitle);
            window.onClose = onClose;
            var pos = window.position;
            window.minSize = window.maxSize = new Vector2(kWidth, kHeight);
            EditorWindowUtils.CenterOnMainWindow(window);

            window.m_WelcomePage = welcomePage;
            var styles = window.m_Styles;

            foreach (var paragraph in window.m_WelcomePage.paragraphs)
            {
                window.m_Paragraphs.Add(
                    new TutorialParagraphView(paragraph, window, styles.orderedListDelimiter, styles.unorderedListBullet, -1)
                );
            }

            window.Show();

            if (MaskingEnabled)
                window.Mask();
        }

        void OnEnable()
        {
            Visible = true;
            var root = this.rootVisualElement;
            //IMGUIContainer iMGUIContainer = new IMGUIContainer(OnGUIOld);
            //root.Add(iMGUIContainer);
            VisualTreeAsset welcomeDialogAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.learn.iet-framework/Framework/UIElementsViews/WelcomeDialog.uxml");
            StyleSheet rootstyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.learn.iet-framework/Framework/UIElementsViews/WelcomeDialog.uss");
            VisualElement rootContainer = welcomeDialogAsset.CloneTree().Q("MainContainer");
            rootContainer.styleSheets.Add(rootstyle);
            Button GetStartedButton = rootContainer.Q<Button>("GetStartedButton");
            m_WelcomePage = UserStartupCode.FindWelcomePage();

            GetStartedButton.clicked += Close;
            GetStartedButton.text = m_WelcomePage.m_StartButtonLabel;

            ((Label)rootContainer.Q("TextContainer").ElementAt(1)).text = m_WelcomePage.title;
            int currentElement = 2;
            foreach (TutorialParagraph paragraph in m_WelcomePage.paragraphs)
            {
                ((Label)rootContainer.Q("TextContainer").ElementAt(currentElement)).text = paragraph.InstructionTitle;
                currentElement++;
            }


            if (m_WelcomePage != null && m_WelcomePage.icon != null)
                rootContainer.Q("LeftColumn").style.backgroundImage = Background.FromTexture2D((Texture2D)m_WelcomePage.icon);

            root.Add(rootContainer);
            //Mask();
        }

        void OnDestroy()
        {
            Visible = false;
            onClose?.Invoke();
            Unmask();
        }

        void Update()
        {
            // Force repaint so that changes to WelcomePage can be previed immediately.
            if (ProjectMode.IsAuthoringMode())
                Repaint();
        }

        void OnGUIOld()
        {
            if (m_Styles == null)
            {
                TutorialStyles.DisplayErrorMessage("TutorialModalWindow.cs");
                return;
            }

            if (m_WelcomePage == null)
            {
                return;
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                return;
            }

            if (ProjectMode.IsAuthoringMode())
            {
                DrawToolbar();
            }

            GUISkin oldSkin = GUI.skin;
            GUI.skin = m_Styles.skin;

            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(kWidth), GUILayout.Height(kHeight)))
            {
                // left column, "image column"
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(kLeftColumnWidth), GUILayout.Height(kHeight)))
                {
                    GUILayout.Label(GUIContent.none);
                }
                if (m_WelcomePage.icon != null)
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), m_WelcomePage.icon, ScaleMode.StretchToFill);
                }

                // right column
                using (new EditorGUILayout.HorizontalScope(AllTutorialStyles.background))
                {
                    GUILayout.Space(8f);

                    using (new EditorGUILayout.VerticalScope(AllTutorialStyles.background, GUILayout.Height(kHeight)))
                    {
                        const bool pageCompleted = false;
                        var previousTaskState = true;
                        foreach (var paragraph in m_Paragraphs)
                        {
                            if (paragraph.paragraph.type == ParagraphType.Instruction)
                                GUILayout.Space(2f);

                            paragraph.Draw(ref previousTaskState, pageCompleted);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(m_WelcomePage.startButtonLabel, AllTutorialStyles.welcomeDialogButton))
                            {
                                Close();
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }

            GUI.skin = oldSkin;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();

            MaskingEnabled = GUILayout.Toggle(
                MaskingEnabled, "Masking", EditorStyles.toolbarButton,
                GUILayout.MaxWidth(TutorialWindow.s_AuthoringModeToolbarButtonWidth)
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (MaskingEnabled) Mask();
                else Unmask();
                GUIUtility.ExitGUI();
                return;
            }

            EditorGUILayout.EndHorizontal();
        }

        void Mask()
        {
            var styles = m_Styles;
            var maskingColor = styles?.maskingColor ?? Color.magenta * new Color(1f, 1f, 1f, 0.8f);
            var highlightColor = styles?.highlightColor ?? Color.cyan * new Color(1f, 1f, 1f, 0.8f);
            var blockedInteractionColor = styles?.blockedInteractionColor ?? new Color(1, 1, 1, 0.5f);
            var highlightThickness = styles?.highlightThickness ?? 3f;

            var unmaskedViews = new UnmaskedView.MaskData();
            unmaskedViews.AddParentFullyUnmasked(this);
            var highlightedViews = new UnmaskedView.MaskData();

            MaskingManager.Mask(
                unmaskedViews,
                maskingColor,
                highlightedViews,
                highlightColor,
                blockedInteractionColor,
                highlightThickness
            );

            MaskingEnabled = true;
        }

        void Unmask()
        {
            MaskingManager.Unmask();
            MaskingEnabled = false;
        }
    }
}
