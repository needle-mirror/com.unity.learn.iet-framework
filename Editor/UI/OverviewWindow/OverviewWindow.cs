#if TUTORIAL_AUTHORING
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// A window to get an at-a-glance view of all TutorialContainers, Tutorials and their Pages present in the project.
    /// The items are sorted in a tree view, and can display warnings to guide the user in fixing potential structural issues.
    /// </summary>
    public class OverviewWindow : EditorWindow
    {
        private const string k_DoubleLinkedTutorial_Tooltip = "The Tutorial is linked inside 2 or more Containers. This might be confusing to the user. Is this intentional?";
        private const string k_WindowName = "Tutorials Overview";

        [SerializeField] private VisualTreeAsset m_Template;
        [SerializeField] private VisualTreeAsset m_TreeItemTemplate;

        [SerializeField] private StyleSheet m_StyleSheetLight;
        [SerializeField] private StyleSheet m_StyleSheetDark;

        private TreeView m_treeView;
        private Button m_collapseContainersButton;
        private Button m_collapseTutorialsButton;
        private List<TreeViewItemData<TutorialTreeItem>> m_treeViewItemData;
        private List<TutorialContainer> m_allContainers;
        private List<TutorialContainer> m_nonRootContainers; // All sub-Containers
        private List<TutorialContainer> m_rootContainers; // All Containers with no parent
        private List<Tutorial> m_tutorialsWithinContainers; // All the Tutorials in the project currently linked in a Container
        private bool m_containersExpanded = true;
        private bool m_tutorialsExpanded = true;
        private bool m_displayTitles = true;
        private bool m_displayAssetNames;
        private bool m_displayWarnings = true;
        private int m_draggedId; // The id of the TreeView item currently being dragged
        private List<Tutorial> m_TutorialsMultiLinking;

        [MenuItem(MenuItems.AuthoringMenuPath + k_WindowName)]
        private static void ShowWindow()
        {
            OverviewWindow wnd = GetWindow<OverviewWindow>();
            Texture2D icon = UIUtils.LoadIcon("TutorialsWindowIcon.png", true, true);
            wnd.titleContent = new GUIContent(k_WindowName, icon);
        }

        private void OnEnable()
        {
            rootVisualElement.styleSheets.Add(EditorGUIUtility.isProSkin ? m_StyleSheetDark : m_StyleSheetLight);
        }

        private void CreateGUI()
        {
            m_Template.CloneTree(rootVisualElement);

            // Buttons
            m_collapseTutorialsButton  = rootVisualElement.Q<Button>("RefreshButton");
            m_collapseTutorialsButton.clicked += OnRefreshClicked;

            m_collapseTutorialsButton  = rootVisualElement.Q<Button>("CollapseTutorialsButton");
            m_collapseTutorialsButton.clicked += OnCollapseTutorialsClicked;

            m_collapseContainersButton  = rootVisualElement.Q<Button>("CollapseContainersButton");
            m_collapseContainersButton.clicked += OnCollapseContainersClicked;

            // Toggles
            Toggle displayTitlesToggle  = rootVisualElement.Q<Toggle>("DisplayTitlesToggle");
            displayTitlesToggle.RegisterValueChangedCallback(OnDisplayTitlesToggled);

            Toggle displayAssetNamesToggle  = rootVisualElement.Q<Toggle>("DisplayAssetNamesToggle");
            displayAssetNamesToggle.RegisterValueChangedCallback(OnDisplayAssetToggled);

            Toggle displayWarningsToggle = rootVisualElement.Q<Toggle>("DisplayWarningsToggle");
            displayWarningsToggle.RegisterValueChangedCallback(OnDisplayWarningsToggled);

            // Tree
            m_treeView = rootVisualElement.Q<TreeView>("ContainersTree");
            m_treeView.makeItem += () => m_TreeItemTemplate.Instantiate();
            m_treeView.bindItem += BindItem;
            m_treeView.selectionChanged += OnSelectionChanged;
            m_treeView.setupDragAndDrop += OnStartDragging;
            m_treeView.dragAndDropUpdate += UpdateDragAndDrop;
            m_treeView.handleDrop += OnDrop;

            PopulateTree();

            return;

            void OnDisplayTitlesToggled(ChangeEvent<bool> evt)
            {
                m_displayTitles = evt.newValue;
                m_treeView.RefreshItems();
            }

            void OnDisplayWarningsToggled(ChangeEvent<bool> evt)
            {
                m_displayWarnings = evt.newValue;
                m_treeView.RefreshItems();
            }

            void OnDisplayAssetToggled(ChangeEvent<bool> evt)
            {
                m_displayAssetNames = evt.newValue;
                m_treeView.RefreshItems();
            }

            void OnSelectionChanged(IEnumerable<object> obj)
            {
                IEnumerable<object> enumerable = obj as object[] ?? obj.ToArray();
                if (!enumerable.Any()) return;

                TutorialTreeItem selected = (TutorialTreeItem)enumerable.First();
                Selection.activeObject = selected.Asset;
                EditorGUIUtility.PingObject(selected.Asset);
            }

            void BindItem(VisualElement element, int elementIndex)
            {
                TutorialTreeItem item = m_treeView.GetItemDataForId<TutorialTreeItem>(m_treeView.GetIdForIndex(elementIndex));

                Label titleLabel = element.Q<Label>("Title");
                UIUtils.ShowOrHide(titleLabel, m_displayTitles);
                if(m_displayTitles) titleLabel.text = item.Title;

                Label assetNameLabel = element.Q<Label>("AssetName");
                UIUtils.ShowOrHide(assetNameLabel, m_displayAssetNames);
                if(m_displayAssetNames) assetNameLabel.text = item.Asset?.name;

                VisualElement iconElement = element.Q<VisualElement>("Icon");
                iconElement.ClearClassList();
                iconElement.AddToClassList(
                    item.Type switch
                    {
                        TreeItemType.Container => "container",
                        TreeItemType.Tutorial => "tutorial",
                        TreeItemType.Page => "tutorialPage",
                        TreeItemType.Fake => "collapse",
                        _ => throw new ArgumentOutOfRangeException()
                    });

                VisualElement issueIcon = element.Q<VisualElement>("IssueIcon");
                UIUtils.ShowOrHide(issueIcon, m_displayWarnings);
                if (m_displayWarnings)
                {
                    issueIcon.EnableInClassList("issue-icon", item.Issue);
                    issueIcon.tooltip = item.Issue ? k_DoubleLinkedTutorial_Tooltip : "";
                }
            }

            StartDragArgs OnStartDragging(SetupDragAndDropArgs args)
            {
                m_draggedId = args.selectedIds.First();

                return new StartDragArgs(args.startDragArgs.title, args.startDragArgs.visualMode);
            }

            DragVisualMode UpdateDragAndDrop(HandleDragAndDropArgs args)
            {
                TutorialTreeItem draggedItem = m_treeView.GetItemDataForId<TutorialTreeItem>(m_draggedId);
                TutorialTreeItem targetItem = m_treeView.GetItemDataForId<TutorialTreeItem>(args.parentId);

                return ValidateDrag(draggedItem, targetItem, args.parentId == -1, false);
            }

            DragVisualMode OnDrop(HandleDragAndDropArgs args)
            {
                bool targetIsRoot = args.parentId == -1;
                TutorialTreeItem draggedItem = m_treeView.GetItemDataForId<TutorialTreeItem>(m_draggedId);
                TutorialTreeItem targetItem = m_treeView.GetItemDataForId<TutorialTreeItem>(args.parentId);

                DragVisualMode result = ValidateDrag(draggedItem, targetItem, targetIsRoot, true);

                if (result != DragVisualMode.Rejected) PopulateTree();
                return result;
            }
        }

        private DragVisualMode ValidateDrag(TutorialTreeItem draggedItem, TutorialTreeItem targetItem, bool targetIsRoot, bool applyAction)
        {
            if(draggedItem.Asset == targetItem.Asset) return DragVisualMode.Rejected; // Dragged onto itself
            if(targetItem.Type == TreeItemType.Fake) return DragVisualMode.Rejected; // Dragged onto fake item (Stray Tutorials)

            switch (draggedItem.Type)
            {
                // Dragged to empty spot - Make root container
                case TreeItemType.Container when targetIsRoot:
                {
                    TutorialContainer draggedContainer = (TutorialContainer)draggedItem.Asset;

                    if (draggedContainer.ParentContainer == null) return DragVisualMode.Rejected; // Already a root Container

                    // Make root Container
                    if(applyAction) draggedContainer.ParentContainer = null;
                    return DragVisualMode.Move;
                }

                // Dragged Container onto another Container
                case TreeItemType.Container when targetItem.Asset.GetType() == typeof(TutorialContainer):
                {
                    TutorialContainer draggedContainer = (TutorialContainer)draggedItem.Asset;
                    TutorialContainer targetContainer = (TutorialContainer)targetItem.Asset;

                    if (draggedContainer.ParentContainer == targetContainer) return DragVisualMode.Rejected; // Already parented
                    if (targetContainer.ParentContainer == draggedContainer) return DragVisualMode.Rejected; // Cyclical reference

                    if(applyAction) draggedContainer.ParentContainer = targetContainer;
                    return DragVisualMode.Move;
                }

                // Add Tutorial to Container
                case TreeItemType.Tutorial when targetItem.Asset.GetType() == typeof(TutorialContainer):
                {
                    Tutorial draggedTutorial = (Tutorial)draggedItem.Asset;
                    TutorialContainer targetContainer = (TutorialContainer)targetItem.Asset;

                    if(targetContainer.Sections.Any(section => section.Tutorial ==  draggedTutorial)) return DragVisualMode.Rejected; // Already a section

                    if (applyAction)
                    {
                        TutorialContainer.Section newSection = null;

                        // Find and remove Tutorial from its original Container
                        foreach (TutorialContainer tutorialContainer in m_allContainers)
                        {
                            foreach (TutorialContainer.Section originalSection in tutorialContainer.Sections)
                            {
                                if (originalSection.Tutorial != draggedTutorial) continue;

                                newSection = originalSection;

                                List<TutorialContainer.Section> list = tutorialContainer.Sections.ToList();
                                list.Remove(originalSection);
                                tutorialContainer.Sections = list.ToArray();
                                break;
                            }
                        }

                        // Add Tutorial to new Container
                        newSection ??= new TutorialContainer.Section { Heading = draggedTutorial.TutorialTitle.Value, Tutorial = draggedTutorial };
                        List<TutorialContainer.Section> sections = targetContainer.Sections.ToList();
                        sections.Add(newSection);
                        targetContainer.Sections = sections.ToArray();
                    }
                    return DragVisualMode.Move;
                }

                case TreeItemType.Page:
                {
                    return DragVisualMode.Move;
                }

                default:
                {
                    return DragVisualMode.Rejected;
                }
            }
        }

        private void OnRefreshClicked() => PopulateTree();

        private void OnCollapseContainersClicked()
        {
            m_containersExpanded = !m_containersExpanded;
            m_tutorialsExpanded = false;
            CollapseExpandItems();
            UpdateExpandButtons();
        }

        private void OnCollapseTutorialsClicked()
        {
            m_tutorialsExpanded = !m_tutorialsExpanded;
            CollapseExpandItems();
            UpdateExpandButtons();
        }

        private void CollapseExpandItems()
        {
            for (int i = 0; i < m_treeView.viewController.GetTreeItemsCount(); i++)
            {
                TutorialTreeItem item = m_treeView.GetItemDataForIndex<TutorialTreeItem>(i);

                bool expand = item.Type switch
                {
                    TreeItemType.Container or TreeItemType.Fake => m_containersExpanded,
                    TreeItemType.Tutorial => m_tutorialsExpanded,
                    TreeItemType.Page => false,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if(expand) m_treeView.viewController.ExpandItemByIndex(i, false);
                else m_treeView.viewController.CollapseItemByIndex(i, false);
            }
        }

        private void UpdateExpandButtons()
        {
            string t = m_containersExpanded ? "Collapse" : "Expand";
            m_collapseContainersButton.text = $"{t} Containers";

            t = m_tutorialsExpanded ? "Collapse" : "Expand";
            m_collapseTutorialsButton.text = $"{t} Tutorials";
        }

        private void PopulateTree()
        {
            m_treeViewItemData = new List<TreeViewItemData<TutorialTreeItem>>();
            m_allContainers = AssetDatabase.FindAssets($"t:{nameof(TutorialContainer)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TutorialContainer>(AssetDatabase.GUIDToAssetPath(guid))).ToList();

            m_rootContainers = m_allContainers.Where(container => container.ParentContainer == null).ToList();
            m_nonRootContainers = m_allContainers.Where(container => container.ParentContainer != null).ToList();

            // So we can later determine which Tutorials are stray, or multi-linked to a Container
            m_tutorialsWithinContainers = new List<Tutorial>();
            foreach (TutorialContainer container in m_allContainers)
            {
                foreach (TutorialContainer.Section section in container.Sections)
                {
                    if(SectionContainsTutorial(section)) m_tutorialsWithinContainers.Add(section.Tutorial);
                }
            }

            // Find Tutorials linked in multiple Containers, to show issue icon
            m_TutorialsMultiLinking = m_tutorialsWithinContainers.GroupBy(tutorial => tutorial)
                .Where(grouping => grouping.Count() > 1)
                .Select(grouping => grouping.Key).ToList();

            int nextId = 0;
            for (int i = 0; i < m_rootContainers.Count; i++)
            {
                DoContainer(m_rootContainers[i], m_treeViewItemData, true);
            }

            // Stray Tutorials
            IEnumerable<Tutorial> strayTutorials = AssetDatabase.FindAssets($"t:{nameof(Tutorial)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Tutorial>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(tutorial => !m_tutorialsWithinContainers.Contains(tutorial));

            List<Tutorial> strayTutorialsList = strayTutorials.ToList();
            if (strayTutorialsList.Count > 0)
            {
                TutorialTreeItem strayTutorialsItem = new("Stray Tutorials", null, TreeItemType.Fake, true);
                List<TreeViewItemData<TutorialTreeItem>> childStrays = new();
                m_treeViewItemData.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, strayTutorialsItem, childStrays));

                foreach (Tutorial tutorial in strayTutorialsList)
                {
                    List<TreeViewItemData<TutorialTreeItem>> pageItems = new();
                    TutorialTreeItem strayTutorialItem = new(tutorial.TutorialTitle.Value, tutorial, TreeItemType.Tutorial);
                    childStrays.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, strayTutorialItem, pageItems));

                    // Pages of Stray Tutorials
                    foreach (TutorialPage tutorialPage in tutorial.PagesCollection)
                    {
                        TutorialTreeItem pageItem = new(tutorialPage.Title.Value, tutorialPage, TreeItemType.Page);
                        pageItems.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, pageItem));
                    }
                }
            }

            m_treeView.SetRootItems(m_treeViewItemData);
            m_treeView.RefreshItems();
            m_treeView.ClearSelection();

            SetupElementExpansion(); // This way Tutorials start collapsed
            return;

            void DoContainer(TutorialContainer container, List<TreeViewItemData<TutorialTreeItem>> dataItems, bool rootContainer = false)
            {
                string label = $"{container.Title.Value}";
                TutorialTreeItem containerItem = new(label, container, TreeItemType.Container, rootContainer);
                List<TreeViewItemData<TutorialTreeItem>> childItems = new();
                dataItems.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, containerItem, childItems));

                foreach (TutorialContainer childContainer in
                         m_nonRootContainers.Where(c => c.ParentContainer == container).OrderBy(c => c.OrderInParent))
                {
                    DoContainer(childContainer, childItems);
                }

                List<Tutorial> tutorials = container.Sections.Where(SectionContainsTutorial).Select(section => section.Tutorial).ToList();
                foreach (Tutorial tutorial in tutorials)
                {
                    // Tutorial is added to two (or more) Containers
                    bool isMultiLinked = m_TutorialsMultiLinking.Contains(tutorial);

                    List<TreeViewItemData<TutorialTreeItem>> pageItems = new();
                    label = $"{tutorial.TutorialTitle.Value}";
                    TutorialTreeItem tutorialItem = new(label, tutorial, TreeItemType.Tutorial, false, isMultiLinked);
                    childItems.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, tutorialItem, pageItems));

                    // Pages
                    foreach (TutorialPage tutorialPage in tutorial.PagesCollection)
                    {
                        TutorialTreeItem pageItem = new(tutorialPage.Title.Value, tutorialPage, TreeItemType.Page);
                        pageItems.Add(new TreeViewItemData<TutorialTreeItem>(nextId++, pageItem));
                    }
                }
            }

            // TODO: Move inside the Section class?
            bool SectionContainsTutorial(TutorialContainer.Section section)
            {
                return section.Tutorial != null &&  section.Url.IsNullOrEmpty();
            }
        }

        private void SetupElementExpansion()
        {
            m_tutorialsExpanded = true;
            m_containersExpanded = true;

            OnCollapseTutorialsClicked();
        }

        private enum TreeItemType
        {
            Container,
            Tutorial,
            Page,
            Fake
        }

        private struct TutorialTreeItem
        {
            public string Title;
            public Object Asset;
            public TreeItemType Type;
            public bool IsRoot; // Currently added correctly when scanning assets, but not utilised - also not updated when drag/dropping
            public bool Issue; // Used to signal that there's an issue. For Tutorials, it means the Tutorial is linked into 2 or more Containers

            public TutorialTreeItem(string title, Object asset, TreeItemType type, bool isRoot = false, bool issue = false)
            {
                Title = title;
                Asset = asset;
                Type = type;
                IsRoot = isRoot;
                Issue = issue;
            }
        }
    }
}
#endif
