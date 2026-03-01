using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    internal class UnmaskedViewsListView : ListView
    {
        internal UnmaskedViewsListView(SerializedProperty prop)
        {
            this.BindProperty(prop);

            headerTitle = prop.displayName;
            name = prop.displayName;
            reorderable = true;
            reorderMode = ListViewReorderMode.Animated;
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            showBorder = true;
            showFoldoutHeader = true;
            showAddRemoveFooter = true;
            showBoundCollectionSize = true;

            AddToClassList("inspector-list");
            //AddToClassList("foldout-bold-title");
        }
    }
}
