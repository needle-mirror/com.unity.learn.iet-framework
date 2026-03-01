using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    internal class UnmaskedControlsListView : ListView
    {
        internal UnmaskedControlsListView(SerializedProperty prop)
        {
            this.BindProperty(prop);

            headerTitle = prop.displayName;
            name = prop.displayName;
            reorderable = true;
            reorderMode = ListViewReorderMode.Animated;
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            showBorder = true;
            showFoldoutHeader = true;
            showAddRemoveFooter = true;
            showBoundCollectionSize = false;

            AddToClassList("inspector-list");
            //AddToClassList("foldout-bold-title");
        }
    }
}
