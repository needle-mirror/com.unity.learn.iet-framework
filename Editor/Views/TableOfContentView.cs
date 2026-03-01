using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Tutorials.Editor.TutorialContainer;

namespace Unity.Tutorials.Editor
{
    internal class TableOfContentView : View
    {
        internal const string k_Name = "TableOfContent";
        internal override string Name => k_Name;
        private VisualElement m_Root;
        private VisualElement m_TutorialsContainer;

        internal int CategoriesOrTutorialsCurrentlyVisibile => m_TutorialsContainer.childCount;

        private TableOfContentModel Model => Application.Model.TableOfContent;
        private List<Tuple<VisualElement, Section>> m_ActiveSections;
        private bool m_SectionsInitialized;
        private EditorCoroutine m_CheckmarksUpdateRoutine;

        private EditorCoroutine m_CategoryStateLoadingRoutine;
        private bool m_CategoriesInitialized;
        private List<Tuple<VisualElement, TutorialContainer>> m_CategoryAwaitingStateUpdate;

        internal void Initialize(VisualElement root)
        {
            m_Root = root;
            m_TutorialsContainer = m_Root.Q("TutorialsList");
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

        private void GoBackInContainerHierachy()
        {
            Application.Broadcast(new BackButtonClickedEvent());
        }

        private void LoadHeader()
        {
            VisualElement imgTitleHeader = m_Root.Q("imgTitleHeader");
            TutorialContainer currentCategory = Model.CurrentContainer;
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

            UIUtils.SetupLabel("ContainerTitle", title, imgTitleHeader, false);
            UIUtils.SetupLabel("ContainerSubtitle", subtitle, imgTitleHeader, false);
            bool enableBackButton = Model.CurrentContainer && (Model.CurrentContainer.ParentContainer || Model.RootCategoriesOfProject.Count() > 1);

            if(enableBackButton)
                UIUtils.SetupButton("ButtonExitCategory", GoBackInContainerHierachy, enableBackButton, imgTitleHeader, string.Empty, Localization.Tr(LocalizationKeys.k_TOCButtonBackTooltip));
            else
                UIUtils.Hide("ButtonExitCategory", imgTitleHeader);
        }

        private void LoadCategories()
        {
            IEnumerable<TutorialContainer> categoriesToLoad = Model.CurrentContainer == null ? Model.RootCategoriesOfProject
                                                                                            : Model.CurrentContainer.FindSubCategories();

            if (categoriesToLoad == null) { return; }

            //sorting category by order in view
            categoriesToLoad = categoriesToLoad.OrderBy(container => container.OrderInParent);

            m_CategoriesInitialized = false;
            m_CategoryAwaitingStateUpdate = new();
            Application.StopAndNullifyEditorCoroutine(ref m_CategoryStateLoadingRoutine);
            m_CategoryStateLoadingRoutine = EditorCoroutineUtility.StartCoroutine(UpdateTutorialsStateFetched(), Application);

            VisualTreeAsset tutorialCategoryUIPrefab = UIUtils.LoadUXML("TutorialCategoryUI");
            VisualElement categoryUI;
            foreach (TutorialContainer category in categoriesToLoad)
            {
                categoryUI = tutorialCategoryUIPrefab.CloneTree();
                SetupCategoryUI(categoryUI, category);
                m_TutorialsContainer.Add(categoryUI);
            }

            m_CategoriesInitialized = true;
        }

        internal void SetupSectionUI(VisualElement sectionUI, Section data)
        {
            UIUtils.SetupLabel("lblName", data.Heading, sectionUI, false);
            UIUtils.SetupLabel("lblDescription", data.Text, sectionUI, false);

            UIUtils.ShowOrHide("imgLink", sectionUI, !string.IsNullOrEmpty(data.Url));

            if (data.Image != null)
            {
                sectionUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.Image);
            }

            sectionUI.UnregisterCallback<MouseUpEvent, Section>(OnSectionClicked);

            UIUtils.ShowOrHide("imgErrorCheckmark", sectionUI, !data.IsConfiguredCorrectly);

            if (data.IsConfiguredCorrectly)
            {
                if (data.IsTutorial)
                {
                    UIUtils.Show("lblCompletionStatus", sectionUI);
                    UpdateCheckmark(sectionUI, data);
                }
                else
                {
                    UIUtils.Hide("lblCompletionStatus", sectionUI);
                    UIUtils.Hide("imgCheckmark", sectionUI);
                }
                sectionUI.RegisterCallback<MouseUpEvent, Section>(OnSectionClicked, data);
                return;
            }
            sectionUI.tooltip = Localization.Tr(LocalizationKeys.k_TutorialLabelParseError);
            UIUtils.Hide("lblCompletionStatus", sectionUI);
            UIUtils.Hide("imgCheckmark", sectionUI);
        }

        internal void SetupCategoryUI(VisualElement categoryUI, TutorialContainer data)
        {
            UIUtils.SetupLabel("lblName", data.Title, categoryUI, false);
            UIUtils.SetupLabel("lblDescription", data.Subtitle, categoryUI, false);

            InitCompletionUI(categoryUI, data);
            m_CategoryAwaitingStateUpdate.Add(new Tuple<VisualElement, TutorialContainer>(categoryUI, data));

            if (data.BackgroundImage != null)
            {
                categoryUI.Q("TutorialImage").style.backgroundImage = Background.FromTexture2D(data.BackgroundImage);
            }
            categoryUI.RegisterCallback((MouseUpEvent evt) => OnCategoryClicked(evt, data));
        }

        private void InitCompletionUI(VisualElement categoryUI, TutorialContainer container)
        {
            Label label = categoryUI.Q<Label>("CategoryCompletionLabel");
            VisualElement bar = categoryUI.Q<VisualElement>("CategoryCompletionBar");

            bar.style.width = 0;
            label.text = "Completion Loading...";
            UIUtils.Hide("Checkmark", categoryUI);
        }

        private void UpdateCompletionUI(VisualElement categoryUI, TutorialContainer container)
        {
            Label label = categoryUI.Q<Label>("CategoryCompletionLabel");
            VisualElement bar = categoryUI.Q<VisualElement>("CategoryCompletionBar");

            float completion = container.GetCompletionRate();
            int completionPercent = Mathf.RoundToInt(completion * 100);

            if (completionPercent == 100)
            {
                label.text = "COMPLETED";
                UIUtils.Show("Checkmark", categoryUI);
            }
            else
            {
                UIUtils.Hide("Checkmark", categoryUI);
                label.text = string.Format($"Completion : {completionPercent}%");
            }

            bar.style.width = Length.Percent(completionPercent);
        }

        private void OnSectionClicked(MouseUpEvent evt, Section section)
        {
            Application.Broadcast(new SectionClickedEvent(section));
        }

        private void OnCategoryClicked(MouseUpEvent evt, TutorialContainer category)
        {
            Application.Broadcast(new CategoryClickedEvent(category));
        }

        private void UpdateCheckmark(VisualElement sectionUI, Section data)
        {
            bool progressTracking = (data.Tutorial != null && data.Tutorial.ProgressTrackingEnabled);
            bool completed = progressTracking && data.Tutorial.CompletedByUser;

            UIUtils.SetupLabel("lblCompletionStatus", completed ? Localization.Tr(LocalizationKeys.k_TOCLabelCompleted) : string.Empty, sectionUI, false);
            VisualElement tutorialCheckmark = sectionUI.Q("imgCheckmark");
            if (completed)
            {
                UIUtils.Show(tutorialCheckmark);
            }
            else
            {
                UIUtils.Hide(tutorialCheckmark);
            }
        }

        private void LoadTutorialsAndLinks()
        {
            m_ActiveSections.Clear();
            m_SectionsInitialized = false;
            IEnumerable<Section> sectionsToLoad;
            if (Model.CurrentContainer == null)
            {
                if (Model.RootCategoriesOfProject.Count() > 1)
                {
                    return; //nothing to load as we're in the 1st screen of the Table of Content
                }
                sectionsToLoad = Model.RootCategoriesOfProject
                                      .OrderBy(rootCategory => rootCategory.OrderInParent)
                                      .SelectMany(rootCategory => rootCategory.Sections);
            }
            else
            {
                sectionsToLoad = Model.CurrentContainer.Sections;
            }

            if (sectionsToLoad == null)
            {
                m_SectionsInitialized = true;
                return;
            }

            if (sectionsToLoad.Any(section => section.Tutorial?.ProgressTrackingEnabled ?? false))
            {
                foreach (Section section in sectionsToLoad)
                {
                    section.LoadState();
                }
                Application.StopAndNullifyEditorCoroutine(ref m_CheckmarksUpdateRoutine);
                m_CheckmarksUpdateRoutine = EditorCoroutineUtility.StartCoroutine(UpdateCheckmarksWhenStatesFetched(), Application);
            }

            VisualTreeAsset sectionUIPrefab = UIUtils.LoadUXML("SectionUI");
            VisualElement sectionUI;
            foreach (Section section in sectionsToLoad)
            {
                sectionUI = sectionUIPrefab.CloneTree();
                SetupSectionUI(sectionUI, section);
                m_TutorialsContainer.Add(sectionUI);
                m_ActiveSections.Add(new Tuple<VisualElement, Section>(sectionUI, section));
            }

            m_SectionsInitialized = true;
        }

        private IEnumerator UpdateCheckmarksWhenStatesFetched()
        {
            while (!m_SectionsInitialized || !Model.FetchedTutorialStates)
            {
                yield return null;
            }

            foreach (Tuple<VisualElement, Section> sectionUIAndData in m_ActiveSections)
            {
                if (sectionUIAndData.Item2.IsConfiguredCorrectly)
                {
                    UpdateCheckmark(sectionUIAndData.Item1, sectionUIAndData.Item2);
                }
            }
        }

        private IEnumerator UpdateTutorialsStateFetched()
        {
            // Model.FetchedTutorialStates will be set to true by the model once all state have been fetched. As this
            // potentially fetch online data, we need to wait until the answer is there
            while (Application != null && (!m_CategoriesInitialized || !Model.FetchedTutorialStates))
            {
                yield return null;
            }


            foreach (Tuple<VisualElement, TutorialContainer> uiAndData in m_CategoryAwaitingStateUpdate)
            {
                UpdateCompletionUI(uiAndData.Item1, uiAndData.Item2);
            }
        }
    }
}
