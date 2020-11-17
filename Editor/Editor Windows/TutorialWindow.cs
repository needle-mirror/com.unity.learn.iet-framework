using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.EditorCoroutines.Editor;

using UnityObject = UnityEngine.Object;
using static Unity.Tutorials.Core.Editor.RichTextParser;

namespace Unity.Tutorials.Core.Editor
{
    using static Localization;

    /// <summary>
    /// The window used to display all tutorial content.
    /// </summary>
    public sealed class TutorialWindow : EditorWindowProxy
    {
        /// <summary>
        /// Should we show the Close Tutorials info dialog for the user for the current project.
        /// By default the dialog is shown once per project and disabled after that.
        /// </summary>
        /// <remarks>
        /// You want to set this typically to false when running unit tests.
        /// </remarks>
        public static ProjectSetting<bool> ShowTutorialsClosedDialog =
            new ProjectSetting<bool>("IET.ShowTutorialsClosedDialog", "Show info dialog when the window is closed", true);

        VisualElement m_VideoBoxElement;

        const int k_MinWidth = 300;
        const int k_MinHeight = 300;
        internal static readonly string k_UIAssetPath = "Packages/com.unity.learn.iet-framework/Editor/UI";

        // Loads an asset from a common UI resource folder.
        internal static T LoadUIAsset<T>(string filename) where T : UnityObject =>
            AssetDatabase.LoadAssetAtPath<T>($"{k_UIAssetPath}/{filename}");

        SystemLanguage m_CurrentEditorLanguage; // uninitialized in order to force translation when the window is enabled for the first time

        static TutorialWindow instance;

        List<TutorialParagraphView> m_Paragraphs = new List<TutorialParagraphView>();
        int[] m_Indexes;
        [SerializeField]
        List<TutorialParagraphView> m_AllParagraphs = new List<TutorialParagraphView>();

        internal static readonly float s_AuthoringModeToolbarButtonWidth = 115;

        static readonly bool s_AuthoringMode = ProjectMode.IsAuthoringMode();

        string m_NextButtonText = "";
        string m_BackButtonText = "";
        string m_WindowTitleContent;
        string m_HomePromptTitle;
        string m_HomePromptText;
        string m_PromptYes;
        string m_PromptNo;
        string m_PromptOk;
        string m_MenuPathGuide;
        string m_TabClosedDialogTitle;
        string m_TabClosedDialogText;

        internal Tutorial currentTutorial;

        /// <summary>
        /// Creates the window if it does not exist, anchoring it as a tab next to the inspector.
        /// </summary>
        internal static TutorialWindow CreateNextToInspector()
        {
            var inspectorWindow =  Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(wnd => wnd.GetType().Name == "InspectorWindow");

            Type windowToAnchorTo = inspectorWindow != null ? inspectorWindow.GetType() : null;
            var tutorialWindow = CreateWindow(windowToAnchorTo);

            if (inspectorWindow)
                inspectorWindow.DockWindow(tutorialWindow, EditorWindowUtils.DockPosition.Right);

            return tutorialWindow;
        }

        /// <summary>
        /// Creates the window if it does not exist, and positions it using a window layout
        /// specified either by the project's TutorialContainer or Tutorial Framework's default layout.
        /// </summary>
        /// <returns></returns>
        internal static TutorialWindow CreateWindowAndLoadLayout()
        {
            instance = CreateWindow();
            var readme = FindReadme();
            if (readme != null)
                readme.LoadTutorialProjectLayout();
            return instance;
        }

        /// <summary>
        /// Creates a window, and positions it as a tab of another window, if wanted.
        /// </summary>
        /// <param name="windowToAnchorTo"></param>
        /// <returns></returns>
        internal static TutorialWindow CreateWindow(Type windowToAnchorTo = null)
        {
            instance = GetWindow<TutorialWindow>(windowToAnchorTo);
            instance.minSize = new Vector2(k_MinWidth, k_MinHeight);
            return instance;
        }

        internal TutorialContainer readme
        {
            get { return m_Readme; }
            set
            {
                if (m_Readme)
                    m_Readme.Modified -= OnTutorialContainerModified;

                var oldReadme = m_Readme;
                m_Readme = value;
                if (m_Readme)
                {
                    if (oldReadme != m_Readme)
                        FetchTutorialStates();

                    m_Readme.Modified += OnTutorialContainerModified;
                }
            }
        }
        [SerializeField] TutorialContainer m_Readme;

        TutorialContainer.Section[] Cards => readme?.Sections ?? new TutorialContainer.Section[0];

        bool CanMoveToNextPage =>
            currentTutorial != null && currentTutorial.CurrentPage != null &&
            (currentTutorial.CurrentPage.AreAllCriteriaSatisfied ||
                currentTutorial.CurrentPage.HasMovedToNextPage);

        bool MaskingEnabled
        {
            get
            {
                return MaskingManager.MaskingEnabled && (m_MaskingEnabled || !s_AuthoringMode);
            }
            set { m_MaskingEnabled = value; }
        }
        [SerializeField]
        bool m_MaskingEnabled = true;

        TutorialStyles Styles { get { return TutorialProjectSettings.Instance.TutorialStyle; } }

        [SerializeField]
        int m_FarthestPageCompleted = -1;

        [SerializeField]
        bool m_PlayModeChanging;

        internal VideoPlaybackManager VideoPlaybackManager { get; } = new VideoPlaybackManager();

        void OnTutorialContainerModified()
        {
            // Update the tutorial content in real-time when changed
            // TODO we end up reinitializing the whole UI when editing a single field of TutorialContainer.
            // Implement more granular updates.
            if (currentTutorial == null)
                InitializeUI();
        }

        void TrackPlayModeChanging(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    m_PlayModeChanging = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    m_PlayModeChanging = false;
                    break;
            }
        }

        void OnFocus()
        {
            readme = FindReadme(); // TODO could this be removed?
        }

        /// <summary>
        /// TODO 2.0 make internal
        /// </summary>
        /// <param name="newTexture"></param>
        public void UpdateVideoFrame(Texture newTexture)
        {
            rootVisualElement.Q("TutorialMedia").style.backgroundImage = Background.FromTexture2D((Texture2D)newTexture);
        }

        void UpdateHeader(TextElement contextText, TextElement titleText, VisualElement backDrop)
        {
            bool hasTutorial = currentTutorial != null;
            var context = readme != null ? readme.Title.Value : string.Empty;
            var title = hasTutorial ? currentTutorial.TutorialTitle.Value : readme?.ProjectName.Value;
            // For now drawing header only for Readme
            if (readme)
            {
                contextText.text = context;
                titleText.text = title;

                backDrop.style.backgroundImage = readme.HeaderBackground;
            }
        }

        void ScrollToTop()
        {
            ((ScrollView)this.rootVisualElement.Q("TutorialContainer").ElementAt(0)).scrollOffset = Vector2.zero;
        }

        void ShowCurrentTutorialContent()
        {
            if (!m_AllParagraphs.Any() || !currentTutorial)
                return;
            if (m_AllParagraphs.Count() <= currentTutorial.CurrentPageIndex)
                return;

            ScrollToTop();

            TutorialParagraph instruction = null;
            TutorialParagraph narrative = null;
            Tutorial endLink = null;
            string endText = "";

            foreach (TutorialParagraph para in currentTutorial.CurrentPage.Paragraphs)
            {
                if (para.Type == ParagraphType.SwitchTutorial)
                {
                    endLink = para.m_Tutorial;
                    endText = para.Text;
                }
                if (para.Type == ParagraphType.Narrative)
                {
                    narrative = para;
                }
                if (para.Type == ParagraphType.Instruction)
                {
                    instruction = para;
                }
                if (para.Type == ParagraphType.Image)
                {
                    if (para.Image != null)
                    {
                        ShowElement("TutorialMediaContainer");
                        rootVisualElement.Q("TutorialMedia").style.backgroundImage = para.Image;
                    }
                    else
                    {
                        HideElement("TutorialMediaContainer");
                    }
                }
                if (para.Type == ParagraphType.Video)
                {
                    if (para.Video != null)
                    {
                        ShowElement("TutorialMediaContainer");
                        rootVisualElement.Q("TutorialMedia").style.backgroundImage = VideoPlaybackManager.GetTextureForVideoClip(para.Video);
                    }
                    else
                    {
                        HideElement("TutorialMediaContainer");
                    }
                }
            }

            Button linkButton = rootVisualElement.Q<Button>("LinkButton");
            if (endLink != null)
            {
                linkButton.clickable.clicked += () => TutorialManager.instance.StartTutorial(endLink);
                linkButton.text = Tr(endText);
                ShowElement(linkButton);
            }
            else
            {
                HideElement(linkButton);
            }

            if (narrative != null)
            {
                rootVisualElement.Q<Label>("TutorialTitle").text = narrative.Title;
                RichTextToVisualElements(narrative.Text, rootVisualElement.Q("TutorialStepBox1"));
            }

            if (instruction == null || string.IsNullOrEmpty(instruction.Text))
            {
                // hide instruction box if no text
                HideElement("InstructionContainer");
            }
            else
            {
                // populate instruction box
                ShowElement("InstructionContainer");
                if (string.IsNullOrEmpty(instruction.Title))
                    HideElement("InstructionTitle");
                else
                    ShowElement("InstructionTitle");
                rootVisualElement.Q<Label>("InstructionTitle").text = instruction.Title;
                RichTextToVisualElements(instruction.Text, rootVisualElement.Q("InstructionDescription"));
            }

            if (IsFirstPage())
            {
                ShowElement("NextButtonBase");
            }
            else
            {
                HideElement("NextButtonBase");
            }
        }

        // Sets the instruction highlight to green or blue and toggles between arrow and checkmark
        void UpdateInstructionBox()
        {
            if (CanMoveToNextPage && currentTutorial.CurrentPage.HasCriteria())
            {
                ShowElement("InstructionHighlightGreen");
                HideElement("InstructionHighlightBlue");
                ShowElement("InstructionCheckmark");
                HideElement("InstructionArrow");
            }
            else
            {
                HideElement("InstructionHighlightGreen");
                ShowElement("InstructionHighlightBlue");
                HideElement("InstructionCheckmark");
                ShowElement("InstructionArrow");
            }
        }

        void UpdatePageState()
        {
            // TODO delayCall needed for now as some criteria don't have up-to-date state when at the moment
            // we call this function, causing canMoveToNextPage to return false even though the criteria
            // are completed.
            EditorApplication.delayCall += () =>
            {
                UpdateInstructionBox();
                SetNextButtonEnabled(CanMoveToNextPage);
            };
        }

        void OnCriterionCompleted(Criterion criterion)
        {
            // The criterion might be non-pertinent for the window (e.g. when running unit tests)
            // TODO Ideally we'd subscribe only to the criteria of the current page so we don't need to check this
            if (!currentTutorial ||
                !currentTutorial.Pages
                    .SelectMany(page => page.Paragraphs)
                    .SelectMany(para => para.Criteria)
                    .Select(crit => crit.Criterion)
                    .Contains(criterion)
            )
            {
                return;
            }

            UpdatePageState();
        }

        void SetNextButtonEnabled(bool enable)
        {
            rootVisualElement.Q("NextButton").SetEnabled(enable);
        }

        void CreateTutorialMenuCards(VisualTreeAsset vistree, string cardElementName, string linkCardElementName, VisualElement cardContainer)
        {
            var cards = Cards.OrderBy(card => card.OrderInView).ToArray();

            cardContainer.style.alignItems = Align.Center;

            for (int index = 0; index < cards.Length; ++index)
            {
                var card = cards[index];

                // If it's a tutorial, use tutorial card - otherwise link card
                VisualElement cardElement = vistree.CloneTree().Q("TutorialsContainer").Q(card.IsTutorial ? cardElementName : linkCardElementName);
                cardElement.Q<Label>("TutorialName").text = card.Heading;
                cardElement.Q<Label>("TutorialDescription").text = card.Text;
                if (card.IsTutorial)
                {
                    cardElement.RegisterCallback((MouseUpEvent evt) =>
                    {
                        card.StartTutorial();
                        //ShowCurrentTutorialContent();
                    });
                }
                if (!string.IsNullOrEmpty(card.Url))
                {
                    AnalyticsHelper.SendExternalReferenceImpressionEvent(card.Url, card.Heading.Untranslated, card.LinkText, card.TutorialId);

                    cardElement.RegisterCallback((MouseUpEvent evt) =>
                    {
                        card.OpenUrl();
                    });
                }

                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        // HACK: needs two delaycalls or GenesisHelper gives 404
                        FetchTutorialStates();
                    };
                };

                cardElement.Q<Label>("CompletionStatus").text = cards[index].TutorialCompleted ? Tr("COMPLETED") : "";
                SetElementVisible(cardElement.Q("TutorialCheckmark"), cards[index].TutorialCompleted);

                EditorCoroutineUtility.StartCoroutineOwnerless(EnforceCheckmark(cards[index], cardElement));

                if (card.Image != null)
                {
                    cardElement.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(card.Image);
                }
                cardElement.tooltip = card.IsTutorial ? Tr("Tutorial: ") + card.Text : card.Url;
                cardContainer.Add(cardElement);
            }
        }

        IEnumerator EnforceCheckmark(TutorialContainer.Section section, VisualElement element)
        {
            float seconds = 20f;
            while (seconds > 0f && !DoneFetchingTutorialStates)
            {
                yield return null;
                seconds -= Time.deltaTime;
            }

            element.Q<Label>("CompletionStatus").text = section.TutorialCompleted ? Tr("COMPLETED") : "";
            SetElementVisible(element.Q("TutorialCheckmark"), section.TutorialCompleted);
        }

        void RenderVideoIfPossible()
        {
            var paragraphType = currentTutorial?.CurrentPage?.Paragraphs.ElementAt(0).Type;
            if (paragraphType == ParagraphType.Video || paragraphType == ParagraphType.Image)
            {
                var pageCompleted = currentTutorial.CurrentPageIndex <= m_FarthestPageCompleted;
                var previousTaskState = true;
                GetCurrentParagraph().ElementAt(0).Draw(ref previousTaskState, pageCompleted);
            }
        }

        void OnEnable()
        {
            InitializeUI();
        }

        void InitializeUI()
        {
            m_WindowTitleContent = Tr("Tutorials");
            m_HomePromptTitle = Tr("Return to Tutorials?");
            m_HomePromptText = Tr(
                "Returning to the Tutorial Selection means exiting the tutorial and losing all of your progress\n" +
                "Do you wish to continue?"
            );
            m_PromptYes = Tr("Yes");
            m_PromptNo = Tr("No");
            m_PromptOk = Tr("OK");
            // Unity's menu guide convetion: text in italics, '>' used as a separator
            // NOTE EditorUtility.DisplayDialog doesn't support italics so cannot use rich text here.
            m_MenuPathGuide = Tr(TutorialWindowMenuItem.Menu) + " > " + Tr(TutorialWindowMenuItem.Item);

            m_TabClosedDialogTitle = Tr("Close Tutorials");
            m_TabClosedDialogText = string.Format(Tr("You can find Tutorials later by choosing {0} in the top menu."), m_MenuPathGuide);

            rootVisualElement.Clear();

            instance = this;

            Criterion.CriterionCompleted += OnCriterionCompleted;

            IMGUIContainer imguiToolBar = new IMGUIContainer(OnGuiToolbar);
            IMGUIContainer videoBox = new IMGUIContainer(RenderVideoIfPossible);
            videoBox.style.alignSelf = new StyleEnum<Align>(Align.Center);
            videoBox.name = "VideoBox";

            var root = rootVisualElement;
            var topBarAsset = LoadUIAsset<VisualTreeAsset>("Main.uxml");
            var tutorialContentAsset = LoadUIAsset<VisualTreeAsset>("TutorialContents.uxml");
            VisualElement tutorialImage = topBarAsset.CloneTree().Q("TutorialImage");
            VisualElement tutorialMenuCard = topBarAsset.CloneTree().Q("CardContainer");

            VisualElement tutorialContents = tutorialContentAsset.CloneTree().Q("TutorialEmptyContents");
            tutorialContents.style.flexGrow = 1f;
            VisualElement TutorialContentPage = tutorialContentAsset.CloneTree().Q("TutorialPageContainer");
            VisualElement TutorialTopBar = TutorialContentPage.Q("Header");

            VisualElement linkButton = topBarAsset.CloneTree().Q("LinkButton");

            VisualElement cardContainer = topBarAsset.CloneTree().Q("TutorialListScrollView");
            CreateTutorialMenuCards(topBarAsset, "CardContainer", "LinkCardContainer", cardContainer); //[TODO] be careful: this will also trigger analytics events even when you start a tutorial

            tutorialContents.Add(cardContainer);
            VisualElement topBarVisElement = topBarAsset.CloneTree().Q("TitleHeader");
            VisualElement footerBar = topBarAsset.CloneTree().Q("TutorialActions");

            TextElement titleElement = topBarVisElement.Q<TextElement>("TitleLabel");
            TextElement contextTextElement = topBarVisElement.Q<TextElement>("ContextLabel");

            UpdateHeader(contextTextElement, titleElement, topBarVisElement);

            root.Add(imguiToolBar);
            root.Add(TutorialTopBar);
            root.Add(videoBox);
            root.Add(topBarVisElement);
            root.Add(tutorialContents);

            Styles.ApplyThemeStyleSheetTo(root);

            VisualElement tutorialContainer = TutorialContentPage.Q("TutorialContainer");
            tutorialContainer.Add(linkButton);
            root.Add(tutorialContainer);

            footerBar.Q<Button>("PreviousButton").clicked += OnPreviousButtonClicked;
            footerBar.Q<Button>("NextButton").clicked += OnNextButtonClicked;

            // Set here in addition to CreateWindow() so that title of old saved layouts is overwritten,
            // also make sure the title is translated always.
            instance.titleContent.text = m_WindowTitleContent;

            VideoPlaybackManager.OnEnable();

            GUIViewProxy.PositionChanged += OnGUIViewPositionChanged;
            HostViewProxy.actualViewChanged += OnHostViewActualViewChanged;
            Tutorial.TutorialPagesModified += OnTutorialPagesModified;

            // test for page completion state changes (rather than criteria completion/invalidation directly)
            // so that page completion state will be up-to-date
            TutorialPage.CriteriaCompletionStateTested -= OnTutorialPageCriteriaCompletionStateTested; // TODO hotfix for auto-completion issues introduced in  7f238cbbcc289120f5c086b66f6bb3c003197a06
            TutorialPage.CriteriaCompletionStateTested += OnTutorialPageCriteriaCompletionStateTested;
            TutorialPage.TutorialPageMaskingSettingsChanged += OnTutorialPageMaskingSettingsChanged;
            TutorialPage.TutorialPageNonMaskingSettingsChanged += OnTutorialPageNonMaskingSettingsChanged;
            EditorApplication.playModeStateChanged -= TrackPlayModeChanging;
            EditorApplication.playModeStateChanged += TrackPlayModeChanging;

            root.Add(footerBar);
            SetUpTutorial();

            MaskingEnabled = true;

            readme = FindReadme();
            EditorCoroutineUtility.StartCoroutineOwnerless(DelayedOnEnable());
        }

        void ExitClicked(MouseUpEvent mouseup)
        {
            SkipTutorial();
        }

        void SetIntroScreenVisible(bool visible)
        {
            if (visible)
            {
                ShowElement("TitleHeader");
                HideElement("TutorialActions");
                HideElement("Header");
                ShowElement("TutorialEmptyContents");
                // SHOW: tutorials
                // HIDE: tutorial steps
                HideElement("TutorialContainer");
                // Show card container
            }
            else
            {
                HideElement("TitleHeader");
                ShowElement("TutorialActions");
                VisualElement headerElement = rootVisualElement.Q("Header");
                ShowElement(headerElement);
                headerElement.Q<Label>("HeaderLabel").text = currentTutorial.TutorialTitle;
                headerElement.Q<Label>("StepCount").text = $"{currentTutorial.CurrentPageIndex + 1} / {currentTutorial.m_Pages.Count}";
                headerElement.Q("Close").RegisterCallback<MouseUpEvent>(ExitClicked);
                //HideElement("TutorialImage");
                HideElement("TutorialEmptyContents");
                ShowElement("TutorialContainer");
                //ShowElement("VideoBox");
                // Hide card container
            }
            rootVisualElement.Q<Button>("PreviousButton").text = m_BackButtonText;
            rootVisualElement.Q<Button>("NextButton").text = m_NextButtonText;
        }

        void ShowElement(string name) => ShowElement(rootVisualElement.Q(name));
        void HideElement(string name) => HideElement(rootVisualElement.Q(name));

        static void ShowElement(VisualElement elem) => SetElementVisible(elem, true);
        static void HideElement(VisualElement elem) => SetElementVisible(elem, false);

        static void SetElementVisible(VisualElement elem, bool visible)
        {
            elem.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDisable()
        {
            if (!m_PlayModeChanging)
            {
                AnalyticsHelper.TutorialEnded(TutorialConclusion.Quit);
            }

            Criterion.CriterionCompleted -= OnCriterionCompleted;

            ClearTutorialListener();

            Tutorial.TutorialPagesModified -= OnTutorialPagesModified;
            TutorialPage.CriteriaCompletionStateTested -= OnTutorialPageCriteriaCompletionStateTested;
            TutorialPage.TutorialPageMaskingSettingsChanged -= OnTutorialPageMaskingSettingsChanged;
            TutorialPage.TutorialPageNonMaskingSettingsChanged -= OnTutorialPageNonMaskingSettingsChanged;
            GUIViewProxy.PositionChanged -= OnGUIViewPositionChanged;
            HostViewProxy.actualViewChanged -= OnHostViewActualViewChanged;

            VideoPlaybackManager.OnDisable();

            ApplyMaskingSettings(false);
        }

        void OnDestroy()
        {
            // TODO SkipTutorial();?

            // Play mode might trigger layout change (maximize on play) and closing of this window also.
            if (ShowTutorialsClosedDialog && !TutorialManager.IsLoadingLayout && !m_PlayModeChanging)
            {
                // Delay call prevents us getting the dialog upon assembly reload.
                EditorApplication.delayCall += delegate
                {
                    ShowTutorialsClosedDialog.SetValue(false);
                    EditorUtility.DisplayDialog(m_TabClosedDialogTitle, m_TabClosedDialogText, m_PromptOk);
                };
            }
        }

        void WindowForParagraph()
        {
            foreach (var p in m_Paragraphs)
            {
                p.SetWindow(instance);
            }
        }

        void OnHostViewActualViewChanged()
        {
            if (TutorialManager.IsLoadingLayout) { return; }
            // do not mask immediately in case unmasked GUIView doesn't exist yet
            // TODO disabled for now in order to get Welcome dialog masking working
            //QueueMaskUpdate();
        }

        void QueueMaskUpdate()
        {
            EditorApplication.update -= ApplyQueuedMask;
            EditorApplication.update += ApplyQueuedMask;
        }

        void OnTutorialPageCriteriaCompletionStateTested(TutorialPage sender)
        {
            if (currentTutorial == null || currentTutorial.CurrentPage != sender) { return; }

            foreach (var paragraph in m_Paragraphs)
            {
                paragraph.ResetState();
            }

            if (sender.AreAllCriteriaSatisfied && sender.AutoAdvanceOnComplete && !sender.HasMovedToNextPage)
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GoToNextPageAfterDelay());
                return;
            }

            ApplyMaskingSettings(true);
        }

        IEnumerator GoToNextPageAfterDelay()
        {
            //TODO WaitForSecondsRealtime();
            float seconds = 0.5f;
            while (seconds > 0f)
            {
                seconds -= Time.deltaTime;
                yield return null;
            }
            if (currentTutorial.TryGoToNextPage())
            {
                UpdatePageState();
                yield break;
            }
            ApplyMaskingSettings(true);
        }

        void SkipTutorial()
        {
            if (currentTutorial == null) { return; }

            switch (currentTutorial.SkipTutorialBehavior)
            {
                case Tutorial.SkipTutorialBehaviorType.SameAsExitBehavior: ExitTutorial(false); break;
                case Tutorial.SkipTutorialBehaviorType.SkipToLastPage: currentTutorial.SkipToLastPage(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        void ExitTutorial(bool completed)
        {
            switch (currentTutorial.ExitBehavior)
            {
                case Tutorial.ExitBehaviorType.ShowHomeWindow:
                    if (completed)
                    {
                        HomeWindowProxy.ShowTutorials();
                    }
                    else if (
                        !IsInProgress() ||
                        EditorUtility.DisplayDialog(m_HomePromptTitle, m_HomePromptText, m_PromptYes, m_PromptNo))
                    {
                        HomeWindowProxy.ShowTutorials();
                        GUIUtility.ExitGUI();
                    }
                    return; // Return to avoid selecting asset on exit
                case Tutorial.ExitBehaviorType.CloseWindow:
                    // New behaviour: exiting resets and nullifies the current tutorial and shows the project's tutorials.
                    if (completed)
                    {
                        SetTutorial(null, false);
                        ResetTutorial();
                        TutorialManager.instance.RestoreOriginalState();
                    }
                    else
                    // TODO experimenting with UX that never shows the exit dialog, can be removed for good if deemed good.
                    //    if (!IsInProgress()
                    //    || EditorUtility.DisplayDialog(k_ExitPromptTitle.text, k_ExitPromptText.text, k_PromptYes.text, k_PromptNo.text))
                    {
                        currentTutorial.CurrentPage.RaiseOnBeforeQuitTutorialEvent();
                        SetTutorial(null, false);
                        ResetTutorial();
                        TutorialManager.instance.RestoreOriginalState();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // TODO new behaviour testing: assetSelectedOnExit was originally used for selecting
            // Readme but this is not required anymore as the TutorialWindow contains Readme's functionality.
            //if (currentTutorial?.assetSelectedOnExit != null)
            //    Selection.activeObject = currentTutorial.assetSelectedOnExit;

            //SaveTutorialStates();
        }

        void OnTutorialInitiated()
        {
            if (!currentTutorial) { return; }

            AnalyticsHelper.TutorialStarted(currentTutorial);
            GenesisHelper.LogTutorialStarted(currentTutorial.LessonId);
            CreateTutorialViews();
        }

        void OnTutorialCompleted(bool exitTutorial)
        {
            if (!currentTutorial) { return; }

            AnalyticsHelper.TutorialEnded(TutorialConclusion.Completed);
            GenesisHelper.LogTutorialEnded(currentTutorial.LessonId);
            MarkTutorialCompleted(currentTutorial.LessonId, currentTutorial.Completed);

            if (!exitTutorial) { return; }
            ExitTutorial(currentTutorial.Completed);
        }

        internal void CreateTutorialViews()
        {
            if (currentTutorial == null) return; // HACK
            m_AllParagraphs.Clear();
            foreach (var page in currentTutorial.Pages)
            {
                if (page == null) { continue; }

                var instructionIndex = 0;
                foreach (var paragraph in page.Paragraphs)
                {
                    if (paragraph.Type == ParagraphType.Instruction)
                    {
                        ++instructionIndex;
                    }
                    m_AllParagraphs.Add(new TutorialParagraphView(paragraph, instance, Styles.OrderedListDelimiter, Styles.UnorderedListBullet, instructionIndex));
                }
            }
        }

        List<TutorialParagraphView> GetCurrentParagraph()
        {
            if (m_Indexes == null || m_Indexes.Length != currentTutorial.PageCount)
            {
                // Update page to paragraph index
                m_Indexes = new int[currentTutorial.PageCount];
                var pageIndex = 0;
                var paragraphIndex = 0;
                foreach (var page in currentTutorial.Pages)
                {
                    m_Indexes[pageIndex++] = paragraphIndex;
                    if (page != null)
                        paragraphIndex += page.Paragraphs.Count();
                }
            }

            List<TutorialParagraphView> tmp = new List<TutorialParagraphView>();
            if (m_Indexes.Length > 0)
            {
                var endIndex = currentTutorial.CurrentPageIndex + 1 > currentTutorial.PageCount - 1 ? m_AllParagraphs.Count : m_Indexes[currentTutorial.CurrentPageIndex + 1];
                for (int i = m_Indexes[currentTutorial.CurrentPageIndex]; i < endIndex; i++)
                {
                    tmp.Add(m_AllParagraphs[i]);
                }
            }
            return tmp;
        }

        // TODO 'page' and 'index' unused
        internal void PrepareNewPage(TutorialPage page = null, int index = 0)
        {
            if (currentTutorial == null) return;
            if (!m_AllParagraphs.Any())
            {
                CreateTutorialViews();
            }
            m_Paragraphs.Clear();

            if (currentTutorial.CurrentPage == null)
            {
                m_NextButtonText = string.Empty;
            }
            else
            {
                m_NextButtonText = IsLastPage()
                    ? currentTutorial.CurrentPage.DoneButton
                    : currentTutorial.CurrentPage.NextButton;
            }
            m_BackButtonText = IsFirstPage() ? Tr("All Tutorials") : Tr("Back");

            m_Paragraphs = GetCurrentParagraph();

            m_Paragraphs.TrimExcess();

            WindowForParagraph();
            ShowCurrentTutorialContent(); // HACK
        }

        internal void ForceInititalizeTutorialAndPage()
        {
            m_FarthestPageCompleted = -1;

            CreateTutorialViews();
            PrepareNewPage();
        }

        static void OpenLoadTutorialDialog()
        {
            string assetPath = EditorUtility.OpenFilePanel("Load a Tutorial", "Assets", "asset");
            if (string.IsNullOrEmpty(assetPath)) { return; }
            assetPath = string.Format("Assets{0}", assetPath.Substring(Application.dataPath.Length));
            TutorialManager.instance.StartTutorial(AssetDatabase.LoadAssetAtPath<Tutorial>(assetPath));
            GUIUtility.ExitGUI();
        }

        bool IsLastPage() { return currentTutorial != null && currentTutorial.PageCount - 1 <= currentTutorial.CurrentPageIndex; }

        bool IsFirstPage() { return currentTutorial != null && currentTutorial.CurrentPageIndex == 0; }

        // Returns true if some real progress has been done (criteria on some page finished).
        bool IsInProgress()
        {
            return currentTutorial
                ?.Pages.Any(pg => pg.Paragraphs.Any(p => p.Criteria.Any() && pg.AreAllCriteriaSatisfied))
                ?? false;
        }

        void ClearTutorialListener()
        {
            if (currentTutorial == null) { return; }

            currentTutorial.TutorialInitiated -= OnTutorialInitiated;
            currentTutorial.TutorialCompleted -= OnTutorialCompleted;
            currentTutorial.PageInitiated -= OnShowPage;
            currentTutorial.StopTutorial();
        }

        internal void SetTutorial(Tutorial tutorial, bool reload)
        {
            ClearTutorialListener();

            currentTutorial = tutorial;
            if (currentTutorial != null)
            {
                if (reload)
                {
                    currentTutorial.ResetProgress();
                }
                m_AllParagraphs.Clear();
                m_Paragraphs.Clear();
            }

            ApplyMaskingSettings(currentTutorial != null);

            SetUpTutorial();
        }

        void SetUpTutorial()
        {
            // bail out if this instance no longer exists such as when e.g., loading a new window layout
            if (this == null || currentTutorial == null || currentTutorial.CurrentPage == null) { return; }

            if (currentTutorial.CurrentPage != null)
            {
                currentTutorial.CurrentPage.Initiate();
            }

            currentTutorial.TutorialInitiated += OnTutorialInitiated;
            currentTutorial.TutorialCompleted += OnTutorialCompleted;
            currentTutorial.PageInitiated += OnShowPage;

            if (m_AllParagraphs.Any())
            {
                PrepareNewPage();
                return;
            }
            ForceInititalizeTutorialAndPage();
        }

        void ApplyQueuedMask()
        {
            if (IsParentNull()) { return; }

            EditorApplication.update -= ApplyQueuedMask;
            ApplyMaskingSettings(true);
        }

        IEnumerator DelayedOnEnable()
        {
            yield return null;

            do
            {
                yield return null;
                m_VideoBoxElement = rootVisualElement.Q("TutorialMediaContainer");
            }
            while (m_VideoBoxElement == null);


            if (currentTutorial == null)
            {
                if (m_VideoBoxElement != null)
                {
                    HideElement(m_VideoBoxElement);
                }
            }
            VideoPlaybackManager.OnEnable();
        }

        void OnGuiToolbar()
        {
            // TODO calling SetIntroScreenVisible every OnGUI, not probably wanted.
            SetIntroScreenVisible(currentTutorial == null);
            if (s_AuthoringMode)
                ToolbarGUI();
        }

        void OnPreviousButtonClicked()
        {
            if (IsFirstPage())
            {
                SkipTutorial();
            }
            else
            {
                currentTutorial.GoToPreviousPage();
                UpdatePageState();
                // TODO OnNextButtonClicked has ShowCurrentTutorialContent() but this doesn't --
                // is this on purpose?
            }
        }

        void OnNextButtonClicked()
        {
            if (currentTutorial)
                currentTutorial.TryGoToNextPage();

            UpdatePageState();
            ShowCurrentTutorialContent();
        }

        // Resets the contents of this window. Use this before saving layouts for tutorials.
        internal void Reset()
        {
            m_AllParagraphs.Clear();
            SetTutorial(null, true);
            readme = null;
        }

        void ToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            bool Button(string text)
            {
                return GUILayout.Button(text, EditorStyles.toolbarButton, GUILayout.MaxWidth(s_AuthoringModeToolbarButtonWidth));
            }

            using (new EditorGUI.DisabledScope(currentTutorial == null))
            {
                if (Button(Tr("Select Tutorial")))
                {
                    Selection.activeObject = currentTutorial;
                }

                using (new EditorGUI.DisabledScope(currentTutorial?.CurrentPage == null))
                {
                    if (Button(Tr("Select Page")))
                    {
                        Selection.activeObject = currentTutorial.CurrentPage;
                    }
                }

                if (Button(Tr("Skip To End")))
                {
                    currentTutorial.SkipToLastPage();
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(currentTutorial == null))
            {
                EditorGUI.BeginChangeCheck();
                MaskingEnabled = GUILayout.Toggle(
                    MaskingEnabled, Tr("Preview Masking"), EditorStyles.toolbarButton,
                    GUILayout.MaxWidth(s_AuthoringModeToolbarButtonWidth)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMaskingSettings(true);
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            if (Button(Tr("Run Startup Code")))
            {
                UserStartupCode.RunStartupCode();
            }

            EditorGUILayout.EndHorizontal();
        }

        void OnTutorialPagesModified(Tutorial sender)
        {
            if (currentTutorial == null || currentTutorial != sender) { return; }

            CreateTutorialViews();
            ShowCurrentTutorialContent();

            ApplyMaskingSettings(true);
        }

        void OnTutorialPageMaskingSettingsChanged(TutorialPage sender)
        {
            if (currentTutorial == null || currentTutorial.CurrentPage != sender) { return; }

            ApplyMaskingSettings(true);
        }

        void OnTutorialPageNonMaskingSettingsChanged(TutorialPage sender)
        {
            if (currentTutorial == null || currentTutorial.CurrentPage != sender) { return; }

            ShowCurrentTutorialContent();
        }

        void OnShowPage(TutorialPage page, int index)
        {
            page.RaiseOnBeforePageShownEvent();
            m_FarthestPageCompleted = Mathf.Max(m_FarthestPageCompleted, index - 1);
            ApplyMaskingSettings(true);

            AnalyticsHelper.PageShown(page, index);
            PrepareNewPage();

            VideoPlaybackManager.ClearCache();
            page.RaiseOnAfterPageShownEvent();
        }

        void OnGUIViewPositionChanged(UnityObject sender)
        {
            if (TutorialManager.IsLoadingLayout || sender.GetType().Name == "TooltipView") { return; }

            ApplyMaskingSettings(true);
        }

        void ApplyMaskingSettings(bool applyMask)
        {
            // TODO IsParentNull() probably not needed anymore as TutorialWindow is always parented in the current design & layout.
            if (!applyMask || !MaskingEnabled || currentTutorial == null
                || currentTutorial.CurrentPage == null || IsParentNull() || TutorialManager.IsLoadingLayout)
            {
                MaskingManager.Unmask();
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            MaskingSettings maskingSettings = currentTutorial.CurrentPage.CurrentMaskingSettings;
            try
            {
                if (maskingSettings == null || !maskingSettings.Enabled)
                {
                    MaskingManager.Unmask();
                }
                else
                {
                    bool foundAncestorProperty;
                    var unmaskedViews = UnmaskedView.GetViewsAndRects(maskingSettings.UnmaskedViews, out foundAncestorProperty);
                    if (foundAncestorProperty)
                    {
                        // Keep updating mask when target property is not unfolded
                        QueueMaskUpdate();
                    }

                    if (currentTutorial.CurrentPageIndex <= m_FarthestPageCompleted)
                    {
                        unmaskedViews = new UnmaskedView.MaskData();
                    }

                    UnmaskedView.MaskData highlightedViews;

                    if (unmaskedViews.Count > 0) //Unmasked views should be highlighted
                    {
                        highlightedViews = (UnmaskedView.MaskData)unmaskedViews.Clone();
                    }
                    else if (CanMoveToNextPage) // otherwise, if the current page is completed, highlight this window
                    {
                        highlightedViews = new UnmaskedView.MaskData();
                        highlightedViews.AddParentFullyUnmasked(this);
                    }
                    else // otherwise, highlight manually specified control rects if there are any
                    {
                        var unmaskedControls = new List<GUIControlSelector>();
                        var unmaskedViewsWithControlsSpecified =
                            maskingSettings.UnmaskedViews.Where(v => v.GetUnmaskedControls(unmaskedControls) > 0).ToArray();
                        // if there are no manually specified control rects, highlight all unmasked views
                        highlightedViews = UnmaskedView.GetViewsAndRects(
                            unmaskedViewsWithControlsSpecified.Length == 0 ?
                            maskingSettings.UnmaskedViews : unmaskedViewsWithControlsSpecified
                        );
                    }

                    // ensure tutorial window's HostView and tooltips are not masked
                    unmaskedViews.AddParentFullyUnmasked(this);
                    unmaskedViews.AddTooltipViews();

                    // tooltip views should not be highlighted
                    highlightedViews.RemoveTooltipViews();

                    MaskingManager.Mask(
                        unmaskedViews,
                        Styles == null ? Color.magenta * new Color(1f, 1f, 1f, 0.8f) : Styles.MaskingColor,
                        highlightedViews,
                        Styles == null ? Color.cyan * new Color(1f, 1f, 1f, 0.8f) : Styles.HighlightColor,
                        Styles == null ? new Color(1, 1, 1, 0.5f) : Styles.BlockedInteractionColor,
                        Styles == null ? 3f : Styles.HighlightThickness
                    );
                }
            }
            catch (ArgumentException e)
            {
                if (s_AuthoringMode)
                    Debug.LogException(e, currentTutorial.CurrentPage);
                else
                    Console.WriteLine(StackTraceUtility.ExtractStringFromException(e));

                MaskingManager.Unmask();
            }
            finally
            {
                InternalEditorUtility.RepaintAllViews();
            }
        }

        void ResetTutorialOnDelegate(PlayModeStateChange playmodeChange)
        {
            switch (playmodeChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.playModeStateChanged -= ResetTutorialOnDelegate;
                    ResetTutorial();
                    break;
            }
        }

        internal void ResetTutorial()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += ResetTutorialOnDelegate;
                EditorApplication.isPlaying = false;
                return;
            }
            else if (!EditorApplication.isPlaying)
            {
                m_FarthestPageCompleted = -1;
                TutorialManager.instance.ResetTutorial();
            }
        }

        /// <summary>
        /// Returns Readme iff one Readme exists in the project.
        /// TODO make internal in 2.0
        /// </summary>
        /// <returns></returns>
        public static TutorialContainer FindReadme()
        {
            var ids = AssetDatabase.FindAssets($"t:{typeof(TutorialContainer).FullName}");
            return ids.Length == 1
                ? (TutorialContainer)AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]))
                : null;
        }

        float checkLanguageTick = 0f;
        float blinkTick = 0f;
        bool blinkOn = true;

        float editorDeltaTime = 0f;
        float lastTimeSinceStartup = 0f;


        private void SetEditorDeltaTime()
        {
            if (lastTimeSinceStartup == 0f)
            {
                lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
            }
            editorDeltaTime = (float)EditorApplication.timeSinceStartup - lastTimeSinceStartup;
            lastTimeSinceStartup = (float)EditorApplication.timeSinceStartup;
        }

        void Update()
        {
            SetEditorDeltaTime();

            blinkTick += editorDeltaTime;
            checkLanguageTick += editorDeltaTime;
            currentTutorial?.CurrentPage?.RaiseOnTutorialPageStayEvent();

            if (blinkTick > 1f)
            {
                blinkTick -= 1f;
                if (IsFirstPage())
                {
                    if (blinkOn)
                    {
                        ShowElement("NextButtonBase");
                    }
                    else
                    {
                        HideElement("NextButtonBase");
                    }
                    blinkOn = !blinkOn;
                }
            }

            if (checkLanguageTick >= 1f)
            {
                checkLanguageTick = 0f;
                if (LocalizationDatabaseProxy.currentEditorLanguage != m_CurrentEditorLanguage)
                {
                    m_CurrentEditorLanguage = LocalizationDatabaseProxy.currentEditorLanguage;
                    InitializeUI();
                }
            }
        }

        internal void MarkAllTutorialsUncompleted()
        {
            Cards.ToList().ForEach(s => MarkTutorialCompleted(s.TutorialId, false));
            // TODO Refresh the cards
        }

        bool DoneFetchingTutorialStates = false;

        // Fetches statuses from the web API
        internal void FetchTutorialStates()
        {
            DoneFetchingTutorialStates = false;
            GenesisHelper.GetAllTutorials((tutorials) =>
            {
                tutorials.ForEach(t => MarkTutorialCompleted(t.lessonId, t.status == "Finished"));
                DoneFetchingTutorialStates = true;
            });
        }

        void MarkTutorialCompleted(string lessonId, bool completed)
        {
            var sections = readme?.Sections ?? new TutorialContainer.Section[0];
            var section = Array.Find(sections, s => s.TutorialId == lessonId);
            if (section != null)
            {
                section.TutorialCompleted = completed;
                section.SaveState();
            }
        }
    }
}
