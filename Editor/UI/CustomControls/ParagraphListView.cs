using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.CustomControl
{
    /// <summary>
    /// A ListView tweaked specifically to display <see cref="ParagraphBase"/> (and inheriting classes).
    /// Intended to be used within a <see cref="TutorialPage"/> Inspector.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor.UI.CustomControl", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class ParagraphListView : ListView
    {
        private readonly TutorialPage m_Page;
        private readonly GenericMenu m_DropdownMenu;

        /// <summary>
        /// Constructor for ParagraphListView.
        /// </summary>
        /// <param name="page">The page in which this paragraph is supposed to be displayed.</param>
        public ParagraphListView(TutorialPage page)
        {
            m_Page = page;

            headerTitle = "Paragraphs";
            showFoldoutHeader = true;
            reorderable = true;
            reorderMode = ListViewReorderMode.Animated;
            selectionType = SelectionType.Multiple;
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            showAddRemoveFooter = true;
            showBoundCollectionSize = false;
            showBorder = true;
            showAlternatingRowBackgrounds = AlternatingRowBackground.None;

            AddToClassList("inspector-list");
            AddToClassList("foldout-bold-title");

            overridingAddButtonBehavior += AddItem;
            onRemove = RemoveSelected;
            makeItem += MakeItem;
            bindItem += BindItem;
            unbindItem += UnbindItem;

            // Build dropdown menu for [+] button
            IEnumerable<Type> listOfParagraphTypes = TypeCache.GetTypesDerivedFrom<ParagraphBase>()
                .Where(type => !type.IsAbstract);

            m_DropdownMenu = new GenericMenu();
            foreach (Type t in listOfParagraphTypes)
            {
                m_DropdownMenu.AddItem(new GUIContent(NicifyParagraphName(t.Name)), false, _ => NewParagraph(t), null);
            }
        }

        private static string NicifyParagraphName(string originalName)
        {
            if (originalName.Length > "Paragraph".Length && originalName.EndsWith("Paragraph"))
                originalName = originalName[..^"Paragraph".Length];
            return ObjectNames.NicifyVariableName(originalName);
        }

        private void RemoveSelected(BaseListView view)
        {
            if (m_Page == null) return;

            List<int> indexes = view.selectedIndices.ToList();
            if (indexes.Count == 0)
            {
                if (m_Page.Paragraphs.Count == 0) return;
                indexes.Add(m_Page.Paragraphs.Count - 1);
            }
            indexes.Sort();

            int undoGroup = Undo.GetCurrentGroup();

            SerializedObject pageSerializedObject = new(m_Page);
            SerializedProperty paragraphsProperty = pageSerializedObject.FindProperty(nameof(TutorialPage.m_PageParagraphs));

            List<ParagraphBase> paragraphsToDestroy = new();

            // Iterate in reverse so deletions don't shift indices we haven't processed yet.
            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                int index = indexes[i];
                if (index < 0 || index >= paragraphsProperty.arraySize) continue;

                ParagraphBase paragraph = m_Page.Paragraphs[index];

                if (paragraph != null)
                {
                    paragraphsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                    paragraphsToDestroy.Add(paragraph);
                }
                paragraphsProperty.DeleteArrayElementAtIndex(index);
            }

            pageSerializedObject.ApplyModifiedProperties();

            foreach (ParagraphBase paragraph in paragraphsToDestroy)
                Undo.DestroyObjectImmediate(paragraph);

            AssetDatabase.SaveAssets();

            view.ClearSelection();
            Undo.SetCurrentGroupName(paragraphsToDestroy.Count > 1 ? "Remove Paragraphs" : "Remove Paragraph");
            Undo.CollapseUndoOperations(undoGroup);
        }

        private void AddItem(BaseListView view, Button b)
        {
            m_DropdownMenu.DropDown(b.worldBound);
        }

        private void NewParagraph(Type paragraphType)
        {
            m_Page.AddParagraph(paragraphType);
        }

        private void BindItem(VisualElement element, int idx)
        {
            SerializedProperty pElement = itemsSource[idx] as SerializedProperty;
            ParagraphBase p = pElement!.objectReferenceValue as ParagraphBase;

            if (p == null) return;

            Label typeTitle = new() { text = NicifyParagraphName(p.GetType().Name) };
            typeTitle.AddToClassList("inspector-paragraph-type-title");
            element.Add(typeTitle);

            PropertyField field = new();
            field.BindProperty(pElement);
            element.Add(field);
        }

        private void UnbindItem(VisualElement element, int idx)
        {
            element.Clear();
        }

        private VisualElement MakeItem()
        {
            VisualElement container = new();

            return container;
        }
    }
}
