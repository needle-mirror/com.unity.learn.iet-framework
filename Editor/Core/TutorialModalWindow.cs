using System;
using System.Collections;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Tutorials.Editor.Localization;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A modal/utility window that can display TutorialWelcomePage as its content.
    /// Optionally utilizes masking for modality.
    /// </summary>
    public class TutorialModalWindow : EditorWindow
    {
        private static readonly Vector2 k_windowSize = new(700, 500);

        /// <summary>
        /// The current instance of this window
        /// </summary>
        public static TutorialModalWindow Instance { get; set; }

        private static TutorialModalWindow FindInstance() => Resources.FindObjectsOfTypeAll<TutorialModalWindow>().FirstOrDefault();
        internal TutorialStyles Styles => TutorialProjectSettings.Instance.TutorialStyle;

        /// <summary>
        /// Does the window utilize masking for modality effect.
        /// </summary>
        /// <remarks>
        /// Remember to set prior to calling TryToShow().
        /// </remarks>
        public static bool MaskingEnabled { get; set; }

        /// <summary>
        /// In order to set the welcome page, use the Show() function instead.
        /// </summary>
        public TutorialWelcomePage WelcomePage
        {
            get => m_WelcomePage;
            private set
            {
                if (m_WelcomePage)
                {
                    m_WelcomePage.Modified.RemoveListener(OnWelcomePageModified);
                }

                m_WelcomePage = value;

                if (m_WelcomePage)
                {
                    m_WelcomePage.Modified.AddListener(OnWelcomePageModified);
                }
            }
        }
        [SerializeField] private TutorialWelcomePage m_WelcomePage;

        private Action m_OnClose;
        private VisualElement m_Root;
        private StyleSheet m_currentEditorThemeStyleSheet; // Dark/Light theme

        private void OnEnable()
        {
            SetupBackend();
            SetupFrontend();
        }

        private void OnDisable()
        {
            TeardownBackend();
        }

        private void OnDestroy() //aka: "When the user closes the window"
        {
            m_OnClose?.Invoke();
            Unmask();
            Instance = null;
        }

        private void SetupBackend()
        {
            if (!Instance)
            {
                Instance = FindInstance();
            }
            SubscribeEvents();
        }

        private void SetupFrontend()
        {
            m_Root = rootVisualElement;
            minSize = k_windowSize;
            maxSize = k_windowSize;
            RebuildFrontend();
        }

        private void RebuildFrontend()
        {
            if (TutorialWindow.s_IsLoadingLayout) { return; }
            LoadUIStructure();
        }

        internal void LoadUIStructure()
        {
            m_Root.Clear();

            if (TutorialModel.s_AuthoringModeEnabled)
            {
                rootVisualElement.Add(new IMGUIContainer(OnGuiToolbar));
            }

            VisualTreeAsset windowContent = UIUtils.LoadUXML("WelcomeDialog");
            windowContent.CloneTree(m_Root);

            //preserve the base style, remove all styles defined in UXML and apply new skin
            for (int i = m_Root.styleSheets.count - 1; i > 0; i--)
            {
                m_Root.styleSheets.Remove(m_Root.styleSheets[i]);
            }

            UIUtils.LoadCommonStyleSheet(m_Root);
            UpdateWindowSkin();

            EditorCoroutineUtility.StartCoroutine(LoadContent(), this);
        }


        private IEnumerator LoadContent()
        {
            while (m_WelcomePage == null)
            {
                yield return null;
            }
            UpdateContent();
            if (MaskingEnabled)
            {
                Mask();
            }
        }

        private void UpdateWindowSkin()
        {
            UIUtils.RemoveStyleSheet(m_currentEditorThemeStyleSheet, m_Root);
            UIUtils.LoadEditorThemeStyleSheet(out m_currentEditorThemeStyleSheet, m_Root);

            if (TutorialProjectSettings.Instance != null && TutorialProjectSettings.Instance.TutorialStyle != null)
            {
                TutorialProjectSettings.Instance.TutorialStyle.AddCustomStyleSheet(m_Root);
            }
        }

        private void SubscribeEvents()
        {
            if (m_WelcomePage)
            {
                m_WelcomePage.Modified.AddListener(OnWelcomePageModified);
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_WelcomePage)
            {
                m_WelcomePage.Modified.RemoveListener(OnWelcomePageModified);
            }
        }

        private void TeardownBackend()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// Shows the window using the provided content.
        /// </summary>
        /// <remarks>
        /// Shown as a utility window, https://docs.unity3d.com/ScriptReference/EditorWindow.ShowUtility.html
        /// </remarks>
        /// <param name="welcomePage">Content to be shown.</param>
        /// <param name="onClose">Optional callback to be called when the window is closed.</param>
        public static void Show(TutorialWelcomePage welcomePage, Action onClose = null)
        {
            Hide();
            TutorialModalWindow window = CreateInstance<TutorialModalWindow>();
            window.titleContent = new GUIContent(welcomePage.WindowTitle);
            window.minSize = k_windowSize;
            window.maxSize = k_windowSize;
            window.m_OnClose = onClose;
            window.WelcomePage = welcomePage;

            window.ShowUtility();
            EditorWindowUtils.CenterOnMainWindow(window); // NOTE: positioning must be done after Show() in order to work.
        }

        /// <summary>
        /// Closes the window if it's open
        /// </summary>
        public static void Hide()
        {
            Instance?.Close();
        }

        private void UpdateContent()
        {
            if (!WelcomePage)
            {
                Debug.LogError("null WelcomePage.");
                return;
            }

            Instance.titleContent = new GUIContent(WelcomePage.WindowTitle);

            if (!WelcomePage.HeaderContent.IsValid())
            {
                UIUtils.ShowOrHide("HeaderContainer", m_Root, false);
            }
            else
            {

                VisualElement header = m_Root.Q("HeaderMedia");
                VideoPlayerElement videoPlayer = m_Root.Q<VideoPlayerElement>();

                switch (WelcomePage.HeaderContent.ContentType)
                {
                    case MediaContent.MediaContentType.Image:
                        UIUtils.ShowOrHide("HeaderMedia", m_Root, WelcomePage.HeaderContent.IsValid());
                        UIUtils.ShowOrHide("VideoPlayerContainer", m_Root, false);
                        header.style.backgroundImage = Background.FromTexture2D(WelcomePage.HeaderContent.Image);
                        break;
                    case MediaContent.MediaContentType.VideoClip:
                    case MediaContent.MediaContentType.VideoUrl:
                        UIUtils.ShowOrHide("VideoPlayerContainer", m_Root, WelcomePage.HeaderContent.IsValid());
                        UIUtils.ShowOrHide("HeaderMedia", m_Root, false);

                        if (WelcomePage.HeaderContent.ContentType == MediaContent.MediaContentType.VideoClip)
                            videoPlayer.SetVideoClip(WelcomePage.HeaderContent.VideoClip,
                                WelcomePage.HeaderContent.Loop);
                        else
                            videoPlayer.SetVideoUrl(WelcomePage.HeaderContent.Url, WelcomePage.HeaderContent.Loop);

                        videoPlayer.SetLooping(WelcomePage.HeaderContent.Loop);
                        break;
                }
            }

            UIUtils.SetupLabel("Heading", WelcomePage.Title, m_Root, false);

            Label welcomeLabel = new(WelcomePage.Description);
            //ensure we got word wrapping
            welcomeLabel.style.whiteSpace = WhiteSpace.Normal;
            m_Root.Q("Description").Add(welcomeLabel);
            AddDynamicButtonsToContent();

            if (WelcomePage.MaskEditor)
            {
                Mask();
            }
        }

        private void AddDynamicButtonsToContent()
        {
            VisualElement buttonContainer = m_Root.Q("ButtonContainer");
            buttonContainer.Clear();

            foreach (TutorialWelcomePage.ButtonData buttonData in WelcomePage.Buttons.Where(buttonData => buttonData.Text.Value.IsNotNullOrEmpty()))
            {
                Button button = new(() => buttonData.OnClick?.Invoke())
                {
                    text = buttonData.Text,
                    tooltip = buttonData.Tooltip
                };
                buttonContainer.Add(button);
            }
        }

        private void OnGuiToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            MaskingEnabled = GUILayout.Toggle
            (
                MaskingEnabled, TutorialWindow.IconContent("Mask Icon", Tr(LocalizationKeys.k_IconPreviewMaskingTooltip)),
                EditorStyles.toolbarButton, GUILayout.Width(TutorialWindow.k_AuthoringButtonWidth)
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (MaskingEnabled)
                {
                    Mask();
                }
                else
                {
                    Unmask();
                }
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Mask()
        {
            UnmaskedView.MaskData unmaskedViews = new();
            unmaskedViews.AddParentFullyUnmasked(this);
            UnmaskedView.MaskData highlightedViews = new();

            MaskingManager.Mask
            (
                unmaskedViews,
                Styles.MaskingColor,
                highlightedViews,
                Styles.HighlightColor,
                Styles.BlockedInteractionColor,
                Styles.HighlightThickness
            );

            MaskingEnabled = true;
        }

        private void Unmask()
        {
            MaskingManager.Unmask();
            MaskingEnabled = false;
        }

        private void OnWelcomePageModified(TutorialWelcomePage sender)
        {
            UpdateContent();
        }
    }
}
