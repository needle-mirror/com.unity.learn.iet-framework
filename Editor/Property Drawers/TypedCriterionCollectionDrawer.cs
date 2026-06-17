using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for <see cref="TypedCriterionCollection"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TypedCriterionCollection))]
    internal class TypedCriterionCollectionDrawer : CollectionWrapperDrawer
    {
        private const string k_TypeNamePath = "Type.m_TypeName";
        private const string k_CriterionPropertyPath = "Criterion";

        protected override void OnListViewCreated(ListView listView)
        {
            listView.onAdd += view =>
            {
                ++m_ItemsProperty.arraySize;
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();
                SerializedProperty lastElement = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1);
                lastElement.FindPropertyRelative(k_TypeNamePath).stringValue = "";
                lastElement.FindPropertyRelative(k_CriterionPropertyPath).objectReferenceValue = null;
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();
            };

            // Override the base's onRemove so the underlying Criterion sub-asset is
            // destroyed via Undo, rather than being left orphaned for the deferred
            // TutorialPage.SyncCriteriaAndFutureReferences GC pass to clean up.
            listView.onRemove = view =>
            {
                int selectedIndex = view.selectedIndex;
                if (selectedIndex == -1) selectedIndex = m_ItemsProperty.arraySize - 1;
                if (selectedIndex < 0) return;

                int undoGroup = Undo.GetCurrentGroup();

                SerializedProperty element = m_ItemsProperty.GetArrayElementAtIndex(selectedIndex);
                Criterion criterion = element.FindPropertyRelative(k_CriterionPropertyPath).objectReferenceValue as Criterion;

                m_ItemsProperty.DeleteArrayElementAtIndex(selectedIndex);
                m_ItemsProperty.serializedObject.ApplyModifiedProperties();

                if (criterion != null) Undo.DestroyObjectImmediate(criterion);

                Undo.SetCurrentGroupName("Remove Criterion");
                Undo.CollapseUndoOperations(undoGroup);
            };

            base.OnListViewCreated(listView);
        }
    }
}
