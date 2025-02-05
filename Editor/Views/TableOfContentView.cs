using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine.UIElements;
using static Unity.Tutorials.Core.Editor.TutorialContainer;

namespace Unity.Tutorials.Core.Editor
{
    internal class TableOfContentView : View
    {
        internal const string k_Name = "TableOfContent";
        internal override string Name => k_Name;
        VisualElement m_Root;
        VisualElement m_TutorialsContainer;

        internal int CategoriesOrTutorialsCurrentlyVisibile => m_TutorialsContainer.childCount;

        TableOfContentModel Model => Application.Model.TableOfContent;
        List<Tuple<VisualElement, Section>> m_ActiveSections;
        bool m_SectionsInitialized = false;
        EditorCoroutine m_CheckmarksUpdateRoutine;

        private EditorCoroutine m_CategoryStateLoadingRoutine;
        private bool m_CategoriesInitialized;
        List<Tuple<VisualElement, TutorialContainer>> m_CategoryAwaitingStateUpdate;

        public TableOfContentView() : base() { }

        internal void Initialize(VisualElement root)
        {
            m_Root = root;
            m_TutorialsContainer = m_Root.Q("lstTutorials");
            m_TutorialsContainer.style.alignItems = Align.Center;
            m_ActiveSections = new List<Tuple<VisualElement, Section>>();
            Refresh();
        }

        internal void Refresh()
        {
            m_TutorialsContainer.Clear();
            LoadHeader();
            LoadCategories();
            LoadTutorialsAndLinks();
        }

        void GoBackInContainerHierachy()
        {
            Application.Broadcast(new BackButtonClickedEvent());
        }

        void LoadHeader()
        {
            VisualElement imgTitleHeader = m_Root.Q("imgTitleHeader");
            TutorialContainer currentCategory = Model.CurrentCategory;
            string subtitle = string.Empty;
            string title = string.Empty;

            if (currentCategory)
            {
                subtitle = currentCategory.Subtitle.Value;
                title = currentCategory.Title.Value;
                imgTitleHeader.style.backgroundImage = currentCategory.BackgroundImage;
            }
            else
            {
                title = Localization.Tr(LocalizationKeys.k_TOCLabelTitle);
                subtitle = Localization.Tr(LocalizationKeys.k_TOCLabelSubtitle);
                imgTitleHeader.style.backgroundImage = null;
            }

            UIElementsUtils.SetupLabel("lblTitle", title, imgTitleHeader, false);
            UIElementsUtils.SetupLabel("lblSubtitle", subtitle, imgTitleHeader, false);
            bool enableBackButton = Model.CurrentCategory && (Model.CurrentCategory.ParentContainer || Model.RootCategoriesOfProject.Count() > 1);

            if(enableBackButton)
                UIElementsUtils.SetupButton("btnExitCategory", GoBackInContainerHierachy, enableBackButton, imgTitleHeader, string.Empty, Localization.Tr(LocalizationKeys.k_TOCButtonBackTooltip));
            else
                UIElementsUtils.Hide("btnExitCategory", imgTitleHeader);
        }

        void LoadCategories()
        {
            IEnumerable<TutorialContainer> categoriesToLoad = Model.CurrentCategory == null ? Model.RootCategoriesOfProject
                                                                                            : Model.CurrentCategory.FindSubCategories();

            if (categoriesToLoad == null) { return; }

            //sorting category by order in view
            categoriesToLoad = categoriesToLoad.OrderBy(container => container.OrderInView);

            m_CategoriesInitialized = false;
            m_CategoryAwaitingStateUpdate = new();
            Application.StopAndNullifyEditorCoroutine(ref m_CategoryStateLoadingRoutine);
            m_CategoryStateLoadingRoutine = EditorCoroutineUtility.StartCoroutine(UpdateTutorialsStateFetched(), Application);

            VisualTreeAsset tutorialCategoryUIPrefab = UIElementsUtils.LoadUXML("TutorialCategoryUI");
            VisualElement categoryUI;
            foreach (var category in categoriesToLoad)
            {
                categoryUI = tutorialCategoryUIPrefab.CloneTree();
                SetupCategoryUI(categoryUI, category);
                m_TutorialsContainer.Add(categoryUI);
            }

            m_CategoriesInitialized = true;
        }

        internal void SetupSectionUI(VisualElement sectionUI, Section data)
        {
            UIElementsUtils.SetupLabel("lblName", data.Heading, sectionUI, false);
            UIElementsUtils.SetupLabel("lblDescription", data.Text, sectionUI, false);

            UIElementsUtils.ShowOrHide("imgLink", sectionUI, !string.IsNullOrEmpty(data.Url));

            if (data.Image != null)
            {
                sectionUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.Image);
            }

            sectionUI.UnregisterCallback<MouseUpEvent, Section>(OnSectionClicked);

            UIElementsUtils.ShowOrHide("imgErrorCheckmark", sectionUI, !data.IsConfiguredCorrectly);

            if (data.IsConfiguredCorrectly)
            {
                sectionUI.tooltip = Localization.Tr(LocalizationKeys.k_TutorialSectionTooltip) + data.Text;
                if (data.IsTutorial)
                {
                    UIElementsUtils.Show("lblCompletionStatus", sectionUI);
                    UpdateCheckmark(sectionUI, data);
                }
                else
                {
                    UIElementsUtils.Hide("lblCompletionStatus", sectionUI);
                    UIElementsUtils.Hide("imgCheckmark", sectionUI);
                }
                sectionUI.RegisterCallback<MouseUpEvent, Section>(OnSectionClicked, data);
                return;
            }
            sectionUI.tooltip = Localization.Tr(LocalizationKeys.k_TutorialLabelParseError);
            UIElementsUtils.Hide("lblCompletionStatus", sectionUI);
            UIElementsUtils.Hide("imgCheckmark", sectionUI);
        }

        internal void SetupCategoryUI(VisualElement categoryUI, TutorialContainer data)
        {
            UIElementsUtils.SetupLabel("lblName", data.Title, categoryUI, false);
            UIElementsUtils.SetupLabel("lblDescription", data.Subtitle, categoryUI, false);

            InitCompletionUI(categoryUI, data);
            m_CategoryAwaitingStateUpdate.Add(new Tuple<VisualElement, TutorialContainer>(categoryUI, data));

            categoryUI.tooltip = data.Description;
            if (data.BackgroundImage != null)
            {
                categoryUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.BackgroundImage);
            }
            categoryUI.RegisterCallback((MouseUpEvent evt) => OnCategoryClicked(evt, data));
        }

        void InitCompletionUI(VisualElement categoryUI, TutorialContainer container)
        {
            var label = categoryUI.Q<Label>("CategoryCompletionLabel");
            var bar = categoryUI.Q<VisualElement>("CategoryCompletionBar");

            bar.style.width = 0;
            label.text = "Completion Loading...";
            UIElementsUtils.Hide("Checkmark", categoryUI);
        }

        void UpdateCompletionUI(VisualElement categoryUI, TutorialContainer container)
        {
            var label = categoryUI.Q<Label>("CategoryCompletionLabel");
            var bar = categoryUI.Q<VisualElement>("CategoryCompletionBar");

            float completion = container.GetCompletionRate();
            int completionPercent = UnityEngine.Mathf.RoundToInt(completion * 100);

            if (completionPercent == 100)
            {
                label.text = "COMPLETED";
                UIElementsUtils.Show("Checkmark", categoryUI);
            }
            else
            {
                UIElementsUtils.Hide("Checkmark", categoryUI);
                label.text = string.Format($"Completion : {completionPercent}%");
            }

            bar.style.width = Length.Percent(completionPercent);
        }

        void OnSectionClicked(MouseUpEvent evt, Section section)
        {
            Application.Broadcast(new SectionClickedEvent(section));
        }

        void OnCategoryClicked(MouseUpEvent evt, TutorialContainer category)
        {
            Application.Broadcast(new CategoryClickedEvent(category));
        }

        void UpdateCheckmark(VisualElement sectionUI, Section data)
        {
            bool progressTracking = (data.Tutorial != null && data.Tutorial.ProgressTrackingEnabled);
            bool completed = progressTracking && data.Tutorial.CompletedByUser;

            UIElementsUtils.SetupLabel("lblCompletionStatus", completed ? Localization.Tr(LocalizationKeys.k_TOCLabelCompleted) : string.Empty, sectionUI, false);
            VisualElement tutorialCheckmark = sectionUI.Q("imgCheckmark");
            if (completed)
            {
                UIElementsUtils.Show(tutorialCheckmark);
            }
            else
            {
                UIElementsUtils.Hide(tutorialCheckmark);
            }
        }

        void LoadTutorialsAndLinks()
        {
            m_ActiveSections.Clear();
            m_SectionsInitialized = false;
            IEnumerable<Section> sectionsToLoad;
            if (Model.CurrentCategory == null)
            {
                if (Model.RootCategoriesOfProject.Count() > 1)
                {
                    return; //nothing to load as we're in the 1st screen of the Table of Content
                }
                sectionsToLoad = Model.RootCategoriesOfProject
                                      .OrderBy(rootCategory => rootCategory.OrderInView)
                                      .SelectMany(rootCategory => rootCategory.Sections);
            }
            else
            {
                sectionsToLoad = Model.CurrentCategory.Sections;
            }

            if (sectionsToLoad == null)
            {
                m_SectionsInitialized = true;
                return;
            }

            if (sectionsToLoad.Any(section => section.Tutorial?.ProgressTrackingEnabled ?? false))
            {
                foreach (var section in sectionsToLoad)
                {
                    section.LoadState();
                }
                Application.StopAndNullifyEditorCoroutine(ref m_CheckmarksUpdateRoutine);
                m_CheckmarksUpdateRoutine = EditorCoroutineUtility.StartCoroutine(UpdateCheckmarksWhenStatesFetched(), Application);
            }

            VisualTreeAsset sectionUIPrefab = UIElementsUtils.LoadUXML("SectionUI");
            VisualElement sectionUI;
            foreach (var section in sectionsToLoad)
            {
                sectionUI = sectionUIPrefab.CloneTree();
                SetupSectionUI(sectionUI, section);
                m_TutorialsContainer.Add(sectionUI);
                m_ActiveSections.Add(new Tuple<VisualElement, Section>(sectionUI, section));
            }

            m_SectionsInitialized = true;
        }
        IEnumerator UpdateCheckmarksWhenStatesFetched()
        {
            while (!m_SectionsInitialized || !Model.FetchedTutorialStates)
            {
                yield return null;
            }

            foreach (var sectionUIAndData in m_ActiveSections)
            {
                if (sectionUIAndData.Item2.IsConfiguredCorrectly)
                {
                    UpdateCheckmark(sectionUIAndData.Item1, sectionUIAndData.Item2);
                }
            }
        }

        IEnumerator UpdateTutorialsStateFetched()
        {
            // Model.FetchedTutorialStates will be set to true by the model once all state have been fetched. As this
            // potentially fetch online data, we need to wait until the answer is there
            while (Application != null && (!m_CategoriesInitialized || !Model.FetchedTutorialStates))
            {
                yield return null;
            }


            foreach (var uiAndData in m_CategoryAwaitingStateUpdate)
            {
                UpdateCompletionUI(uiAndData.Item1, uiAndData.Item2);
            }
        }
    }
}
