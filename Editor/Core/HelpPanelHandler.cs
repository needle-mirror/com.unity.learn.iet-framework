using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// An entry for the FAQ Array in the tutorial and tutorial page. A question with its associated answer
    /// </summary>
    [Serializable]
    public class FaqEntry
    {
        /// <summary>
        /// The Question this FAQ entry answer
        /// </summary>
        public string Question;
        /// <summary>
        /// The Answer to this FAQ entry
        /// </summary>
        public string Answer;
    }

    /// <summary>
    /// A Window that display the FAQ from the given tutorial and current page
    /// </summary>
    public class HelpPanelHandler
    {
        /// <summary>
        /// Section of the TutorialContainer hierarchy
        /// </summary>
        public enum Section
        {
            /// <summary>
            /// Designate the FAQ Entry defined on the TutorialContainer
            /// </summary>
            TutorialContainer,
            /// <summary>
            /// Designate the FAQ Entry defined on the Tutorial
            /// </summary>
            Tutorial,
            /// <summary>
            /// Designate the FAQ Entry defined on the Page
            /// </summary>
            Page
        }

        /// <summary>
        /// Is the Help Panel opened or hidden
        /// </summary>
        public bool IsOpened => m_IsOpened;

        private VisualTreeAsset m_EntryTemplate;
        private VisualElement m_SectionSelection;
        private Button m_TutorialSectionButton;
        private Button m_UnitSectionButton;
        private Button m_StepSectionButton;
        private Button m_CurrentSectionButtonEnabled;

        private VisualElement m_FaqRoot;
        private ScrollView m_FaqScrollView;
        private VisualElement m_EntriesRoot;
        private VisualElement m_EntriesContainer;

        private Label m_ReportLabel;
        private Button m_ReportButton;

        private Tutorial m_Tutorial;

        private bool m_IsOpened;
        private Section m_CurrentSection = Section.Page;

        private List<FaqEntry> m_TutorialEntries = new();
        private List<FaqEntry> m_UnitEntries = new();
        private List<FaqEntry> m_StepEntries = new();

        /// <summary>
        /// Initialize the Help panel, storing all references to UIElements it will need for its functionalities
        /// </summary>
        /// <param name="root">The VisualElement of which the panel is a child</param>
        public void Initialize(VisualElement root)
        {
            m_EntryTemplate = UIUtils.LoadUXML("FaqEntry");

            //use parent because as its a template
            m_FaqRoot = root.Q<VisualElement>("FaqBackground");

            m_FaqRoot.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                //we retranslate as this doesn't seem to be recomputed by the layout engine (the 100% is only computed
                //when assigned once)
                //we assign a different value first as reassigning the same value (e.g. setting length ot -100 when it's
                //already at -100, it won't trigger a recomputation)
                if (m_IsOpened)
                {
                    m_FaqRoot.style.translate = new Translate(0, 1);
                    m_FaqRoot.style.translate = new Translate(0, 0);
                }
                else
                {
                    m_FaqRoot.style.translate = new Translate(0, Length.Percent(-99));
                    m_FaqRoot.style.translate = new Translate(0, Length.Percent(-100));
                }
            });

            m_SectionSelection = root.Q<VisualElement>("ToggleGroup");
            m_TutorialSectionButton = m_SectionSelection.Q<Button>("TutorialSectionButton");
            m_UnitSectionButton = m_SectionSelection.Q<Button>("UnitSectionButton");
            m_StepSectionButton = m_SectionSelection.Q<Button>("StepSectionButton");

            m_TutorialSectionButton.clicked += () => { SwitchCategory(m_TutorialSectionButton, Section.TutorialContainer); };
            m_UnitSectionButton.clicked += () => { SwitchCategory(m_UnitSectionButton, Section.Tutorial); };
            m_StepSectionButton.clicked += () => { SwitchCategory(m_StepSectionButton, Section.Page); };

            m_FaqScrollView = root.Q<ScrollView>("FaqScrollView");
            m_EntriesRoot = m_FaqScrollView.Q<VisualElement>("Entries");

            VisualElement reportContainer = root.Q<VisualElement>("ReportEntry");
            m_ReportLabel = reportContainer.Q<Label>("ReportLabel");
            m_ReportLabel.text = Localization.Tr(LocalizationKeys.k_ReportProblemText);
            m_ReportButton = reportContainer.Q<Button>("ReportButton");
            m_ReportButton.clicked += TutorialEditorUtils.ReportLinkClicked;

            VisualElement askAiContainer = root.Q<VisualElement>("AskAI");
            Label askAILabel = askAiContainer.Q<Label>("AskAILabel");
            askAILabel.text = Localization.Tr(LocalizationKeys.k_AskAIText);
            Button askAIButton = askAiContainer.Q<Button>("AskAIButton");
            askAIButton.clicked += () =>
            {
                ListRequest listRequest = Client.List(true, false);
                while (!listRequest.IsCompleted) ;

                if (listRequest.Result.Any(info => info.name == "com.unity.muse.chat"))
                {
                    EditorApplication.ExecuteMenuItem("Muse/Chat");
                }
                else
                {
                    InstallAIWarningWindow win = InstallAIWarningWindow.OpenNew(
                        "To use the AI Assistant tool,\nyou need to install the AI packages\n\n" +
                        "Click on the highlighted button to install it");

                    win.OnClosed += MaskingManager.Unmask;

                    GuiControlSelector selector = new();
                    selector.SelectorMode = GuiControlSelector.Mode.VisualElement;
                    selector.VisualElementClassName = "unity-editor-toolbar-element";
                    selector.VisualElementName = "AIDropdown";
                    UnmaskedView views = UnmaskedView.CreateInstanceForGUIView(Type.GetType("UnityEditor.Toolbar, UnityEditor.CoreModule"), new []{selector});

                    UnmaskedView.MaskData unmaskedViews = UnmaskedView.GetViewsAndRects(new[] { views });
                    unmaskedViews.AddParentFullyUnmasked(win);
                    UnmaskedView.MaskData highlightedViews = UnmaskedView.GetViewsAndRects(new[] { views });

                    TutorialStyles styles = TutorialProjectSettings.Instance.TutorialStyle;
                    MaskingManager.Mask
                    (
                        unmaskedViews,
                        styles.MaskingColor,
                        highlightedViews,
                        styles.HighlightColor,
                        styles.BlockedInteractionColor,
                        styles.HighlightThickness
                    );
                }
            };

            askAiContainer.style.display = Unsupported.IsDeveloperMode() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Open the Help Panel using the given tutorial. Will listen to tutorial events like page initiated and quit
        /// to update the displayed question
        /// </summary>
        /// <param name="tutorial">The tutorial from which to display the FAQ entries</param>
        public void Open(Tutorial tutorial)
        {
            if(tutorial == null)
                return;

            m_Tutorial = tutorial;
            m_CurrentSection = Section.Page;

            TutorialWindow.Instance.CurrentTutorial.GetFaqQuestions(Section.TutorialContainer, out m_TutorialEntries);
            m_TutorialSectionButton.style.display = m_TutorialEntries.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            TutorialWindow.Instance.CurrentTutorial.GetFaqQuestions(Section.Tutorial, out m_UnitEntries);
            m_UnitSectionButton.style.display = m_UnitEntries.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            TutorialWindow.Instance.CurrentTutorial.GetFaqQuestions(Section.Page, out m_StepEntries);
            m_StepSectionButton.style.display = m_StepEntries.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            if (m_TutorialEntries.Count == 0 && m_UnitEntries.Count == 0 && m_StepEntries.Count == 0)
            {
                m_SectionSelection.parent.style.display = DisplayStyle.None;
                m_FaqScrollView.style.display = DisplayStyle.None;
            }
            else
            {
                if (m_StepEntries.Count > 0)
                {
                    //this ensure that switch category will work, as it early exit if the section is already set
                    m_CurrentSection = Section.TutorialContainer;
                    SwitchCategory(m_StepSectionButton, Section.Page);
                }
                else if (m_UnitEntries.Count > 0)
                {
                    //this ensure that switch category will work, as it early exit if the section is already set
                    m_CurrentSection = Section.TutorialContainer;
                    SwitchCategory(m_UnitSectionButton, Section.Tutorial);
                }
                else
                {
                    //this ensure that switch category will work, as it early exit if the section is already set
                    m_CurrentSection = Section.Tutorial;
                    SwitchCategory(m_TutorialSectionButton, Section.TutorialContainer);
                }

                m_SectionSelection.parent.style.display = DisplayStyle.Flex;
                m_FaqScrollView.style.display = DisplayStyle.Flex;

                RefreshEntries();
            }

            RegisterEvents(m_Tutorial);

            m_FaqRoot.style.translate = new Translate(0, 0);
            m_IsOpened = true;

            AnalyticsHelper.SendFaqOpenedEvent(TutorialWindow.Instance.CurrentTutorial.name,
                TutorialWindow.Instance.CurrentTutorial.CurrentPageIndex);
        }

        /// <summary>
        /// Close the Help Panel(roll back up)
        /// </summary>
        public void Close()
        {
            m_FaqRoot.style.translate = new Translate(0, Length.Percent(-100));
            m_IsOpened = false;

            UnregisterEvents(m_Tutorial);
            m_Tutorial = null;
        }

        private void RegisterEvents(Tutorial tutorial)
        {
            tutorial.PageInitiated.AddListener(OnPageInitiated);
            tutorial.Quit.AddListener(OnTutorialQuit);
        }

        private void UnregisterEvents(Tutorial tutorial)
        {
            tutorial?.PageInitiated.RemoveListener(OnPageInitiated);
            tutorial?.Quit.RemoveListener(OnTutorialQuit);
        }

        private void OnTutorialQuit(Tutorial tutorial)
        {
            if(m_Tutorial == null)
                return;

            UnregisterEvents(m_Tutorial);
        }

        private void OnPageInitiated(Tutorial tutorial, TutorialPage page, int pageIndex)
        {
            RefreshEntries();
        }

        private void SwitchCategory(Button categoryButton, Section newSection)
        {
            if (m_CurrentSection == newSection)
                return;

            if (m_CurrentSectionButtonEnabled != null)
            {
                m_CurrentSectionButtonEnabled.RemoveFromClassList("selected");
                m_CurrentSectionButtonEnabled.SetEnabled(true);
            }

            categoryButton.AddToClassList("selected");
            categoryButton.SetEnabled(false);

            m_CurrentSection = newSection;
            m_CurrentSectionButtonEnabled = categoryButton;
            RefreshEntries();
        }

        private void RefreshEntries()
        {
            m_EntriesRoot.Clear();

            TutorialWindow.Instance.CurrentTutorial.GetFaqQuestions(m_CurrentSection, out List<FaqEntry> questions);

            foreach (FaqEntry question in questions)
            {
                TemplateContainer newEntry = m_EntryTemplate.CloneTree();

                Foldout entryQuestion = newEntry.Q<Foldout>("Entry");
                entryQuestion.text = question.Question;

                entryQuestion.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        entryQuestion.AddToClassList("open");
                        AnalyticsHelper.SendFaqQuestionClickedEvent(TutorialWindow.Instance.CurrentTutorial.name,
                            TutorialWindow.Instance.CurrentTutorial.CurrentPageIndex, question.Question);
                    }
                    else
                    {
                        entryQuestion.RemoveFromClassList("open");
                    }
                });

                Label entryAnswer = newEntry.Q<Label>("Answer");
                entryAnswer.text = question.Answer;

                m_EntriesRoot.Add(newEntry);
            }
        }
    }

    internal class InstallAIWarningWindow : EditorWindow
    {
        internal Action OnClosed;

        private string m_Content = "Default";

        internal static InstallAIWarningWindow OpenNew(string content)
        {
            InstallAIWarningWindow win = CreateInstance<InstallAIWarningWindow>();
            win.m_Content = content;

            Rect p = EditorGUIUtility.GetMainWindowPosition();
            Vector2 popupSize = new(500, 200);
            win.ShowAsDropDown(new Rect(p.center - new Vector2(popupSize.x * 0.5f, 0), popupSize), popupSize);
            win.position =
                new Rect(new Rect(p.center - new Vector2(popupSize.x * 0.5f, popupSize.y * 0.5f), popupSize));

            return win;
        }

        private void OnDestroy()
        {
            OnClosed?.Invoke();
        }

        private void CreateGUI()
        {
            Label label = new();
            label.text = m_Content;

            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 20;

            rootVisualElement.Add(label);
        }
    }
}
