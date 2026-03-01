using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unity.Tutorials.Editor
{
    internal class TableOfContentController : Controller
    {
        private TableOfContentModel m_Model => Application.Model.TableOfContent;
        private TableOfContentView m_View => Application.TableOfContentView;

        internal TableOfContentController()
        {
            SetupCategories();

            AddListener<CategoriesRefreshRequestedEvent>(OnCategoriesRefreshRequested);
            AddListener<CategoryClickedEvent>(OnCategoryClicked);
            AddListener<SectionClickedEvent>(OnSectionClicked);
            AddListener<BackButtonClickedEvent>(OnBackButtonClicked);
            AddListener<TutorialsCompletionStatusUpdatedEvent>(OnTutorialsCompletionStatusUpdated);

            EditorApplication.update += OnEditorUpdate;
        }

        internal override void RemoveListeners()
        {
            RemoveListener<CategoriesRefreshRequestedEvent>(OnCategoriesRefreshRequested);
            RemoveListener<CategoryClickedEvent>(OnCategoryClicked);
            RemoveListener<SectionClickedEvent>(OnSectionClicked);
            RemoveListener<BackButtonClickedEvent>(OnBackButtonClicked);
            RemoveListener<TutorialsCompletionStatusUpdatedEvent>(OnTutorialsCompletionStatusUpdated);

            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnTutorialsCompletionStatusUpdated(TutorialsCompletionStatusUpdatedEvent evt)
        {
            if (Application.CurrentView != m_View.Name)
            {
                return;
            }
            m_View.Refresh();
        }

        private void SetupCategories()
        {
            IEnumerable<TutorialContainer> allCategories = TutorialFrameworkModel.s_AreTestsRunning ? TableOfContentModel.CategoriesOfProjectDuringTests
                                                                         : TutorialEditorUtils.FindAssets<TutorialContainer>();
            IEnumerable<TutorialContainer> rootCategories = allCategories.Where(category => category.ParentContainer is null);

            TutorialContainer defaultCategory = rootCategories.FirstOrDefault();

            /* If we have more than one root container, we show a selection view. Exactly one (or zero) container
            is set active immediately without possibility to return to the the selection view. */
            m_Model.RootCategoriesOfProject = rootCategories;
            if (rootCategories.Count() < 2)
            {
                m_Model.CurrentContainer = defaultCategory;
            }

            foreach (TutorialContainer category in allCategories)
            {
                category.Modified.RemoveListener(OnTutorialCategoryModified);
                category.Modified.AddListener(OnTutorialCategoryModified);
            }

            m_Model.FetchAllTutorialStates();
        }

        private void OnEditorUpdate()
        {
            MaskingManager.OnEditorUpdate();
        }

        private void OnTutorialCategoryModified(TutorialContainer category)
        {
            if (Application == null
            || Application.CurrentView != m_View.Name)
            {
                return;
            }

            if (m_Model.CurrentContainer == category
            || m_Model.CurrentContainer == category.ParentContainer)
            {
                m_View.Refresh();
            }
        }

        private void OnCategoriesRefreshRequested(CategoriesRefreshRequestedEvent evt)
        {
            SetupCategories();
        }

        private void OnCategoryClicked(CategoryClickedEvent evt)
        {
            EnterCategory(evt.Category);
        }

        private void EnterCategory(TutorialContainer category)
        {
            MaskingManager.Unmask();

            if (m_Model.CurrentContainer == category) { return; }
            m_Model.CurrentContainer = category;
            m_View.Refresh();
        }

        private void OnBackButtonClicked(BackButtonClickedEvent evt)
        {
            ExitCategory();
        }

        private void ExitCategory()
        {
            if (m_Model.CurrentContainer && m_Model.CurrentContainer.ParentContainer)
            {
                m_Model.CurrentContainer = m_Model.CurrentContainer.ParentContainer;
            }
            else
            {
                m_Model.CurrentContainer = null;
            }
            m_View.Refresh();
        }

        private void OnSectionClicked(SectionClickedEvent evt)
        {
            if (evt.Section.IsTutorial)
            {
                StartTutorial(evt.Section.Tutorial);
                return;
            }
            evt.Section.OpenUrl();
        }

        private void StartTutorial(Tutorial tutorial)
        {
            Application.Broadcast(new TutorialStartRequestedEvent(tutorial, null));
        }
    }
}
