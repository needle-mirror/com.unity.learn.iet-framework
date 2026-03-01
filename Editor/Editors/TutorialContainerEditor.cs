using System.Collections.Generic;
using System.Linq;
using Unity.Tutorials.Editor.CustomControl;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomEditor(typeof(TutorialContainer))]
    internal class TutorialContainerEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private readonly string[] k_PropertiesToHide =
        {
            "m_Script",
            nameof(TutorialContainer.Modified)  // this is not not something tutorial authors should subscribe to typically
        };

        private TutorialContainer Target => (TutorialContainer)target;

        private void OnEnable()
        {
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            Target.RaiseModified();
            /* If this category is parented, we consider modifications to 'this'
            category also to be modifications of the parent. */
            if (Target.ParentContainer != null)
            {
                Target.ParentContainer.RaiseModified();
            }
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            UndoPropertyModification previousCategoryModification = modifications
                .FirstOrDefault(m => m.previousValue.propertyPath == nameof(Target.ParentContainer));

            bool parentCategoryWasEdited = previousCategoryModification.previousValue != null;
            if (parentCategoryWasEdited)
            {
                /* If this category was parented, we consider modifications to 'this'
                category also to be modifications of the parent. */
                TutorialContainer previousCategory = previousCategoryModification.previousValue.objectReference as TutorialContainer;
                if (previousCategory != null)
                {
                    previousCategory.RaiseModified();
                }
            }

            Target.RaiseModified();
            /* If this category is parented, we consider modifications to 'this'
            category also to be modifications of the parent. */
            if (Target.ParentContainer != null)
            {
                Target.ParentContainer.RaiseModified();
            }
            return modifications;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.styleSheets.Add(m_Stylesheet);

            // TODO : Update if related setting changes while the inspector is open.
            TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(root);

            UIUtils.DrawInspectorExcluding(root, serializedObject, this, k_PropertiesToHide);

            // Display all linked sub-containers (i.e. the Containers ones referencing this as their Parent Container)
            List<TutorialContainer> subContainers = AssetDatabase.FindAssets($"t:{nameof(TutorialContainer)}")
                            .Select(asset => AssetDatabase.LoadAssetAtPath<TutorialContainer>(AssetDatabase.GUIDToAssetPath(asset)))
                            .Where(c => c.ParentContainer == Target).OrderBy(c => c.OrderInParent).ToList();

            // Inspector Tweaks ---

            // Order in view
            PropertyField orderField = root.Q<PropertyField>($"PropertyField:{nameof(TutorialContainer.OrderInParent)}");
            orderField.AddToClassList("indented-property");
            orderField.SetEnabled(Target.ParentContainer != null);

            // Sections
            PropertyField sectionsField = root.Q<PropertyField>($"PropertyField:{nameof(TutorialContainer.Sections)}");
            sectionsField.AddToClassList("inspector-list");
            sectionsField.AddToClassList("foldout-bold-title");

            // Sub containers (as a list, or as a "No Sub Containers" message)
            string subTutsLabel = "Sub-Containers";
            string noTutsMessage = "No Sub-Containers";
            string labelTooltip =
                "The sub-containers to this Tutorial Container, found in the project. " +
                "Sub-containers are identified as such because they specify this Tutorial Container as their parent.";
            string messageTooltip =
                "No Tutorial Container in the project references this as their parent.";
            if (subContainers.Count > 0)
            {
                ListView subContainersList = new(subContainers)
                {
                    viewDataKey = "SubContainersList",
                    tooltip = labelTooltip,
                    selectionType = SelectionType.None,
                    showBorder = true,
                    reorderable = false,
                    horizontalScrollingEnabled = false,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                    virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                    fixedItemHeight = 20,
                    showBoundCollectionSize = false,
                    showFoldoutHeader = true,
                    headerTitle = subTutsLabel,
                    showAddRemoveFooter = false,
                    reorderMode = ListViewReorderMode.Animated,
                    makeItem = () => new ObjectField
                    {
                        objectType = typeof(TutorialContainer)
                    },
                    bindItem = (element, index) =>
                    {
                        ObjectField objectField = (ObjectField)element;
                        objectField.value = subContainers[index];
                        string title = subContainers[index].Title.Value;
                        objectField.AddToClassList("unity-base-field__aligned");
                        objectField.label = string.IsNullOrEmpty(title) ? "No title" : title;
                        objectField[1].SetEnabled(false);
                    },
                };
                subContainersList.AddToClassList("inspector-list");
                subContainersList.AddToClassList("foldout-bold-title");

                ScrollView scrollView = subContainersList.Q<ScrollView>();
                scrollView.AddToClassList("unity-list-view__scroll-view--with-footer"); // makes it look like a ScrollView that have the +/- button

                root.Add(subContainersList);
                subContainersList.PlaceBehind(sectionsField);
            }

            else
            {
                DoubleLabel doubleLabel = new(subTutsLabel, noTutsMessage,
                                                labelTooltip, messageTooltip);
                root.Add(doubleLabel);
                doubleLabel.PlaceBehind(sectionsField);
            }

            // FAQs
            PropertyField faqEntriesField = root.Q<PropertyField>($"PropertyField:FaqEntries");
            faqEntriesField.label = "FAQ Entries";
            faqEntriesField.viewDataKey = "TutorialContainerFaqEntriesFoldout";
            faqEntriesField.AddToClassList("inspector-list");
            faqEntriesField.AddToClassList("foldout-bold-title");

            return root;
        }
    }
}
