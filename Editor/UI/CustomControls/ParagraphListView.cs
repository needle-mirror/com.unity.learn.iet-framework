using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Tutorials.Editor.Paragraphs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.CustomControl
{
    /// <summary>
    /// A ListView tweaked specifically to display <see cref="ParagraphBase"/> (and inheriting classes).
    /// Intended to be used within a <see cref="TutorialPage"/> Inspector.
    /// </summary>
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

            reorderable = true;
            reorderMode = ListViewReorderMode.Animated;
            selectionType = SelectionType.Multiple;
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            showAddRemoveFooter = true;
            showBoundCollectionSize = false;
            showBorder = true;
            showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            style.marginTop = 4;

            makeHeader = OnMakeHeader;
            overridingAddButtonBehavior += AddItem;
            itemsRemoved += ints => RemoveItems(ints);

            makeItem += MakeItem;
            bindItem += BindItem;
            unbindItem += UnbindItem;

            Type[] listOfParagraphTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => typeof(ParagraphBase).IsAssignableFrom(type) && type != typeof(ParagraphBase)
                ).ToArray();

            m_DropdownMenu = new GenericMenu();

            foreach (Type t in listOfParagraphTypes)
            {
                m_DropdownMenu.AddItem(new GUIContent(t.Name), false, _ => NewParagraph(t), null);
            }

            RegisterCallbackOnce<GeometryChangedEvent>(AddV6Warning);
        }

        private void RemoveItems(IEnumerable<int> ints)
        {
            List<int> indexes = ints.ToList();
            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                int index = indexes[i];
                ParagraphBase paragraph = m_Page.Paragraphs[index];
                AssetDatabase.RemoveObjectFromAsset(paragraph);
                m_Page.Paragraphs.RemoveAt(index);
            }

            AssetDatabase.SaveAssets();
        }

        private void AddV6Warning(GeometryChangedEvent evt)
        {
            VisualElement warning = new();
            warning.style.marginBottom = 6;

            if (m_Page!.LegacyParagraphs.Count > 0)
            {
                HelpBox helpBox = new("This TutorialPage contains paragraphs stored in a previous format. " +
                                      "It is recommended to migrate them to v6 paragraph format as soon as possible. ",
                    HelpBoxMessageType.Warning);
                warning.Add(helpBox);

                Button upgradeButton = new()
                {
                    text = "Upgrade to v6 Paragraph Format"
                };
                upgradeButton.clicked += OnMigrateButtonPressed;
                warning.Add(upgradeButton);
                enabledSelf = false;
                tooltip = "Run v6 paragraph format migration to re-enable paragraph editing on this page.";

                void OnMigrateButtonPressed()
                {
                    m_Page.MigrateToV6();

                    warning.Remove(helpBox);
                    warning.Remove(upgradeButton);
                    enabledSelf = true;
                    tooltip = "";
                }
            }

            parent.Add(warning);
            warning.PlaceInFront(this);
        }

        private VisualElement OnMakeHeader()
        {
            Label header = new("Paragraphs");
            header.style.marginBottom = 4;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;

            return header;
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
            ParagraphBase p = pElement.objectReferenceValue as ParagraphBase;

            if (p != null)
            {
                Label typeTitle = new();
                typeTitle.text = p.GetType().Name;
                typeTitle.AddToClassList("inspector-paragraph-type-title");
                element.Add(typeTitle);

                PropertyField pf = new();
                pf.BindProperty(pElement);
                element.Add(pf);
            }
        }

        private void UnbindItem(VisualElement element, int idx)
        {
            element.Clear();
        }

        private VisualElement MakeItem()
        {
            VisualElement container = new();
            container.AddToClassList("inspector-paragraph-container");

            return container;
        }
    }
}
